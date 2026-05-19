// ----------------------------------------------------------------------------------------------
// <copyright file="RepositoryExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Common.Batch;
using Shared.Common.Extensions;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Exceptions.Models;
using Shared.Domain.Core.Extensions;
using Shared.Domain.Core.Interfaces;
using Shared.Domain.Core.Mapping.Interfaces;

namespace Shared.Domain.Core.Dal.Repository.Extensions;

/// <summary>
/// Расширение для интерфейса <see cref="IRepository{TEntity}"/>.
/// Предоставляет вспомогательные методы для работы с репозиториями.
/// </summary>
public static class RepositoryExtensions
{
    /// <summary>
    /// Асинхронно извлекает существующие сущности на основе их идентификаторов и заданного фильтра по имени.
    /// </summary>
    /// <typeparam name="TEntity">Тип сущности.</typeparam>
    /// <param name="repository">Репозиторий для работы с сущностями.</param>
    /// <param name="ids">Массив идентификаторов сущностей.</param>
    /// <param name="nameFilter">Выражение для фильтрации сущностей по имени.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Список существующих сущностей, соответствующих критериям, или пустой список.</returns>
    public static async Task<ICollection<TEntity>> FindEntitiesByNamesAsync<TEntity>(
        this IRepository<TEntity> repository,
        Guid[] ids,
        Expression<Func<TEntity, bool>> nameFilter,
        CancellationToken cancellationToken = default)
        where TEntity : class, IEntity<Guid>
    {
        var queryOptions = new QueryOptions<TEntity>(true);
        queryOptions.AddFilter(nameFilter);

        if (ids.Any())
        {
            queryOptions.AddFilter(entity => !ids.Contains(entity.Id));
        }

        return await repository.GetRangeAsync(queryOptions, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Асинхронно извлекает сущность по ее идентификатору.
    /// В случае отсутствия сущности выбрасывает исключение <see cref="NotFoundException"/>.
    /// </summary>
    /// <typeparam name="TEntity">Тип сущности.</typeparam>
    /// <typeparam name="TKey">Тип идентификатора.</typeparam>
    /// <param name="repository">Репозиторий для работы с сущностями.</param>
    /// <param name="id">Идентификатор искомой сущности.</param>
    /// <param name="options">Опции запроса.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Найденная сущность.</returns>
    /// <exception cref="NotFoundException">Сущность не найдена.</exception>
    public static async Task<TEntity> GetByIdOrThrowAsync<TEntity, TKey>(
        this IRepository<TEntity> repository,
        TKey id,
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default)
        where TEntity : class, IEntity
    {
        var entity = await repository.GetAsync(id, options, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(typeof(TEntity), id);
        }

        return entity;
    }

    /// <summary>
    /// Асинхронно извлекает сущность, преобразованную в <see cref="TOut"/>, по ее идентификатору.
    /// В случае отсутствия сущности выбрасывает исключение <see cref="NotFoundException"/>.
    /// </summary>
    /// <typeparam name="TEntity">Тип сущности.</typeparam>
    /// <typeparam name="TKey">Тип идентификатора.</typeparam>
    /// <typeparam name="TOut">Тип преобразования.</typeparam>
    /// <param name="repository">Репозиторий для работы с сущностями.</param>
    /// <param name="id">Идентификатор искомой сущности.</param>
    /// <param name="options">Опции запроса.</param>
    /// <param name="selector">Преобразование (если null, то используется преобразование с помощью маппера).</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Найденная сущность.</returns>
    /// <exception cref="NotFoundException">Сущность не найдена.</exception>
    public static async Task<TOut> GetByIdOrThrowAsync<TEntity, TKey, TOut>(
        this IRepository<TEntity> repository,
        TKey id,
        QueryOptions<TEntity>? options = null,
        Expression<Func<TEntity, TOut>>? selector = default,
        CancellationToken cancellationToken = default)
        where TEntity : class, IEntity
    {
        var entity = await repository.GetAsync(id, options, selector, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(typeof(TEntity), id);
        }

        return entity;
    }

    /// <summary>
    /// Асинхронно извлекает сущности по их идентификаторам.
    /// В случае отсутствия одной или нескольких сущностей выбрасывает исключение <see cref="NotFoundException"/>.
    /// </summary>
    /// <typeparam name="TEntity">Тип сущности.</typeparam>
    /// <typeparam name="TKey">Тип идентификатора.</typeparam>
    /// <param name="repository">Репозиторий для работы с сущностями.</param>
    /// <param name="ids">Массив идентификаторов искомых сущностей.</param>
    /// <param name="options">Опции запроса.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Список найденных сущностей.</returns>
    /// <exception cref="NotFoundException">Одна или несколько сущностей не найдены.</exception>
    public static async Task<ICollection<TEntity>> GetByIdOrThrowAsync<TEntity, TKey>(
        this IRepository<TEntity> repository,
        TKey[] ids,
        QueryOptions<TEntity> options,
        CancellationToken cancellationToken = default)
        where TEntity : class, IEntity<TKey>
    {
        options.AddFilter(entity => ids.Contains(entity.Id));
        var entities = ids.Length == 1
            ? await repository.GetRangeAsync(options, cancellationToken: cancellationToken)
            : [await repository.GetAsync(ids.Single(), options, cancellationToken)];

        var notFoundEntities = entities.Where(entity => !ids.Contains(entity.Id)).ToArray();
        if (notFoundEntities.Any())
        {
            throw new NotFoundException(typeof(TEntity), ids);
        }

        return entities;
    }

    /// <summary>
    /// Асинхронно обновляет навигационные свойства для коллекции сущностей на основе предоставленных DTO.
    /// Позволяет управлять добавлением и удалением навигационных свойств.
    /// </summary>
    /// <typeparam name="TEntity">Тип основной сущности.</typeparam>
    /// <typeparam name="TNavigationDto">Тип DTO для навигационных сущностей.</typeparam>
    /// <typeparam name="TNavigationEntity">Тип навигационной сущности.</typeparam>
    /// <param name="repository">Репозиторий для навигационных сущностей.</param>
    /// <param name="entities">Коллекция основных сущностей.</param>
    /// <param name="navigationDtos">Коллекция DTO для навигационных сущностей.</param>
    /// <param name="comparisonFunc">Функция для сравнения основной сущности с навигационной сущностью.</param>
    /// <param name="mapper">Сервис маппинга объектов.</param>
    /// <param name="addAction">Действие для добавления навигационной сущности к основной сущности.</param>
    /// <param name="removeAction">Действие для удаления навигационной сущности из основной сущности.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public static Task UpdateNavigationPropertiesAsync<TEntity, TNavigationDto, TNavigationEntity>(
        this IRepository<TNavigationEntity> repository,
        ICollection<TEntity> entities,
        ICollection<TNavigationDto> navigationDtos,
        Func<TEntity, TNavigationEntity, bool> comparisonFunc,
        IMapper mapper,
        Action<TEntity, TNavigationEntity>? addAction = null,
        Action<TEntity, TNavigationEntity>? removeAction = null,
        CancellationToken cancellationToken = default)
        where TNavigationEntity : class, IEntity<Guid>
        where TNavigationDto : class, IEntity<Guid>
    {
        var navigationDtoIds = navigationDtos.Select(dto => dto.Id).ToArray();

        return UpdateNavigationPropertiesAsync(
            repository,
            entities,
            navigationDtos,
            navEntity => navigationDtoIds.Contains(navEntity.Id),
            comparisonFunc,
            (entity, navEntity) => entity.Id == navEntity.Id,
            (navEntity, navDto) => navEntity.Id == navDto.Id,
            dto => dto.Id,
            entity => entity.Id,
            mapper,
            addAction,
            removeAction,
            cancellationToken);
    }

    /// <summary>
    /// Асинхронно обновляет навигационные свойства сущностей на основе данных DTO и существующих навигационных сущностей в репозитории.
    /// </summary>
    /// <typeparam name="TDest">Тип основной сущности для обновления навигационных свойств.</typeparam>
    /// <typeparam name="TNavigationSrc">Тип DTO для навигационной сущности.</typeparam>
    /// <typeparam name="TNavigationDest">Тип навигационной сущности.</typeparam>
    /// <param name="repository">Репозиторий с доступом к навигационным сущностям.</param>
    /// <param name="destItems">Коллекция основных сущностей.</param>
    /// <param name="srcItems">Коллекция DTO навигационных сущностей для обновления существующих.</param>
    /// <param name="filter">Фильтр для выборки навигационных сущностей для обновления.</param>
    /// <param name="destComparisonFunc">Функция для сравнения основной сущности с навигационной.</param>
    /// <param name="navDestComparisonFunc">Функция для сравнения двух навигационных сущностей.</param>
    /// <param name="navDestToSrcComparisonFunc">Функция для сравнения навигационной сущности с ее DTO.</param>
    /// <param name="dtoSelector">Функция для выбора идентификатора из DTO навигационной сущности.</param>
    /// <param name="entitySelector">Функция для выбора идентификатора из навигационной сущности.</param>
    /// <param name="mapper">Сервис маппинга объектов.</param>
    /// <param name="addAction">Действие для добавления навигационной сущности.</param>
    /// <param name="removeAction">Действие для удаления навигационной сущности.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию обновления навигационных свойств.</returns>
    public static async Task UpdateNavigationPropertiesAsync<TDest, TNavigationSrc, TNavigationDest>(
        this IRepository<TNavigationDest> repository,
        ICollection<TDest> destItems,
        ICollection<TNavigationSrc> srcItems,
        Expression<Func<TNavigationDest, bool>> filter,
        Func<TDest, TNavigationDest, bool> destComparisonFunc,
        Func<TNavigationDest, TNavigationDest, bool> navDestComparisonFunc,
        Func<TNavigationDest, TNavigationSrc, bool> navDestToSrcComparisonFunc,
        Func<TNavigationSrc, object> dtoSelector,
        Func<TNavigationDest, object> entitySelector,
        IMapper mapper,
        Action<TDest, TNavigationDest>? addAction = null,
        Action<TDest, TNavigationDest>? removeAction = null,
        CancellationToken cancellationToken = default)
        where TNavigationDest : class, IEntity
        where TNavigationSrc : class
    {
        var options = new QueryOptions<TNavigationDest>(withTracking: true)
            .AddFilter(filter);

        var existingNavEntities = await repository.GetRangeAsync(
            options,
            cancellationToken: cancellationToken);

        await existingNavEntities.MergeAsync(
            srcItems,
            dtoSelector,
            entitySelector,
            toAdd =>
            {
                var newEntity = mapper.Map<TNavigationSrc, TNavigationDest>(toAdd);
                existingNavEntities.Add(newEntity);
                return Task.CompletedTask;
            },
            toRemove =>
            {
                var currentEntity = existingNavEntities.First(e => navDestComparisonFunc(e, toRemove));
                existingNavEntities.Remove(currentEntity);
                return Task.CompletedTask;
            },
            (src, dest) =>
            {
                mapper.Map(src, dest);
                return Task.CompletedTask;
            });

        foreach (var navEntity in existingNavEntities)
        {
            if (addAction is not null || removeAction is not null)
            {
                destItems
                    .Where(x => destComparisonFunc(x, navEntity))
                    .ForEach(entity =>
                    {
                        addAction?.Invoke(entity, navEntity);
                        removeAction?.Invoke(entity, navEntity);
                    });
            }
        }
    }

    /// <summary>
    /// Асинхронно обрабатывает батчи сущностей указанного типа.
    /// </summary>
    /// <typeparam name="TEntity">Тип сущности репозитория.</typeparam>
    /// <param name="repository">Репозиторий с сущностями.</param>
    /// <param name="options">Опции запроса для получения сущностей.</param>
    /// <param name="batchSize">Размер батча для обработки.</param>
    /// <param name="processBatchAction">Функция для обработки батча.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Задача для асинхронной операции обработки батчей.</returns>
    public static Task ProcessBatchesAsync<TEntity>(
        this IRepository<TEntity> repository,
        QueryOptions<TEntity> options,
        int batchSize = Constants.DefaultBatchSize,
        Func<ICollection<TEntity>, Task>? processBatchAction = null,
        CancellationToken cancellationToken = default)
        where TEntity : class, IEntity
    {
        return BatchHelper.ProcessBatchesAsync(
            async (skip, take) => await repository.GetRangeAsync(options, skip, take, cancellationToken),
            batchSize,
            processBatchAction,
            cancellationToken);
    }
}
