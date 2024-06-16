// ----------------------------------------------------------------------------------------------
// <copyright file="LoggingPipelineBehaviour.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using MediatR;
using Microsoft.Extensions.Logging;

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
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogDebug("Start processing {RequestName} request at {DateTime}", requestName, DateTime.UtcNow);

        var result = await next();

        logger.LogDebug("Finished processing {RequestName} request at {DateTime}", requestName, DateTime.UtcNow);

        return result;
    }
}
