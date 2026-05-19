// ----------------------------------------------------------------------------------------------
// <copyright file="ExceptionMapperBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Shared.Application.Core.Configuration.Extensions;
using Shared.Application.Core.Dto.Responses;
using Shared.Application.Core.Extensions;
using Shared.Presentation.Core.Exceptions.Interfaces;
using Shared.Presentation.Core.Exceptions.Settings;

namespace Shared.Presentation.Core.Exceptions.Mappers.Base;

/// <summary>
/// Базовый класс преобразователя исключений: задаёт <see cref="IExceptionMapper.HandledType"/>, делегирует
/// не-generic <see cref="IExceptionMapper.Map"/> в типобезопасный <see cref="Handle"/>
/// и предоставляет реализацию по умолчанию на основе <see cref="Title"/>.
/// </summary>
/// <typeparam name="TException">Тип обрабатываемого исключения.</typeparam>
public abstract class ExceptionMapperBase<TException>(
    IConfiguration configuration)
    : IExceptionMapper<TException>
    where TException : Exception
{
    private readonly ExceptionMapperSettings _settings = configuration.GetOptions<ExceptionMapperSettings>() ?? new();

    /// <inheritdoc />
    public Type HandledType => typeof(TException);

    /// <summary>
    /// Заголовок ошибки.
    /// </summary>
    protected abstract string Title { get; }

    /// <summary>
    /// Определяет, нужно ли добавлять stack trace и детали исключения в ответ.
    /// По умолчанию берётся из <see cref="ExceptionMapperSettings.ShouldEnrichWithTrace"/>;
    /// переопределите в <c>false</c> для исключений,
    /// данные которых не должны обогащаться (например, проксированные ошибки).
    /// </summary>
    protected virtual bool ShouldEnrichWithTrace => _settings.ShouldEnrichWithTrace;

    /// <inheritdoc />
    public ErrorResponse Map(Exception exception)
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
        return new ErrorResponse
        {
            Details = ShouldEnrichWithTrace
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

    private void AppendExceptionDetails(
        StringBuilder builder,
        Exception exception,
        int currentDepth)
    {
        if (currentDepth >= _settings.MaxExceptionDepth)
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
            foreach (var line in exception.StackTrace.Split(Environment.NewLine).Take(_settings.StackTraceDepth))
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
}
