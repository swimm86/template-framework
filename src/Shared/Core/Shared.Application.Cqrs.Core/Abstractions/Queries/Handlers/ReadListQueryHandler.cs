// ----------------------------------------------------------------------------------------------
// <copyright file="ReadListQueryHandler.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Dto.Interfaces;
using Shared.Application.Core.Dto.Requests;
using Shared.Application.Core.Dto.Responses;
using Shared.Application.Cqrs.Core.Abstractions.Queries.Requests;
using Shared.Common.Extensions;
using Shared.Common.Helpers;
using Shared.Domain.Core.Dal;
using Shared.Domain.Core.Dal.Models;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Cqrs.Core.Abstractions.Queries.Handlers;

/// <summary>
/// Базовый Handler множественного чтения
/// </summary>
/// <param name="loggerFactory">Фабрика логирования</param>
/// <param name="unitOfWork"><see cref="IUnitOfWork"/>.</param>
/// <typeparam name="TQuery">Query.</typeparam>
/// <typeparam name="TRequest">Запрос.</typeparam>
/// <typeparam name="TFilter">Фильтр.</typeparam>
/// <typeparam name="TResponse">Ответ.</typeparam>
/// <typeparam name="TPayload">ДТО.</typeparam>
/// <typeparam name="TEntity">Сущность.</typeparam>
public abstract class ReadListQueryHandler<TQuery, TRequest, TFilter, TResponse, TPayload, TEntity>(
    ILoggerFactory loggerFactory,
    IUnitOfWork unitOfWork)
    : EntityRequestHandler<TQuery, TResponse, TEntity>(unitOfWork, loggerFactory)
    where TQuery : ReadListQuery<TRequest, TFilter, TResponse>
    where TRequest : PageableRequest<TFilter>
    where TResponse : PageableResponse<ICollection<TPayload>>, new()
    where TEntity : class, IEntity
    where TFilter : FilterBase, new()
{
    /// <inheritdoc/>
    public override async Task<TResponse> Handle(
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
    /// <param name="query"> Запрос. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <returns> Ответ. </returns>
    protected virtual async Task<TResponse> FindAsync(
        TQuery query,
        CancellationToken cancellationToken)
    {
        var options = ConstructOptions(query);
        ApplySortOptions(query.Request.ConvertSortOptions(), options);
        var (skip, take) = CalculatePagination(query.PageNumber, query.PageSize);

        var repository = unitOfWork.GetRepository<TEntity>();
        var dtoList = await GetPayloadAsync(repository, options, skip, take);
        var totalCount = await repository.CountAsync(options, cancellationToken);
        await PostProcessAsync(dtoList, query);
        var pagesCount = PaginationHelper.GetTotalPages(totalCount, query.PageSize);
        var response = new TResponse
        {
            Payload = dtoList,
            StatusCode = StatusCodes.Status200OK,
            TotalPages = pagesCount,
            PageNumber = query.PageNumber,
        };
        await ProcessResponseAsync(response, query);
        return response;
    }

    /// <summary>
    /// Возвращает коллекцию проекций сущностей <see cref="TEntity"/> к <see cref="TPayload"/>.
    /// </summary>
    /// <param name="repository"><see cref="IRepository{TEntity}"/>.</param>
    /// <param name="options"><inheritdoc cref="QueryOptions{TEntity}"/>.</param>
    /// <param name="skip">Количество пропускаемых элементов.</param>
    /// <param name="take">Количество возвращаемых элементов.</param>
    /// <returns>Коллекция проекций сущностей <see cref="TEntity"/> к <see cref="TPayload"/>.</returns>
    protected virtual async Task<ICollection<TPayload>> GetPayloadAsync(
        IRepository<TEntity> repository,
        QueryOptions<TEntity> options,
        int? skip,
        int? take)
    {
        return await repository.GetRangeAsync<TPayload>(options, skip, take);
    }

    /// <summary>
    /// Пост обработка Payload-а.
    /// </summary>
    /// <param name="dtoCollection">Payload.</param>
    /// <param name="query"><see cref="TQuery"/>.</param>
    /// <returns><see cref="Task"/>.</returns>
    protected virtual Task PostProcessAsync(ICollection<TPayload> dtoCollection, TQuery query)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Добавление сортировки.
    /// </summary>
    /// <param name="sortOptions">Настройки сортировки.</param>
    /// <param name="options">Настройки запроса.</param>
    protected virtual void ApplySortOptions(
        IEnumerable<SortOption> sortOptions,
        QueryOptions<TEntity> options)
    {
        var sortOptionsCollection = sortOptions.ToList();
        sortOptionsCollection.ForEach(options.AddOrderBy);
        var dateCreatedOption = sortOptionsCollection.FirstOrDefault(x =>
            x.Key.Equals(nameof(IWithCreated.DateCreated), StringComparison.OrdinalIgnoreCase));
        if (typeof(TEntity).GetInterfaces().All(x => x != typeof(IWithDateCreated)) ||
            dateCreatedOption is not null)
        {
            return;
        }

        options.AddOrderBy(x => (x as IWithDateCreated)!.DateCreated, OrderDirectionType.Ascending, 0);
    }

    /// <inheritdoc/>
    protected override QueryOptions<TEntity> ConstructOptions(TQuery request)
    {
        var options = base.ConstructOptions(request);
        request.Filter.Fields?.ForEach(f => options.AddFilter(f));
        if (request.Filter is IWithIdsFilter baseFilter && (baseFilter.Ids?.Any() ?? false))
        {
            options.AddFilter(x => baseFilter.Ids.Any(id => id.Equals(x.Id)));
        }

        return options;
    }

    /// <summary>
    /// Расчет пагинации.
    /// </summary>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Количество элементов на странице.</param>
    /// <returns>Кортеж (кол-во элементов для пропуска, кол-во элементов для взятия)</returns>
    protected virtual (int? skip, int? take) CalculatePagination(int? pageNumber, int? pageSize)
    {
        if (pageSize == null)
        {
            return (null, null);
        }

        const int minPageAmount = 1;
        var skip = (pageNumber - minPageAmount) * pageSize;
        var take = pageSize;
        return (skip, take);
    }
}
