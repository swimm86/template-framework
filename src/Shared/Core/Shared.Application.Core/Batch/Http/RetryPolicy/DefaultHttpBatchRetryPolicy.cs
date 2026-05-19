// ----------------------------------------------------------------------------------------------
// <copyright file="DefaultHttpBatchRetryPolicy.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Batch.Http.RetryPolicy.Interfaces;
using Shared.Application.Core.Batch.Http.RetryPolicy.Models;

namespace Shared.Application.Core.Batch.Http.RetryPolicy;

/// <summary>
/// Политика повторов на основе <see cref="RetryConfiguration"/> (транзиентность HTTP/BCL, backoff, Retry-After из данных исключения).
/// </summary>
public sealed class DefaultHttpBatchRetryPolicy
    : IHttpBatchRetryPolicy
{
    private readonly RetryConfiguration _options;

    /// <summary>
    /// Создаёт политику повторов по указанным опциям.
    /// </summary>
    /// <param name="options">Конфигурация повторов.</param>
    public DefaultHttpBatchRetryPolicy(RetryConfiguration options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        _options = options;
    }

    /// <inheritdoc />
    public async Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var backoff = _options.Backoff;
        if (backoff.MaxAttempts <= 1)
        {
            return await operation(cancellationToken);
        }

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await operation(cancellationToken);
            }
            catch (Exception ex) when (
                attempt < backoff.MaxAttempts
                && HttpBatchRetryHelper.IsTransientFailure(ex, cancellationToken, _options))
            {
                var delay = HttpBatchRetryHelper.ComputeDelayBeforeRetry(ex, attempt, _options);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }
}
