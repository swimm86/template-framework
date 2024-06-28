// ----------------------------------------------------------------------------------------------
// <copyright file="ReadListQueryHandler.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Shared.Application.Core.Dal;
using Shared.Application.Core.Dal.Repository.Interfaces;
using Shared.Application.Core.Dal.Repository.Models;
using Shared.Application.Core.Dto.Requests;
using Shared.Application.Core.Dto.Responses;
using Shared.Application.Cqrs.Core.Abstractions.Queries.Requests;
using Shared.Application.Cqrs.Core.Utils;
using Shared.Application.Cqrs.Core.Utils.PostProcessors;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Cqrs.Core.Abstractions.Queries.Handlers;

/// <summary>
/// Базовый Handler множественного чтения
/// </summary>
/// <param name="loggerFactory">Фабрика логирования</param>
/// <param name="repository">Репозиторий из которого идет чтение</param>
/// <param name="postProcessor">Пост обработка найденной коллекции Необязательный параметр</param>
/// <typeparam name="TQuery">Query.</typeparam>
/// <typeparam name="TRequest">Request.</typeparam>
/// <typeparam name="TResponse">Response.</typeparam>
/// <typeparam name="TEntity">Сущность.</typeparam>
/// <typeparam name="TDto">Dto.</typeparam>
/// <typeparam name="TFilter">Фильтр.</typeparam>
public abstract class ReadListQueryHandler<TQuery, TRequest, TResponse, TEntity, TDto, TFilter>(
    ILoggerFactory loggerFactory,
    IRepository<TEntity> repository,
    IDtoPostProcessor<TDto>? postProcessor)
    : RequestHandler<TQuery, PageableResponse<ICollection<TDto>>>(loggerFactory)
    where TQuery : ReadListQuery<TRequest, TFilter, TResponse>
    where TRequest : PageableRequest<TFilter>
    where TResponse : PageableResponse<ICollection<TDto>>
    where TEntity : class, IEntity
    where TFilter : new()
{
    /// <inheritdoc/>
    public override async Task<PageableResponse<ICollection<TDto>>> Handle(
        TQuery query,
        CancellationToken cancellationToken)
    {
        await GuardAsync(query, cancellationToken).ConfigureAwait(false);
        var response = await FindAsync(query, cancellationToken).ConfigureAwait(false);
        return response;
    }

    /// <summary>
    /// Виртуальный метод поиска.
    /// </summary>
    /// <param name="request"> Запрос. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <returns> Ответ. </returns>
    protected virtual async Task<PageableResponse<ICollection<TDto>>> FindAsync(
        TQuery request,
        CancellationToken cancellationToken)
    {
        var specification = ConstructOptions(request);
        ApplySortOptions(request.SortOptions, specification);
        var (skip, take) = CalculatePagination(request.PageNumber, request.PageSize);

        var dtoList = await repository.GetRangeAsync<TDto>(specification, skip, take).ConfigureAwait(false);
        var totalCount = await repository.CountAsync(specification).ConfigureAwait(false);
        if (postProcessor != null)
        {
            await postProcessor.HandleAsync(dtoList).ConfigureAwait(false);
        }

        var pagesCount = request.PageSize.HasValue && totalCount > 0 ? totalCount / request.PageSize.Value : 0;
        return new PageableResponse<ICollection<TDto>>(pagesCount, dtoList);
    }

    /// <summary>
    /// Построение спецификации.
    /// </summary>
    /// <param name="request"> Запрос. </param>
    /// <returns> Спецификация. </returns>
    protected abstract QueryOptions<TEntity> ConstructOptions(TQuery request);

    /// <summary>
    /// Добавление сортировки.
    /// </summary>
    /// <param name="sortOptions">Настройки сортировки.</param>
    /// <param name="options">Настройки запроса.</param>
    protected virtual void ApplySortOptions(
        IEnumerable<SortOption> sortOptions,
        QueryOptions<TEntity> options)
    {
        if (typeof(TEntity).GetInterfaces().All(x => x != typeof(IWithDateCreated)))
        {
            return;
        }

        var sortList = sortOptions.ToList();
        var orderDirection =
            sortList
                .FirstOrDefault(x => x.Key.Equals(nameof(IWithCreated.DateCreated)))
                ?.DirectionType ??
            OrderDirectionType.Ascending;
        options.AddOrderBy(x => (x as IWithDateCreated)!.DateCreated, orderDirection, 0);
    }
}
