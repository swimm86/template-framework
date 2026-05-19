// ----------------------------------------------------------------------------------------------
// <copyright file="ValidationPipelineBehaviour.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Shared.Application.Cqrs.Core.Behaviours;

/// <summary>
/// Pipeline-поведение для автоматической валидации запросов перед обработкой.
/// Выполняет все зарегистрированные валидаторы FluentValidation и выбрасывает
/// <see cref="ValidationException"/> при наличии ошибок.
/// </summary>
/// <typeparam name="TRequest">Тип обрабатываемого запроса.</typeparam>
/// <typeparam name="TResponse">Тип возвращаемого значения.</typeparam>
/// <param name="logger">Логгер для записи событий валидации.</param>
/// <param name="validators">Коллекция валидаторов для типа запроса.</param>
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
            .GroupBy(f => new { f.PropertyName, f.ErrorMessage })
            .Select(g => g.First())
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
