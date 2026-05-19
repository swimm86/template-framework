// ----------------------------------------------------------------------------------------------
// <copyright file="ReadQueryHandler.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
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
/// Базовый обработчик запроса на чтение сущности по ключу.
/// </summary>
/// <typeparam name="TQuery">Тип запроса на чтение.</typeparam>
/// <typeparam name="TEntity">Тип читаемой сущности.</typeparam>
/// <typeparam name="TResponse">Тип возвращаемого значения.</typeparam>
/// <param name="loggerFactory">Фабрика для создания логгеров.</param>
/// <param name="mapper">Сервис маппинга объектов.</param>
/// <param name="unitOfWork">Единица работы для управления транзакциями.</param>
public abstract class ReadQueryHandler<TQuery, TEntity, TResponse>(
    ILoggerFactory loggerFactory,
    IMapper mapper,
    IUnitOfWork unitOfWork)
    : EntityRequestHandler<TQuery, TResponse, TEntity>(unitOfWork, loggerFactory), IQueryHandler<TQuery, TResponse>
    where TQuery : ReadByKeyQuery<TResponse>
    where TEntity : class, IEntity
{
    /// <inheritdoc />
    public override async Task<TResponse> Handle(
        TQuery query,
        CancellationToken cancellationToken)
    {
        await GuardAsync(query, cancellationToken);
        var entity = await FindAsync(query, cancellationToken);
        var dto = mapper.Map<TEntity, TResponse>(entity);
        return dto;
    }

    /// <summary>
    /// Выполняет поиск сущности по ключу.
    /// </summary>
    /// <param name="query">Запрос на чтение.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Найденная сущность.</returns>
    /// <exception cref="NotFoundException">Выбрасывается, если сущность не найдена.</exception>
    protected virtual async Task<TEntity> FindAsync(
        TQuery query,
        CancellationToken cancellationToken)
    {
        // TODO BUG (#4): Repository.GetAsync call does not pass cancellationToken — query execution will not be cancelled when the request is aborted
        var entity = await Repository.GetAsync(
            query.Key,
            ConstructOptions(query),
            cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException($"Сущность не найдена. Поиск по ключу: {query.Key}");
        }

        return entity;
    }
}
