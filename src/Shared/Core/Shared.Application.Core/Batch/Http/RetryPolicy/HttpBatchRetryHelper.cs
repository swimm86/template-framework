// ----------------------------------------------------------------------------------------------
// <copyright file="HttpBatchRetryHelper.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Shared.Application.Core.Batch.Http.RetryPolicy.Models;

namespace Shared.Application.Core.Batch.Http.RetryPolicy;

/// <summary>
/// Внутренняя реализация backoff, классификации транзиентных сбоев и расчёта паузы перед повтором страницы.
/// </summary>
internal static class HttpBatchRetryHelper
{
    /// <summary>
    /// Экспоненциальный backoff без джиттера и без Retry-After.
    /// </summary>
    /// <param name="failedAttemptIndex">Индекс неудачной попытки (1 после первого сбоя).</param>
    /// <param name="initial">Базовая задержка.</param>
    /// <param name="maxDelay">Верхняя граница или <see langword="null"/>.</param>
    /// <returns>Длительность паузы перед следующей попыткой.</returns>
    internal static TimeSpan ComputeBackoff(
        int failedAttemptIndex,
        TimeSpan initial,
        TimeSpan? maxDelay)
    {
        var scale = Math.Pow(2, failedAttemptIndex - 1);
        var ms = (long)Math.Min(double.MaxValue, initial.TotalMilliseconds * scale);
        var delay = TimeSpan.FromMilliseconds(ms);
        return maxDelay is { } cap && delay > cap ? cap : delay;
    }

    /// <summary>
    /// Транзиентность с учётом пользовательских правил и встроенных эвристик HTTP/BCL.
    /// </summary>
    /// <param name="ex">Исключение попытки.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <param name="options">Конфигурация повторов.</param>
    /// <returns><see langword="true"/>, если сбой считается временным.</returns>
    internal static bool IsTransientFailure(
        Exception ex,
        CancellationToken cancellationToken,
        RetryConfiguration options)
    {
        return options.Transient.IsAdditionalTransientException?.Invoke(ex) == true ||
               IsTransientBuiltIn(ex, cancellationToken);
    }

    /// <summary>
    /// Retry-After из <see cref="Exception.Data"/> одного узла исключения.
    /// </summary>
    /// <param name="ex">Исключение.</param>
    /// <returns>Положительная пауза или <see langword="null"/>.</returns>
    internal static TimeSpan? TryParseRetryAfterSingle(Exception ex)
    {
        if (ex.Data is not { Count: > 0 })
        {
            return null;
        }

        foreach (var keyObj in ex.Data.Keys)
        {
            if (keyObj is not string key)
            {
                continue;
            }

            if (!string.Equals(
                    key,
                    TransientConfiguration.RetryAfterExceptionDataKey,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return ConvertRetryAfterDataValue(ex.Data[key]);
        }

        return null;
    }

    /// <summary>
    /// Максимальная подсказка Retry-After по цепочке <see cref="Exception.InnerException"/>.
    /// </summary>
    /// <param name="ex">Корневое исключение.</param>
    /// <returns>Максимальная положительная пауза или <see langword="null"/>.</returns>
    internal static TimeSpan? TryGetRetryAfterMaxFromChain(Exception ex)
    {
        TimeSpan? best = null;
        for (var cur = ex; cur != null; cur = cur.InnerException)
        {
            var parsed = TryParseRetryAfterSingle(cur);
            best = MaxNullable(best, parsed);
        }

        return best;
    }

    /// <summary>
    /// Полная пауза перед следующей попыткой (backoff, подсказки, джиттер).
    /// </summary>
    /// <param name="ex">Исключение текущей попытки.</param>
    /// <param name="failedAttemptIndex">Индекс неудачной попытки.</param>
    /// <param name="options">Конфигурация повторов.</param>
    /// <returns>Длительность задержки перед следующей попыткой.</returns>
    internal static TimeSpan ComputeDelayBeforeRetry(
        Exception ex,
        int failedAttemptIndex,
        RetryConfiguration options)
    {
        var backoffSettings = options.Backoff;
        var backoff = ComputeBackoff(
            failedAttemptIndex,
            backoffSettings.InitialDelay,
            backoffSettings.MaxDelay);
        var dataHint = TryGetRetryAfterMaxFromChain(ex);
        var userHint = options.Transient.ResolveRetryDelayAfterTransientFailure?.Invoke(ex);
        var combinedHint = MaxNullable(dataHint, userHint);

        var delay = combinedHint is { } hint
            ? TimeSpan.FromMilliseconds(Math.Max(backoff.TotalMilliseconds, hint.TotalMilliseconds))
            : backoff;
        delay = ApplyOptionalBackoffJitter(delay, backoffSettings);
        if (backoffSettings.MaxDelay is { } cap && delay > cap)
        {
            delay = cap;
        }

        return delay;
    }

    private static TimeSpan ApplyOptionalBackoffJitter(
        TimeSpan delay,
        BackoffConfiguration backoffSettings)
    {
        if (!backoffSettings.UseBackoffJitter || delay <= TimeSpan.Zero)
        {
            return delay;
        }

        var ms = delay.TotalMilliseconds;
        var factor = 0.75 + (Random.Shared.NextDouble() * 0.5);
        var jitteredMs = (long)Math.Max(1, ms * factor);
        return TimeSpan.FromMilliseconds(jitteredMs);
    }

    private static bool IsTransientBuiltIn(Exception ex, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        for (var cur = ex; cur != null; cur = cur.InnerException)
        {
            switch (cur)
            {
                case HttpRequestException hre:
                    return IsTransientHttpRequestException(hre);
                case IOException:
                case SocketException:
                case TimeoutException:
                    return true;
                case TaskCanceledException tce:
                    return IsTransientTaskCanceledException(tce, cancellationToken);
            }
        }

        return false;
    }

    private static bool IsTransientHttpRequestException(HttpRequestException ex)
    {
        if (!ex.StatusCode.HasValue)
        {
            return true;
        }

        return IsTransientHttpStatusCode(ex.StatusCode.Value);
    }

    private static bool IsTransientHttpStatusCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.RequestTimeout => true,
            HttpStatusCode.TooManyRequests => true,
            HttpStatusCode.InternalServerError => true,
            HttpStatusCode.BadGateway => true,
            HttpStatusCode.ServiceUnavailable => true,
            HttpStatusCode.GatewayTimeout => true,
            _ => false,
        };
    }

    private static bool IsTransientTaskCanceledException(
        TaskCanceledException ex,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        return !cancellationToken.CanBeCanceled ||
               !ex.CancellationToken.CanBeCanceled ||
               !ex.CancellationToken.Equals(cancellationToken);
    }

    private static TimeSpan? ConvertRetryAfterDataValue(object? raw)
    {
        switch (raw)
        {
            case TimeSpan ts:
                return ts > TimeSpan.Zero ? ts : null;
            case int sec and > 0:
                return TimeSpan.FromSeconds(sec);
            case long secL and > 0:
                return TimeSpan.FromSeconds(secL);
            case double secD and > 0:
                return TimeSpan.FromSeconds(secD);
            case string s when !string.IsNullOrWhiteSpace(s):
                if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) && i > 0)
                {
                    return TimeSpan.FromSeconds(i);
                }

                if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) && d > 0)
                {
                    return TimeSpan.FromSeconds(d);
                }

                break;
        }

        return null;
    }

    private static TimeSpan? MaxNullable(TimeSpan? a, TimeSpan? b)
    {
        if (a is null)
        {
            return b;
        }

        if (b is null)
        {
            return a;
        }

        return a.Value >= b.Value ? a : b;
    }
}
