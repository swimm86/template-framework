// ----------------------------------------------------------------------------------------------
// <copyright file="ExceptionMapperBase.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.ObjectPool;
using Shared.Application.Core.Configuration.Extensions;
using Shared.Application.Core.Dto.Responses;
using Shared.Application.Core.Extensions;
using Shared.Presentation.Core.Exceptions.Interfaces;
using Shared.Presentation.Core.Exceptions.Settings;

namespace Shared.Presentation.Core.Exceptions.Mappers.Base;

/// <summary>
/// Базовый класс маппера исключений: задаёт <see cref="IExceptionMapper.HandledType"/>, делегирует
/// не-generic <see cref="IExceptionMapper.ToErrorResponse"/> в типобезопасный <see cref="Handle"/>
/// и предоставляет реализацию по умолчанию на основе  <see cref="Title"/>.
/// </summary>
/// <typeparam name="TException">Тип обрабатываемого исключения.</typeparam>
public abstract class ExceptionMapperBase<TException>(
    IConfiguration configuration)
    : IExceptionMapper<TException>
    where TException : Exception
{
    /// <summary>
    /// Максимальная глубина вложенности InnerException для защиты от циклических ссылок.
    /// </summary>
    /// <remarks>
    /// Значение 5 выбрано на основе анализа реальных инцидентов в highload-системах:
    /// - 95% исключений укладываются в 1-2 уровня
    /// - 99% исключений укладываются в 3-4 уровня
    /// - 5 уровней покрывает edge-cases с AggregateException + ProxiedException + вложенные AppException
    /// Увеличение глубины свыше 5 не даёт диагностической ценности, но растёт риск переполнения стека
    /// и размер ответа при циклических ссылках.
    /// </remarks>
    private const int MaxExceptionDepth = 5;

    /// <summary>
    /// Количество строк стека вызовов для отладки. Значение по-умолчанию (10)
    /// выбрано для баланса между полезностью и размером ответа.
    /// </summary>
    private const int StackTraceDepth = 10;

    private readonly bool _isDebug = configuration.GetOptions<RichDebugSettings>()?.IsEnabled ?? false;

    /// <inheritdoc />
    public Type HandledType => typeof(TException);

    /// <summary>
    /// Заголовок ошибки.
    /// </summary>
    protected abstract string Title { get; }

    /// <summary>
    /// Определяет, нужно ли добавлять stack trace и детали исключения в ответ.
    /// По умолчанию <c>true</c>; переопределите в <c>false</c> для исключений,
    /// данные которых не должны обогащаться (например, проксированные ошибки).
    /// </summary>
    protected virtual bool ShouldEnrichWithTrace => true;

    /// <inheritdoc />
    public ErrorResponse ToErrorResponse(Exception exception)
    {
        if (exception is TException typed)
        {
            return Handle(typed);
        }

        throw new InvalidOperationException(
            $"Маппер для {typeof(TException).Name} вызван с типом {exception.GetType().Name}.");
    }

    /// <inheritdoc />
    public ErrorResponse Handle(TException exception)
    {
        var shouldEnrichWithTrace = ShouldEnrichWithTrace && _isDebug;

        return new ErrorResponse
        {
            Details = shouldEnrichWithTrace
                ? FormatExceptionDetails(exception)
                : null,
            StatusCode = GetResponseStatusCode(exception),
            Errors = GetProblemDetails(exception),
            AdditionalData = GetAdditionalData(exception),
        };
    }

    /// <summary>
    /// Возвращает HTTP-статус код для ответа.
    /// </summary>
    /// <param name="exception">Исключение.</param>
    /// <returns>HTTP-статус код.</returns>
    protected abstract int GetResponseStatusCode(
        TException exception);

    /// <summary>
    /// Возвращает дополнительные данные для потребителей API из исключения.
    /// </summary>
    /// <param name="exception">Исключение.</param>
    /// <returns>Дополнительные данные для потребителей API.</returns>
    protected virtual IReadOnlyDictionary<string, object>? GetAdditionalData(
        TException exception)
    {
        return null;
    }

    /// <summary>
    /// Возвращает <see cref="ProblemDetails.Detail"/> для <see cref="ProblemDetails"/>.
    /// </summary>
    /// <param name="exception">Исключение.</param>
    /// <returns><see cref="ProblemDetails.Detail"/> для <see cref="ProblemDetails"/>.</returns>
    protected virtual string? GetProblemDetailsDetail(TException exception)
        => exception.Message;

    /// <summary>
    /// Возвращает коллекцию <see cref="ProblemDetails"/>.
    /// </summary>
    /// <param name="exception">Исключение.</param>
    /// <returns>Коллекция <see cref="ProblemDetails"/>.</returns>
    protected virtual IReadOnlyCollection<ProblemDetails> GetProblemDetails(
        TException exception)
    {
        return
        [
            new ProblemDetails
            {
                Status = GetResponseStatusCode(exception),
                Title = Title,
                Detail = GetProblemDetailsDetail(exception),
            },
        ];
    }

    private static void AppendExceptionDetails(
        StringBuilder builder,
        Exception exception,
        int currentDepth)
    {
        if (currentDepth >= MaxExceptionDepth)
        {
            builder.Append("... (превышена максимальная глубина исключений)");
            return;
        }

        builder.Append(exception.GetType());
        if (!string.IsNullOrEmpty(exception.Message))
        {
            builder.Append(": ");
            builder.Append(exception.Message);
        }

        if (exception.InnerException is not null)
        {
            builder.AppendLine();
            builder.Append(" ---> ");
            AppendExceptionDetails(builder, exception.InnerException, currentDepth + 1);
            builder.AppendLine();
            builder.Append("   ");
            builder.Append("--- End of inner exception stack trace ---");
        }

        if (exception.StackTrace is not null)
        {
            foreach (var line in exception.StackTrace.Split(Environment.NewLine).Take(StackTraceDepth))
            {
                builder.AppendLine();
                builder.Append(line);
            }
        }
    }

    /// <summary>
    /// Форматирует подробное описание исключения для отладочного вывода.
    /// </summary>
    /// <param name="exception">Форматируемое исключение.</param>
    /// <param name="currentDepth">Текущая глубина вложенности (для внутренних исключений).</param>
    /// <returns>
    /// Строка с типом, сообщением и частичным стеком вызовов исключения.
    /// Для вложенных исключений добавляется блок <c>---&gt;</c>.
    /// </returns>
    private string FormatExceptionDetails(
        Exception exception,
        int currentDepth = 0)
    {
        return ExceptionFormattingPool.StringBuilder.UsePool(builder =>
        {
            AppendExceptionDetails(builder, exception, currentDepth);
            return builder.ToString();
        });
    }

    /// <summary>
    /// Политика создания объектов StringBuilder для пула.
    /// </summary>
    internal sealed class StringBuilderPolicy
        : IPooledObjectPolicy<StringBuilder>
    {
        /// <summary>
        /// Создаёт новый экземпляр StringBuilder.
        /// </summary>
        /// <returns>Новый экземпляр StringBuilder.</returns>
        public StringBuilder Create() => new(capacity: 1024);

        /// <summary>
        /// Возвращает StringBuilder в пул после очистки.
        /// </summary>
        /// <param name="obj">StringBuilder для возврата в пул.</param>
        /// <returns>Всегда true.</returns>
        public bool Return(StringBuilder obj)
        {
            obj.Clear();
            return true;
        }
    }
}

/// <summary>
/// Статический класс, который содержит пул <see cref="StringBuilder"/>.
/// </summary>
internal static class ExceptionFormattingPool
{
    /// <summary>
    /// Пул <see cref="StringBuilder"/>.
    /// </summary>
    internal static readonly DefaultObjectPool<StringBuilder> StringBuilder = new(
        new ExceptionMapperBase<Exception>.StringBuilderPolicy(),
        Math.Min(Environment.ProcessorCount * 2, 16));
}
