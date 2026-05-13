// ----------------------------------------------------------------------------------------------
// <copyright file="RequestHandler.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Shared.Application.Cqrs.Core.Abstractions;

/// <summary>
/// Базовый обработчик
/// </summary>
/// <param name="loggerFactory">Фабрика логгеров.</param>
/// <typeparam name="TRequest">Тип запроса.</typeparam>
/// <typeparam name="TResponse">Тип ответа.</typeparam>
public abstract class RequestHandler<TRequest, TResponse>(
    ILoggerFactory loggerFactory)
    : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Инстанс логгера.
    /// </summary>
    protected readonly ILogger Logger = loggerFactory.CreateLogger<RequestHandler<TRequest, TResponse>>();

    /// <summary>
    /// Метод вызыва. Вызывается внутри IMediatr.
    /// Вызывается первым.
    /// </summary>
    /// <param name="query">запрос</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <returns><see cref="Task"/>.</returns>
    public abstract Task<TResponse> Handle(TRequest query, CancellationToken cancellationToken);

    /// <summary>
    /// Гарды.
    /// </summary>
    /// <param name="request">Запрос.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns><see cref="Task"/>.</returns>
    protected virtual Task GuardAsync(TRequest request, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Поиск и вызов валидаторов для TEntity
    /// </summary>
    /// <param name="entity">Сущность</param>
    /// <param name="validators">Коллекция валидаторов</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <exception cref="ValidationException">Ошибка валидации</exception>
    /// <typeparam name="TEntity">Тип сущности.</typeparam>
    /// <returns><see cref="Task"/></returns>
    protected virtual async Task ValidateAsync<TEntity>(
        TEntity entity,
        IEnumerable<IValidator<TEntity>> validators,
        CancellationToken cancellationToken = default)
    {
        var validatorsArray = validators as IValidator<TEntity>[] ?? validators.ToArray();
        if (validatorsArray.Length == 0)
        {
            return;
        }

        var validationContext = new ValidationContext<TEntity>(entity);
        var failures = new List<ValidationFailure>();

        foreach (var validator in validatorsArray)
        {
            var result = await validator.ValidateAsync(validationContext, cancellationToken).ConfigureAwait(false);
            failures.AddRange(result.Errors.Where(error => error is not null));
        }

        if (failures.Count != 0)
        {
            throw new ValidationException(failures);
        }
    }
}
