// ----------------------------------------------------------------------------------------------
// <copyright file="ReadQueryHandler.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Shared.Application.Cqrs.Core.Abstractions.Queries.Requests;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Exceptions.Models;
using Shared.Domain.Core.Interfaces;
using Shared.Domain.Core.Mapping.Interfaces;

namespace Shared.Application.Cqrs.Core.Abstractions.Queries.Handlers;

/// <summary>
/// Обработчик чтения.
/// </summary>
/// <typeparam name="TQuery">Запроса.</typeparam>
/// <typeparam name="TEntity">Сущность.</typeparam>
/// <typeparam name="TResponse"> Ответ.</typeparam>
/// <param name="loggerFactory">Логгер.</param>
/// <param name="mapper">Сервис маппинга.</param>
public abstract class ReadQueryHandler<TQuery, TEntity, TResponse>(
    ILoggerFactory loggerFactory,
    IMapper mapper,
    IUnitOfWork unitOfWork)
    : EntityRequestHandler<TQuery, TResponse, TEntity>(unitOfWork, loggerFactory), IQueryHandler<TQuery, TResponse>
    where TQuery : ReadByKeyQuery<TResponse>
    where TEntity : class, IEntity
{
    /// <summary>
    /// Обработчик.
    /// </summary>
    /// <param name="query"> Запрос. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <returns> Dto. </returns>
    public override async Task<TResponse> Handle(TQuery query, CancellationToken cancellationToken)
    {
        await GuardAsync(query, cancellationToken);
        var entity = await FindAsync(query, cancellationToken);
        var dto = mapper.Map<TEntity, TResponse>(entity);
        return dto;
    }

    /// <summary>
    /// Поиск сущности.
    /// </summary>
    /// <param name="query">  Запрос. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <returns> Сущность. </returns>
    /// <exception cref="NullReferenceException"> Вызывается, когда сущность не найдена. </exception>
    protected virtual async Task<TEntity> FindAsync(TQuery query, CancellationToken cancellationToken)
    {
        var entity = await Repository.GetAsync(query.Key, ConstructOptions(query));
        if (entity is null)
        {
            throw new NotFoundException($"Сущность не найдена. Поиск по ключу: {query.Key}");
        }

        return entity;
    }
}
