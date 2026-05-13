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
/// Пайплайн логирования
/// </summary>
/// <param name="logger">Логгер.</param>
/// <typeparam name="TRequest">Тип запроса.</typeparam>
/// <typeparam name="TResponse">Тип ответа.</typeparam>
internal sealed class LoggingPipelineBehaviour<TRequest, TResponse>(
    ILogger<LoggingPipelineBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken _)
    {
        return logger.LogTaskAsync(
            () => next(),
            processDescription: $"'{request.GetType().Name}' handler");
    }
}
