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
/// Базовый класс обработчика запросов MediatR.
/// </summary>
/// <typeparam name="TRequest">Тип обрабатываемого запроса.</typeparam>
/// <typeparam name="TResponse">Тип возвращаемого значения.</typeparam>
/// <param name="loggerFactory">Фабрика для создания логгеров.</param>
public abstract class RequestHandler<TRequest, TResponse>(
    ILoggerFactory loggerFactory)
    : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>Логгер для записи событий обработки запроса.</summary>
    protected readonly ILogger Logger = loggerFactory.CreateLogger<RequestHandler<TRequest, TResponse>>();

    /// <inheritdoc />
    public abstract Task<TResponse> Handle(TRequest query, CancellationToken cancellationToken);

    /// <summary>
    /// Выполняет предварительные проверки перед обработкой запроса.
    /// Вызывается первым в цепочке обработки.
    /// </summary>
    /// <param name="request">Обрабатываемый запрос.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Задача выполнения проверок.</returns>
    protected virtual Task GuardAsync(TRequest request, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Выполняет валидацию сущности с использованием всех зарегистрированных валидаторов.
    /// </summary>
    /// <typeparam name="TEntity">Тип валидируемой сущности.</typeparam>
    /// <param name="entity">Сущность для валидации.</param>
    /// <param name="validators">Коллекция валидаторов.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <exception cref="ValidationException">Выбрасывается при ошибках валидации.</exception>
    /// <returns>Задача выполнения валидации.</returns>
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
            var result = await validator.ValidateAsync(validationContext, cancellationToken);
            failures.AddRange(result.Errors.Where(error => error is not null));
        }

        if (failures.Count != 0)
        {
            throw new ValidationException(failures);
        }
    }
}
