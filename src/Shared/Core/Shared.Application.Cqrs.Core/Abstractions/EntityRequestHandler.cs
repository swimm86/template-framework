// ----------------------------------------------------------------------------------------------
// <copyright file="EntityRequestHandler.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
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
/// Базовый обрабочтик для работы с сущностями.
/// </summary>
/// <typeparam name="TRequest">Тип запроса.</typeparam>
/// <typeparam name="TResponse">Тип ответа.</typeparam>
/// <typeparam name="TEntity">Тип сущности.</typeparam>
/// <param name="loggerFactory">Фабрика логгеров.</param>
public abstract class EntityRequestHandler<TRequest, TResponse, TEntity>(
    IUnitOfWork unitOfWork,
    ILoggerFactory loggerFactory)
    : RequestHandler<TRequest, TResponse>(loggerFactory)
    where TRequest : IRequest<TResponse>
    where TEntity : class, IEntity
{
    /// <summary>
    /// Репозиторий.
    /// </summary>
    protected IRepository<TEntity> Repository => unitOfWork.GetRepository<TEntity>();

    /// <summary>
    /// Построение параметров для запроса.
    /// </summary>
    /// <param name="request">Запрос.</param>
    /// <returns>Параметры для запроса.</returns>
    protected virtual QueryOptions<TEntity> ConstructOptions(TRequest request)
    {
        return ConstructOptions(request, false);
    }

    /// <summary>
    /// Построение параметров для запроса.
    /// </summary>
    /// <param name="request">Запрос.</param>
    /// <param name="withDeletable">Признак того, что необходимо включить в результат удаленные сущности.</param>
    /// <returns>Параметры для запроса.</returns>
    protected virtual QueryOptions<TEntity> ConstructOptions(
        TRequest request,
        bool withDeletable)
    {
        var options = new QueryOptions<TEntity>();

        if (!withDeletable && typeof(IWithDeleted).IsAssignableFrom(typeof(TEntity)))
        {
            options.AddFilter(x => !((IWithDeleted)x).IsDeleted);
        }

        return options;
    }

    /// <summary>
    /// Дополнительные операции с сущностью перед сохранением.
    /// </summary>
    /// <param name="entity">Сущность.</param>
    /// <param name="request"><see cref="IRequest"/>.</param>
    /// <returns><see cref="Task"/>.</returns>
    protected virtual Task ProcessEntityAsync(TEntity entity, TRequest request)
    {
        return Task.FromResult(true);
    }

    /// <summary>
    /// Пост обработка Response-а.
    /// </summary>
    /// <param name="response">Response.</param>
    /// <param name="request">Запрос.</param>
    /// <returns><see cref="Task"/>.</returns>
    protected virtual Task ProcessResponseAsync(TResponse response, TRequest request)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Дополнительные операции с сущностями.
    /// </summary>
    /// <param name="entities">Сущности.</param>
    /// <param name="request">Команда.</param>
    /// <returns><see cref="Task"/>.</returns>
    protected virtual Task ProcessEntitiesAsync(ICollection<TEntity> entities, TRequest request)
    {
        return Task.FromResult(true);
    }
}
