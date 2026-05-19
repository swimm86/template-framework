// ----------------------------------------------------------------------------------------------
// <copyright file="LoggingPipelineBehaviour.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Common.Logging.Extensions;

namespace Shared.Application.Cqrs.Core.Behaviours;

/// <summary>
/// Pipeline-поведение для логирования выполнения запросов и команд.
/// Записывает время выполнения и результат обработки каждого запроса.
/// </summary>
/// <typeparam name="TRequest">Тип обрабатываемого запроса.</typeparam>
/// <typeparam name="TResponse">Тип возвращаемого значения.</typeparam>
/// <param name="logger">Экземпляр <see cref="ILogger"/> для работы с логированием.</param>
internal sealed class LoggingPipelineBehaviour<TRequest, TResponse>(
    ILogger<LoggingPipelineBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        // TODO BUG (#1): CancellationToken discarded with '_' — token is not passed to `next()` delegate, cancelling the handler does not propagate to inner pipeline steps
        CancellationToken _)
    {
        return logger.LogTaskAsync(
            () => next(),
            processDescription: $"'{request.GetType().Name}' handler");
    }
}
