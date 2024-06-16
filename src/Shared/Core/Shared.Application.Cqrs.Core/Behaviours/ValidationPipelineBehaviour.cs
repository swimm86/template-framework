// ----------------------------------------------------------------------------------------------
// <copyright file="ValidationPipelineBehaviour.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Shared.Application.Cqrs.Core.Behaviours;

/// <summary>
/// Пайплайн валидации
/// </summary>
/// <typeparam name="TRequest">Тип запроса.</typeparam>
/// <typeparam name="TResponse">Тип ответа.</typeparam>
internal sealed class ValidationPipelineBehaviour<TRequest, TResponse>(
    ILogger<ValidationPipelineBehaviour<TRequest, TResponse>> logger,
    IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogDebug("Start processing validation for {RequestName}.", requestName);

        if (!validators.Any())
        {
            logger.LogDebug("There is no any validator for {RequestName}.", requestName);

            return await next();
        }

        var validationContext = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            validators.Select(v =>
                v.ValidateAsync(validationContext, cancellationToken)));

        var failures = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();
        if (failures.Count == 0)
        {
            logger.LogDebug("Validation for {RequestName} succeeded.", requestName);

            return await next();
        }

        logger.LogWarning("Validation for {RequestName} failed.", requestName);

        throw new ValidationException(failures);
    }
}
