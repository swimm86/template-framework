// ----------------------------------------------------------------------------------------------
// <copyright file="EntityRequestHandler.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Cqrs.Core.Abstractions;

/// <summary>
/// Базовый обработчик запросов, работающих с сущностями.
/// </summary>
/// <typeparam name="TRequest">Тип обрабатываемого запроса.</typeparam>
/// <typeparam name="TResponse">Тип возвращаемого значения.</typeparam>
/// <typeparam name="TEntity">Тип сущности, с которой работает обработчик.</typeparam>
/// <param name="unitOfWork">Единица работы для управления транзакциями.</param>
/// <param name="loggerFactory">Фабрика для создания логгеров.</param>
public abstract class EntityRequestHandler<TRequest, TResponse, TEntity>(
    IUnitOfWork unitOfWork,
    ILoggerFactory loggerFactory)
    : RequestHandler<TRequest, TResponse>(loggerFactory)
    where TRequest : IRequest<TResponse>
    where TEntity : class, IEntity
{
    /// <summary>Признак отслеживания изменений сущностей при запросе к базе данных.</summary>
    protected virtual bool WithTracking => false;

    /// <summary>Признак загрузки связанных сущностей раздельными SQL-запросами.</summary>
    protected virtual bool AsSplitQuery => false;

    /// <summary>Репозиторий для работы с сущностями типа <typeparamref name="TEntity"/>.</summary>
    protected IRepository<TEntity> Repository => unitOfWork.GetRepository<TEntity>();

    /// <summary>
    /// Формирует параметры запроса к базе данных.
    /// </summary>
    /// <param name="request">Исходный запрос.</param>
    /// <returns>Параметры запроса.</returns>
    protected virtual QueryOptions<TEntity> ConstructOptions(TRequest request)
    {
        return ConstructOptions(request, false);
    }

    /// <summary>
    /// Формирует параметры запроса к базе данных.
    /// </summary>
    /// <param name="request">Исходный запрос.</param>
    /// <param name="withDeletable">Включать логически удалённые сущности в результат.</param>
    /// <returns>Параметры запроса.</returns>
    protected virtual QueryOptions<TEntity> ConstructOptions(
        TRequest request,
        bool withDeletable)
    {
        var options = new QueryOptions<TEntity>(WithTracking, AsSplitQuery);
        if (!withDeletable && typeof(IWithDeleted).IsAssignableFrom(typeof(TEntity)))
        {
            options.AddFilter(x => !((IWithDeleted)x).IsDeleted);
        }

        return options;
    }

    /// <summary>
    /// Выполняет дополнительные действия с сущностью перед сохранением.
    /// </summary>
    /// <param name="entity">Сущность для обработки.</param>
    /// <param name="request">Исходный запрос.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Задача выполнения обработки.</returns>
    protected virtual Task ProcessEntityAsync(
        TEntity entity,
        TRequest request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    /// <summary>
    /// Выполняет постобработку ответа перед возвратом.
    /// </summary>
    /// <param name="response">Ответ для постобработки.</param>
    /// <param name="request">Исходный запрос.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Задача выполнения постобработки.</returns>
    protected virtual Task ProcessResponseAsync(
        TResponse response,
        TRequest request,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Выполняет дополнительные действия с коллекцией сущностей.
    /// </summary>
    /// <param name="entities">Коллекция сущностей для обработки.</param>
    /// <param name="request">Исходный запрос.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Задача выполнения обработки.</returns>
    protected virtual Task ProcessEntitiesAsync(
        ICollection<TEntity> entities,
        TRequest request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }
}
