// ----------------------------------------------------------------------------------------------
// <copyright file="IGetterRepository.Get.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Dal.Specification.Interfaces;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Dal.Repository.Interfaces;

/// <summary>
/// Интерфейс репозитория получения данных для сущности <typeparamref name="TEntity"/>.
/// </summary>
/// <typeparam name="TEntity">Тип сущности.</typeparam>
public partial interface IGetterRepository<TEntity>
    where TEntity : class, IEntity
{
    /// <summary>
    /// Асинхронно возвращает экземпляр сущности по её идентификатору с учётом настроек запроса.
    /// </summary>
    /// <param name="id">Идентификатор сущности.</param>
    /// <param name="options">Настройки запроса. Если <see langword="null"/>, запрос выполняется без дополнительных настроек.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Экземпляр сущности или <see langword="null"/>, если выборка пуста.</returns>
    Task<TEntity?> GetAsync(
        object id,
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Асинхронно возвращает <typeparamref name="TOut"/>-проекцию экземпляра сущности по её идентификатору с учётом настроек запроса.
    /// </summary>
    /// <typeparam name="TOut">Тип результата проекции.</typeparam>
    /// <param name="selector">Выражение проекции. Если <see langword="null"/>, проекция выполняется с использованием маппера.</param>
    /// <returns><typeparamref name="TOut"/>-проекция экземпляра сущности или <see langword="null"/>, если выборка пуста.</returns>
    /// <inheritdoc cref="GetAsync(object?, QueryOptions{TEntity}?, CancellationToken)"/>
    /// <param name="id"/><param name="options"/><param name="cancellationToken"/>
    Task<TOut?> GetAsync<TOut>(
        object id,
        QueryOptions<TEntity>? options = null,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="GetAsync(object?, QueryOptions{TEntity}?, CancellationToken)"/>
    /// <param name="specification">Спецификация.</param>
    /// <param name="id"/><param name="cancellationToken"/>
    Task<TEntity?> GetAsync(
        object id,
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default) =>
        GetAsync(id, specification.BuildOptions(), cancellationToken);

    /// <inheritdoc cref="IGetterRepository{TEntity}.GetAsync{TOut}(object, QueryOptions{TEntity}?, Expression{Func{TEntity, TOut}}?, CancellationToken)"/>
    /// <param name="specification"><inheritdoc cref="GetAsync(object, ISpecification{TEntity}, CancellationToken)" path="/param[@name='specification']"/></param>
    /// <param name="id"/><param name="selector"/><param name="cancellationToken"/><typeparam name="TOut"/>
    Task<TOut?> GetAsync<TOut>(
        object id,
        ISpecification<TEntity> specification,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default) =>
        GetAsync(id, specification.BuildOptions(), selector, cancellationToken);

    /// <summary>
    /// Асинхронно возвращает коллекцию сущностей с учётом настроек запроса.
    /// </summary>
    /// <param name="options">Настройки запроса. Если <see langword="null"/>, запрос выполняется без дополнительных настроек.</param>
    /// <param name="skip">Количество сущностей, которые необходимо пропустить.</param>
    /// <param name="take">Количество сущностей, которые необходимо извлечь.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Коллекция сущностей, отфильтрованных и упорядоченных согласно заданным параметрам.</returns>
    Task<List<TEntity>> GetRangeAsync(
        QueryOptions<TEntity>? options = null,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Асинхронно возвращает коллекцию <typeparamref name="TOut"/>-проекций сущностей с учётом настроек запроса.
    /// </summary>
    /// <typeparam name="TOut">Тип результата проекции.</typeparam>
    /// <param name="selector">Выражение проекции. Если <see langword="null"/>, проекция выполняется с использованием маппера.</param>
    /// <returns>Коллекция <typeparamref name="TOut"/>-проекций сущностей, отфильтрованных и упорядоченных согласно заданным параметрам.</returns>
    /// <inheritdoc cref="GetRangeAsync(QueryOptions{TEntity}?, int?, int?, CancellationToken)"/>
    /// <param name="options"/><param name="skip"/><param name="take"/><param name="cancellationToken"/>
    Task<List<TOut>> GetRangeAsync<TOut>(
        QueryOptions<TEntity>? options = null,
        int? skip = null,
        int? take = null,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="GetRangeAsync(QueryOptions{TEntity}?, int?, int?, CancellationToken)"/>
    /// <param name="specification">Спецификация.</param>
    /// <param name="skip"/><param name="take"/><param name="cancellationToken"/>
    Task<List<TEntity>> GetRangeAsync(
        ISpecification<TEntity> specification,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default) =>
        GetRangeAsync(specification.BuildOptions(), skip, take, cancellationToken);

    /// <inheritdoc cref="IGetterRepository{TEntity}.GetRangeAsync{TOut}(QueryOptions{TEntity}?, int?, int?, Expression{Func{TEntity, TOut}}?, CancellationToken)"/>
    /// <param name="specification"><inheritdoc cref="GetRangeAsync(ISpecification{TEntity}, int?, int?, CancellationToken)" path="/param[@name='specification']"/></param>
    /// <param name="skip"/><param name="take"/><param name="selector"/><param name="cancellationToken"/><typeparam name="TOut"/>
    Task<List<TOut>> GetRangeAsync<TOut>(
        ISpecification<TEntity> specification,
        int? skip = null,
        int? take = null,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default) =>
        GetRangeAsync(specification.BuildOptions(), skip, take, selector, cancellationToken);

    /// <summary>
    /// Асинхронно возвращает первый элемент выборки с учётом настроек запроса или null, если выборка пуста.
    /// </summary>
    /// <param name="options">Настройки запроса. Если <see langword="null"/>, запрос выполняется без дополнительных настроек.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Первый элемент выборки или null, если выборка пуста.</returns>
    Task<TEntity?> FirstOrDefaultAsync(
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Асинхронно возвращает <typeparamref name="TOut"/>-проекцию первого элемента выборки с учётом настроек запроса или null, если выборка пуста.
    /// </summary>
    /// <typeparam name="TOut">Тип результата проекции.</typeparam>
    /// <param name="selector">Выражение проекции. Если <see langword="null"/>, проекция выполняется с использованием маппера.</param>
    /// <returns><typeparamref name="TOut"/>-проекция первого элемента выборки или null, если выборка пуста.</returns>
    /// <inheritdoc cref="FirstOrDefaultAsync(QueryOptions{TEntity}?, CancellationToken)"/>
    /// <param name="options"/><param name="cancellationToken"/>
    Task<TOut?> FirstOrDefaultAsync<TOut>(
        QueryOptions<TEntity>? options = null,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="FirstOrDefaultAsync(QueryOptions{TEntity}?, CancellationToken)"/>
    /// <param name="specification">Спецификация.</param>
    /// <param name="cancellationToken"/>
    Task<TEntity?> FirstOrDefaultAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default) =>
        FirstOrDefaultAsync(specification.BuildOptions(), cancellationToken);

    /// <inheritdoc cref="IGetterRepository{TEntity}.FirstOrDefaultAsync{TOut}(QueryOptions{TEntity}?, Expression{Func{TEntity, TOut}}?, CancellationToken)"/>
    /// <param name="specification"><inheritdoc cref="FirstOrDefaultAsync(ISpecification{TEntity}, CancellationToken)" path="/param[@name='specification']"/></param>
    /// <param name="selector"/><param name="cancellationToken"/><typeparam name="TOut"/>
    Task<TOut?> FirstOrDefaultAsync<TOut>(
        ISpecification<TEntity> specification,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default) =>
        FirstOrDefaultAsync(specification.BuildOptions(), selector, cancellationToken);

    /// <summary>
    /// Асинхронно возвращает единственный элемент выборки с учётом настроек запроса или null, если выборка пуста.
    /// </summary>
    /// <param name="options">Настройки запроса. Если <see langword="null"/>, запрос выполняется без дополнительных настроек.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Единственный элемент выборки или null, если выборка пуста.</returns>
    /// <exception cref="InvalidOperationException">Выборка содержит более одного элемента.</exception>
    Task<TEntity?> SingleOrDefaultAsync(
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Асинхронно возвращает <typeparamref name="TOut"/>-проекцию единственного элемента выборки с учётом настроек запроса или null, если выборка пуста.
    /// </summary>
    /// <typeparam name="TOut">Тип результата проекции.</typeparam>
    /// <param name="selector">Выражение проекции. Если <see langword="null"/>, проекция выполняется с использованием маппера.</param>
    /// <returns><typeparamref name="TOut"/>-проекция единственного элемента выборки или null, если выборка пуста.</returns>
    /// <inheritdoc cref="SingleOrDefaultAsync(QueryOptions{TEntity}?, CancellationToken)"/>
    /// <param name="options"/><param name="cancellationToken"/>
    Task<TOut?> SingleOrDefaultAsync<TOut>(
        QueryOptions<TEntity>? options = null,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="SingleOrDefaultAsync(QueryOptions{TEntity}?, CancellationToken)"/>
    /// <param name="specification">Спецификация.</param>
    /// <param name="cancellationToken"/>
    Task<TEntity?> SingleOrDefaultAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default) =>
        SingleOrDefaultAsync(specification.BuildOptions(), cancellationToken);

    /// <inheritdoc cref="IGetterRepository{TEntity}.SingleOrDefaultAsync{TOut}(QueryOptions{TEntity}?, Expression{Func{TEntity, TOut}}?, CancellationToken)"/>
    /// <param name="specification"><inheritdoc cref="SingleOrDefaultAsync(ISpecification{TEntity}, CancellationToken)" path="/param[@name='specification']"/></param>
    /// <param name="selector"/><param name="cancellationToken"/><typeparam name="TOut"/>
    Task<TOut?> SingleOrDefaultAsync<TOut>(
        ISpecification<TEntity> specification,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default) =>
        SingleOrDefaultAsync(specification.BuildOptions(), selector, cancellationToken);

    /// <summary>
    /// Асинхронно возвращает последний элемент выборки с учётом настроек запроса или null, если выборка пуста.
    /// </summary>
    /// <param name="options">Настройки запроса. Если <see langword="null"/>, запрос выполняется без дополнительных настроек.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Последний элемент выборки или null, если выборка пуста.</returns>
    Task<TEntity?> LastOrDefaultAsync(
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Асинхронно возвращает <typeparamref name="TOut"/>-проекцию последнего элемента выборки с учётом настроек запроса или null, если выборка пуста.
    /// </summary>
    /// <typeparam name="TOut">Тип результата проекции.</typeparam>
    /// <param name="selector">Выражение проекции. Если <see langword="null"/>, проекция выполняется с использованием маппера.</param>
    /// <returns><typeparamref name="TOut"/>-проекция последнего элемента выборки или null, если выборка пуста.</returns>
    /// <inheritdoc cref="LastOrDefaultAsync(QueryOptions{TEntity}?, CancellationToken)"/>
    /// <param name="options"/><param name="cancellationToken"/>
    Task<TOut?> LastOrDefaultAsync<TOut>(
        QueryOptions<TEntity>? options = null,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="LastOrDefaultAsync(QueryOptions{TEntity}?, CancellationToken)"/>
    /// <param name="specification">Спецификация.</param>
    /// <param name="cancellationToken"/>
    Task<TEntity?> LastOrDefaultAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default) =>
        LastOrDefaultAsync(specification.BuildOptions(), cancellationToken);

    /// <inheritdoc cref="IGetterRepository{TEntity}.LastOrDefaultAsync{TOut}(QueryOptions{TEntity}?, Expression{Func{TEntity, TOut}}, CancellationToken)"/>
    /// <param name="specification"><inheritdoc cref="LastOrDefaultAsync(ISpecification{TEntity}, CancellationToken)" path="/param[@name='specification']"/></param>
    /// <param name="selector"/><param name="cancellationToken"/><typeparam name="TOut"/>
    Task<TOut?> LastOrDefaultAsync<TOut>(
        ISpecification<TEntity> specification,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default) =>
        LastOrDefaultAsync(specification.BuildOptions(), selector, cancellationToken);

    /// <summary>
    /// Возвращает сгруппированные по ключу <typeparamref name="TKey"/> элементы с учётом настроек запроса.
    /// </summary>
    /// <remarks>
    /// Сортировка групп по ключу (<paramref name="groupKeyOrderDirection"/>) поддерживается
    /// провайдерами, транслирующими <c>OrderBy</c> по ключу группы в SQL (например, Npgsql/MSSQL).
    /// Для InMemory и SQLite-провайдеров упорядочивание может выполняться клиентской оценкой
    /// либо не поддерживаться вовсе — см. тесты <c>EfRepositoryIntegrationTests.GetGroupingAsync_AscendingOrder_*</c>
    /// и <c>GetGroupingAsync_DescendingOrder_*</c>, помеченные <c>[Fact(Skip = ...)]</c>.
    /// При использовании пагинации (<paramref name="skip"/>/<paramref name="take"/>) порядок групп
    /// должен быть задан явно; в противном случае реализация бросает <see cref="ArgumentException"/>.
    /// </remarks>
    /// <param name="keySelector">Выражение для выбора ключа группировки.</param>
    /// <param name="options">Настройки запроса. Если <see langword="null"/>, запрос выполняется без дополнительных настроек.</param>
    /// <param name="skip">Количество групп, которые необходимо пропустить.</param>
    /// <param name="take">Количество групп, которые необходимо извлечь.</param>
    /// <param name="groupKeyOrderDirection">
    /// Направление сортировки групп по ключу: <see langword="null"/> — порядок не задаётся;
    /// иначе <see cref="OrderDirectionType.Ascending"/> / <see cref="OrderDirectionType.Descending"/>.
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <typeparam name="TKey">Тип ключа группировки.</typeparam>
    /// <returns>Элементы, сгруппированные по ключу <typeparamref name="TKey"/>.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="skip"/> или <paramref name="take"/> заданы при <paramref name="groupKeyOrderDirection"/> = <see langword="null"/>.
    /// </exception>
    /// <example>
    /// <code>
    /// Expression&lt;Func&lt;Product, int&gt;&gt; keySelector = e =&gt; e.CategoryId;
    /// var options = new QueryOptions&lt;Product&gt;().AddFilter(e =&gt; e.IsActive);
    /// var groups = await repository.GetGroupingAsync(
    ///     keySelector: keySelector,
    ///     options: options,
    ///     skip: 0,
    ///     take: 10,
    ///     groupKeyOrderDirection: OrderDirectionType.Ascending,
    ///     cancellationToken: ct);
    /// </code>
    /// </example>
    Task<List<IGrouping<TKey, TEntity>>> GetGroupingAsync<TKey>(
        Expression<Func<TEntity, TKey>> keySelector,
        QueryOptions<TEntity>? options = null,
        int? skip = null,
        int? take = null,
        OrderDirectionType? groupKeyOrderDirection = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает сгруппированные по ключу <typeparamref name="TKey"/> <typeparamref name="TOut"/>-проекции элементов с учётом настроек запроса.
    /// </summary>
    /// <remarks>
    /// Параметр <paramref name="selector"/> применяется к каждой <see cref="IGrouping{TKey, TEntity}"/>,
    /// а не к отдельному элементу выборки. Для проекции элементов внутри группы используйте
    /// <c>g.SelectMany(items =&gt; …)</c> внутри выражения.
    /// </remarks>
    /// <typeparam name="TOut">Тип результата проекции.</typeparam>
    /// <param name="selector">Выражение проекции. Если <see langword="null"/>, проекция выполняется с использованием маппера.</param>
    /// <returns><typeparamref name="TOut"/>-проекции элементов, сгруппированные по ключу <typeparamref name="TKey"/>.</returns>
    /// <inheritdoc cref="GetGroupingAsync{TKey}"/>
    /// <typeparam name="TKey"/>
    /// <param name="keySelector"/><param name="options"/><param name="skip"/><param name="take"/>
    /// <param name="groupKeyOrderDirection"/><param name="cancellationToken"/>
    Task<List<TOut>> GetGroupingAsync<TKey, TOut>(
        Expression<Func<TEntity, TKey>> keySelector,
        QueryOptions<TEntity>? options = null,
        int? skip = null,
        int? take = null,
        Expression<Func<IGrouping<TKey, TEntity>, TOut>>? selector = null,
        OrderDirectionType? groupKeyOrderDirection = null,
        CancellationToken cancellationToken = default);
}
