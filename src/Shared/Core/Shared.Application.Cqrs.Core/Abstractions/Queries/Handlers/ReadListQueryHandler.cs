// ----------------------------------------------------------------------------------------------
// <copyright file="ReadListQueryHandler.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Dto.Requests;
using Shared.Application.Core.Dto.Responses;
using Shared.Application.Cqrs.Core.Abstractions.Queries.Requests;
using Shared.Common.Helpers;
using Shared.Domain.Core.Dal;
using Shared.Domain.Core.Dal.Models;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Cqrs.Core.Abstractions.Queries.Handlers;

/// <summary>
/// Базовый обработчик запроса на чтение коллекции сущностей с пагинацией.
/// </summary>
/// <typeparam name="TQuery">Тип запроса на чтение.</typeparam>
/// <typeparam name="TRequest">Тип запроса с параметрами пагинации.</typeparam>
/// <typeparam name="TFilter">Тип фильтра для отбора данных.</typeparam>
/// <typeparam name="TResponse">Тип возвращаемого значения.</typeparam>
/// <typeparam name="TPayload">Тип данных полезной нагрузки ответа.</typeparam>
/// <typeparam name="TEntity">Тип читаемой сущности.</typeparam>
/// <param name="loggerFactory">Фабрика для создания логгеров.</param>
/// <param name="unitOfWork">Единица работы для управления транзакциями.</param>
public abstract class ReadListQueryHandler<TQuery, TRequest, TFilter, TResponse, TPayload, TEntity>(
    ILoggerFactory loggerFactory,
    IUnitOfWork unitOfWork)
    : EntityRequestHandler<TQuery, TResponse, TEntity>(unitOfWork, loggerFactory)
    where TQuery : ReadListQuery<TRequest, TFilter, TResponse>
    where TRequest : PageableRequest<TFilter>
    where TResponse : PageableResponse<ICollection<TPayload>>, new()
    where TEntity : class, IEntity
    where TFilter : new()
{
    /// <inheritdoc />
    public override async Task<TResponse> Handle(
        TQuery query,
        CancellationToken cancellationToken)
    {
        await GuardAsync(query, cancellationToken);
        var response = await FindAsync(query, cancellationToken);
        return response;
    }

    /// <summary>
    /// Выполняет поиск сущностей с применением пагинации и сортировки.
    /// </summary>
    /// <param name="query">Запрос на чтение.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Ответ с пагинированной коллекцией данных.</returns>
    protected virtual async Task<TResponse> FindAsync(
        TQuery query,
        CancellationToken cancellationToken)
    {
        var options = ConstructOptions(query);
        ApplySortOptions(query.Request.ConvertSortOptions(), options);
        var (skip, take) = PaginationHelper.CalculatePagination(query.PageNumber, query.PageSize);

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
        await ProcessResponseAsync(response, query, cancellationToken);
        return response;
    }

    /// <summary>
    /// Возвращает коллекцию проекций сущностей <see cref="TEntity"/> к типу <see cref="TPayload"/>.
    /// </summary>
    /// <param name="repository">Репозиторий для работы с сущностями.</param>
    /// <param name="options">Параметры запроса к базе данных.</param>
    /// <param name="skip">Количество пропускаемых элементов.</param>
    /// <param name="take">Количество возвращаемых элементов.</param>
    /// <returns>Коллекция проекций сущностей.</returns>
    protected virtual async Task<ICollection<TPayload>> GetPayloadAsync(
        IRepository<TEntity> repository,
        QueryOptions<TEntity> options,
        int? skip,
        int? take)
    {
        return await repository.GetRangeAsync<TPayload>(options, skip, take);
    }

    /// <summary>
    /// Выполняет постобработку полезной нагрузки ответа.
    /// </summary>
    /// <param name="dtoCollection">Коллекция DTO для постобработки.</param>
    /// <param name="query">Исходный запрос.</param>
    /// <returns>Задача выполнения постобработки.</returns>
    protected virtual Task PostProcessAsync(ICollection<TPayload> dtoCollection, TQuery query)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Применяет параметры сортировки к запросу.
    /// </summary>
    /// <param name="sortOptions">Параметры сортировки.</param>
    /// <param name="options">Параметры запроса к базе данных.</param>
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

    /// <inheritdoc />
    protected override QueryOptions<TEntity> ConstructOptions(TQuery request)
    {
        var options = base.ConstructOptions(request);
        if (request.Filter is ListFilterBase baseFilter && (baseFilter.Ids?.Any() ?? false))
        {
            options.AddFilter(x => baseFilter.Ids.Any(id => id.Equals(x.Id)));
        }

        return options;
    }
}
