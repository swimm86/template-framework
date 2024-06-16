// ----------------------------------------------------------------------------------------------
// <copyright file="ReadQueryHandler.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Shared.Application.Core.Dal.Repository.Interfaces;
using Shared.Application.Core.Dal.Repository.Models;
using Shared.Application.Core.Exceptions.Models;
using Shared.Application.Core.Mapping.Interfaces;
using Shared.Application.Cqrs.Core.Abstractions.Queries.Requests;
using Shared.Application.Cqrs.Core.Utils.PostProcessors;
using Shared.Domain.Core.Interfaces;

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
    IRepository<TEntity> repository,
    IDtoPostProcessor<TResponse>? postProcessor)
    : RequestHandler<TQuery, TResponse>(loggerFactory), IQueryHandler<TQuery, TResponse>
    where TQuery : ReadByKeyQuery<TResponse>
    where TEntity : class, IEntity
{
    /// <summary>
    /// Обработчик.
    /// </summary>
    /// <param name="request"> Запрос. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <returns> Dto. </returns>
    public override async Task<TResponse> Handle(TQuery request, CancellationToken cancellationToken)
    {
        await GuardAsync(request, cancellationToken).ConfigureAwait(false);
        var entity = await FindAsync(request, cancellationToken).ConfigureAwait(false);
        var dto = mapper.Map<TEntity, TResponse>(entity);
        if (postProcessor != null)
        {
            await postProcessor.HandleAsync([dto]).ConfigureAwait(false);
        }

        return dto;
    }

    /// <summary>
    /// Поиск сущности.
    /// </summary>
    /// <param name="request">  Запрос. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <returns> Сущность. </returns>
    /// <exception cref="NullReferenceException"> Вызывается, когда сущность не найдена. </exception>
    protected virtual async Task<TEntity> FindAsync(TQuery request, CancellationToken cancellationToken)
    {
        var entity = await repository.GetAsync(request.Key, ConstructQueryOptions(request)).ConfigureAwait(false);
        if (entity is null)
        {
            throw new NotFoundException($"Сущность не найдена. Поиск по ключу: {request.Key}");
        }

        return entity;
    }

    /// <summary>
    /// Построение настроек для запроса.
    /// </summary>
    /// <param name="request">Запрос.</param>
    /// <returns>Настройки для запроса.</returns>
    protected virtual QueryOptions<TEntity>? ConstructQueryOptions(TQuery request) => null;
}
