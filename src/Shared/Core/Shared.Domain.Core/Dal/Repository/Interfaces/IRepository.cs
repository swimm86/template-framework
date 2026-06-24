// ----------------------------------------------------------------------------------------------
// <copyright file="IRepository.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Dal.Specification.Interfaces;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Dal.Repository.Interfaces;

/// <summary>
/// Интерфейс репозитория данных для сущности <typeparamref name="TEntity"/>.
/// </summary>
/// <typeparam name="TEntity">Тип сущности.</typeparam>
public interface IRepository<TEntity>
    where TEntity : class, IEntity
{
    #region Read methods

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

    /// <inheritdoc cref="IRepository{TEntity}.GetAsync{TOut}(object, QueryOptions{TEntity}?, Expression{Func{TEntity, TOut}}?, CancellationToken)"/>
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

    /// <inheritdoc cref="IRepository{TEntity}.GetRangeAsync{TOut}(QueryOptions{TEntity}?, int?, int?, Expression{Func{TEntity, TOut}}?, CancellationToken)"/>
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

    /// <inheritdoc cref="IRepository{TEntity}.FirstOrDefaultAsync{TOut}(QueryOptions{TEntity}?, Expression{Func{TEntity, TOut}}?, CancellationToken)"/>
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

    /// <inheritdoc cref="IRepository{TEntity}.SingleOrDefaultAsync{TOut}(QueryOptions{TEntity}?, Expression{Func{TEntity, TOut}}?, CancellationToken)"/>
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

    /// <inheritdoc cref="IRepository{TEntity}.LastOrDefaultAsync{TOut}(QueryOptions{TEntity}?, Expression{Func{TEntity, TOut}}, CancellationToken)"/>
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

    #endregion

    #region Aggregation methods

    /// <summary>
    /// Асинхронно возвращает количество элементов в выборке с учётом настроек запроса.
    /// </summary>
    /// <param name="options">Настройки запроса. Если <see langword="null"/>, запрос выполняется без дополнительных настроек.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Количество элементов в выборке.</returns>
    Task<int> CountAsync(
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CountAsync(QueryOptions{TEntity}?, CancellationToken)"/>
    /// <param name="specification">Спецификация.</param>
    /// <param name="cancellationToken"/>
    Task<int> CountAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default) =>
        CountAsync(specification.BuildOptions(), cancellationToken);

    /// <summary>
    /// Асинхронно возвращает признак наличия хотя бы одного элемента в выборке с учётом настроек запроса.
    /// </summary>
    /// <param name="options">Настройки запроса. Если <see langword="null"/>, запрос выполняется без дополнительных настроек.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Значение true, если выборка содержит хотя бы один элемент, иначе false.</returns>
    Task<bool> AnyAsync(
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="AnyAsync(QueryOptions{TEntity}?, CancellationToken)"/>
    /// <param name="specification">Спецификация.</param>
    /// <param name="cancellationToken"/>
    Task<bool> AnyAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default) =>
        AnyAsync(specification.BuildOptions(), cancellationToken);

    /// <summary>
    /// Асинхронно вычисляет сумму проекций элементов выборки в числовые значения с учётом настроек запроса.
    /// </summary>
    /// <param name="selector">Выражение проекции, применяемое к каждому элементу.</param>
    /// <param name="options">Настройки запроса. Если <see langword="null"/>, запрос выполняется без дополнительных настроек.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Сумма проекций элементов выборки.</returns>
    Task<decimal> SumAsync(
        Expression<Func<TEntity, decimal>> selector,
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="SumAsync(Expression{Func{TEntity, decimal}}, QueryOptions{TEntity}?, CancellationToken)"/>
    /// <param name="specification">Спецификация.</param>
    /// <param name="selector"/><param name="cancellationToken"/>
    Task<decimal> SumAsync(
        Expression<Func<TEntity, decimal>> selector,
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default) =>
        SumAsync(selector, specification.BuildOptions(), cancellationToken);

    /// <summary>
    /// Асинхронно возвращает количество групп при группировке по ключу <typeparamref name="TKey"/> с учётом настроек запроса.
    /// </summary>
    /// <param name="keySelector">Выражение для выбора ключа группировки.</param>
    /// <param name="options">Настройки запроса. Если <see langword="null"/>, запрос выполняется без дополнительных настроек.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <typeparam name="TKey">Тип ключа группировки.</typeparam>
    /// <returns>Количество групп.</returns>
    Task<int> CountGroupsAsync<TKey>(
        Expression<Func<TEntity, TKey>> keySelector,
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="IRepository{TEntity}.CountGroupsAsync{TKey}(Expression{Func{TEntity, TKey}}, QueryOptions{TEntity}?, CancellationToken)"/>
    /// <param name="specification">Спецификация.</param>
    /// <param name="keySelector"/><param name="cancellationToken"/><typeparam name="TKey"/>
    Task<int> CountGroupsAsync<TKey>(
        Expression<Func<TEntity, TKey>> keySelector,
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default) =>
        CountGroupsAsync(keySelector, specification.BuildOptions(), cancellationToken);

    /// <summary>
    /// Возвращает максимальное значение <typeparamref name="TOut"/>-проекции <paramref name="selector"/> с учётом настроек запроса.
    /// </summary>
    /// <param name="selector">Проекция, к которой применяется агрегатная функция.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <typeparam name="TOut">Тип результата проекции.</typeparam>
    /// <returns>
    /// Максимальное значение <typeparamref name="TOut"/>-проекции, либо <see langword="default"/>(<typeparamref name="TOut"/>),
    /// если выборка не содержит элементов.
    /// </returns>
    Task<TOut?> MaxAsync<TOut>(
        Expression<Func<TEntity, TOut>> selector,
        CancellationToken cancellationToken = default) =>
        MaxAsync(selector, options: null, cancellationToken);

    /// <summary>
    /// Возвращает максимальное значение проекции <paramref name="selector"/> с учётом настроек запроса.
    /// </summary>
    /// <param name="options">Настройки запроса. Если <see langword="null"/>, запрос выполняется без дополнительных настроек.</param>
    /// <inheritdoc cref="IRepository{TEntity}.MaxAsync{TOut}(Expression{Func{TEntity, TOut}}, CancellationToken)"/>
    /// <param name="selector"/><param name="cancellationToken"/>
    Task<TOut?> MaxAsync<TOut>(
        Expression<Func<TEntity, TOut>> selector,
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="IRepository{TEntity}.MaxAsync{TOut}(Expression{Func{TEntity, TOut}}, QueryOptions{TEntity}?, CancellationToken)"/>
    /// <param name="specification">Спецификация.</param>
    /// <param name="selector"/><param name="cancellationToken"/><typeparam name="TOut"/>
    Task<TOut?> MaxAsync<TOut>(
        Expression<Func<TEntity, TOut>> selector,
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default) =>
        MaxAsync(selector, specification.BuildOptions(), cancellationToken);

    /// <summary>
    /// Возвращает минимальное значение <typeparamref name="TOut"/>-проекции <paramref name="selector"/> с учётом настроек запроса.
    /// </summary>
    /// <param name="selector">Проекция, к которой применяется агрегатная функция.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <typeparam name="TOut">Тип результата проекции.</typeparam>
    /// <returns>
    /// Минимальное значение <typeparamref name="TOut"/>-проекции, либо <see langword="default"/>(<typeparamref name="TOut"/>),
    /// если выборка не содержит элементов.
    /// </returns>
    Task<TOut?> MinAsync<TOut>(
        Expression<Func<TEntity, TOut>> selector,
        CancellationToken cancellationToken = default) =>
        MinAsync(selector, options: null, cancellationToken);

    /// <summary>
    /// Возвращает минимальное значение проекции <paramref name="selector"/> с учётом настроек запроса.
    /// </summary>
    /// <param name="options">Настройки запроса. Если <see langword="null"/>, запрос выполняется без дополнительных настроек.</param>
    /// <inheritdoc cref="IRepository{TEntity}.MinAsync{TOut}(Expression{Func{TEntity, TOut}}, CancellationToken)"/>
    /// <param name="selector"/><param name="cancellationToken"/>
    Task<TOut?> MinAsync<TOut>(
        Expression<Func<TEntity, TOut>> selector,
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="IRepository{TEntity}.MinAsync{TOut}(Expression{Func{TEntity, TOut}}, QueryOptions{TEntity}?, CancellationToken)"/>
    /// <param name="specification">Спецификация.</param>
    /// <param name="selector"/><param name="cancellationToken"/><typeparam name="TOut"/>
    Task<TOut?> MinAsync<TOut>(
        Expression<Func<TEntity, TOut>> selector,
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default) =>
        MinAsync(selector, specification.BuildOptions(), cancellationToken);

    #endregion

    #region Add methods

    /// <summary>
    /// Асинхронно добавляет экземпляр сущности в БД.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="entity">Экземпляр сущности.</param>
    /// <param name="userId">Id пользователя, добавившего запись.</param>
    /// <param name="userName">Имя пользователя, добавившего запись.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Экземпляр созданной сущности.</returns>
    Task<TEntity> AddAsync(
        TEntity entity,
        Guid? userId = null,
        string? userName = null,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="AddAsync(TEntity, Guid?, string?, CancellationToken)"/>
    /// <param name="entities">Коллекция экземпляров сущности.</param>
    /// <param name="userId"/><param name="userName"/><param name="cancellationToken"/>
    Task AddRangeAsync(
        IEnumerable<TEntity> entities,
        Guid? userId = null,
        string? userName = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Update methods

    /// <summary>
    /// Выполняет пакетное обновление свойств сущностей на уровне БД (без предварительной загрузки экземпляров) с учётом настроек запроса.
    /// </summary>
    /// <param name="options">Настройки запроса. Если <see langword="null"/>, запрос выполняется без дополнительных настроек.</param>
    /// <param name="updateData">Массив кортежей, содержащий выражения для обновляемых свойств и их значений.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task ExecuteUpdateRangeAsync(
        QueryOptions<TEntity> options,
        params (LambdaExpression propertyExpression, LambdaExpression valueExpression)[] updateData);

    /// <summary>
    /// Выполняет пакетное обновление свойств сущностей на уровне БД (без предварительной загрузки экземпляров) с учётом настроек запроса.
    /// </summary>
    /// <param name="specification">Спецификация.</param>
    /// <param name="updateData">Массив кортежей, содержащий выражения для обновляемых свойств и их значений.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task ExecuteUpdateRangeAsync(
        ISpecification<TEntity> specification,
        params (LambdaExpression propertyExpression, LambdaExpression valueExpression)[] updateData) =>
        ExecuteUpdateRangeAsync(specification.BuildOptions(), updateData);

    /// <summary>
    /// Выполняет пакетное обновление свойств сущностей на уровне БД (без предварительной загрузки экземпляров) по указанной спецификации.
    /// </summary>
    /// <param name="predicate">Условие фильтрации сущностей, к которым применяются обновления.</param>
    /// <param name="updateData">Массив кортежей, содержащий выражения для обновляемых свойств и их значений.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task ExecuteUpdateRangeAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        params (LambdaExpression propertyExpression, LambdaExpression valueExpression)[] updateData)
    {
        var options = new QueryOptions<TEntity>();
        if (predicate is not null)
        {
            options.AddFilter(predicate);
        }

        return ExecuteUpdateRangeAsync(options, updateData);
    }

    /// <summary>
    /// Создаёт кортеж выражений для использования в методах массового обновления.
    /// </summary>
    /// <typeparam name="TProp">Тип обновляемого свойства.</typeparam>
    /// <param name="propertyExpression">Выражение, указывающее на обновляемое свойство.</param>
    /// <param name="valueExpression">Выражение, вычисляющее новое значение свойства.</param>
    /// <returns>Кортеж из двух выражений: свойства и значения.</returns>
    /// <remarks>
    /// Пример использования:
    /// <code>
    /// await repository.ExecuteUpdateRangeAsync(
    ///     x => x.Id == targetId,
    ///     repository.GetUpdateRangeAsyncLambdaFunc(x => x.Name, x => "newName"),
    ///     repository.GetUpdateRangeAsyncLambdaFunc(x => x.Version, x => "v2"));
    /// </code>
    /// </remarks>
    public static (LambdaExpression, LambdaExpression) GetUpdateRangeAsyncLambdaFunc<TProp>(
        Expression<Func<TEntity, TProp>> propertyExpression,
        Expression<Func<TEntity, TProp>> valueExpression)
    {
        return (propertyExpression, valueExpression);
    }

    #endregion

    #region Remove methods

    /// <summary>
    /// Асинхронно удаляет экземпляр сущности из БД.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="entity">Экземпляр сущности.</param>
    /// <param name="userId">Id пользователя, удалившего запись.</param>
    /// <param name="hard">Признак того, что сущность должна быть удалена физически.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task RemoveAsync(
        TEntity entity,
        Guid? userId,
        bool hard = false,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="RemoveAsync(TEntity, Guid?, bool, CancellationToken)"/>
    /// <param name="entity"/><param name="hard"/><param name="cancellationToken"/>
    Task RemoveAsync(
        TEntity entity,
        bool hard = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Асинхронно удаляет коллекцию экземпляров сущности из БД.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="entities">Коллекция экземпляров сущности.</param>
    /// <param name="hard">Признак того, что сущность должна быть удалена физически.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task RemoveRangeAsync(
        IEnumerable<TEntity> entities,
        bool hard = false,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="RemoveRangeAsync(QueryOptions{TEntity}, bool, CancellationToken)"/>
    /// <param name="specification">Спецификация для отбора удаляемых сущностей.</param>
    /// <param name="hard"/><param name="cancellationToken"/>
    Task RemoveRangeAsync(
        ISpecification<TEntity> specification,
        bool hard = false,
        CancellationToken cancellationToken = default) =>
        RemoveRangeAsync(specification.BuildOptions(), hard, cancellationToken);

    /// <inheritdoc cref="RemoveRangeAsync(IEnumerable{TEntity}, bool, CancellationToken)"/>
    /// <param name="entities"/><param name="cancellationToken"/>
    /// <remarks>Сущности удаляются физически, мягкое удаление не применяется.</remarks>
    Task RemovePermanentRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Асинхронно удаляет сущности, отобранные с учётом настроек запроса.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="options">Настройки запроса. Если <see langword="null"/>, запрос выполняется без дополнительных настроек.</param>
    /// <param name="hard">Удалить сущности физически (true) или применить мягкое удаление (false).</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Задача выполнения операции удаления.</returns>
    Task RemoveRangeAsync(
        QueryOptions<TEntity> options,
        bool hard = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Асинхронно удаляет сущности, удовлетворяющие заданному условию.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="predicate">Выражение-условие для отбора удаляемых сущностей.</param>
    /// <param name="hard">Удалить сущности физически (true) или применить мягкое удаление (false).</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Задача выполнения операции удаления.</returns>
    Task RemoveRangeAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool hard = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Асинхронно физически удаляет коллекцию экземпляров сущности напрямую из БД (без предварительной загрузки экземпляров) по указанной настройке.
    /// </summary>
    /// <remarks>
    /// Имеет эффект без вызова <see cref="SaveChangesAsync"/>.
    /// </remarks>
    /// <param name="options">Настройки запроса. Если <see langword="null"/>, запрос выполняется без дополнительных настроек.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Результат выполнения асинхронной операции.</returns>
    Task ExecuteRemoveRangeAsync(
        QueryOptions<TEntity> options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Асинхронно физически удаляет коллекцию экземпляров сущности напрямую из БД (без предварительной загрузки экземпляров).
    /// </summary>
    /// <inheritdoc cref="ExecuteRemoveRangeAsync(QueryOptions{TEntity}, CancellationToken)"/>
    /// <param name="predicate">Условие выборки для последующего удаления.</param>
    /// <param name="cancellationToken"/>
    Task ExecuteRemoveRangeAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        ExecuteRemoveRangeAsync(new QueryOptions<TEntity>().AddFilter(predicate), cancellationToken);

    /// <inheritdoc cref="ExecuteRemoveRangeAsync(QueryOptions{TEntity}, CancellationToken)"/>
    /// <param name="specification">Спецификация.</param>
    /// <param name="cancellationToken"/>
    Task ExecuteRemoveRangeAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default) =>
        ExecuteRemoveRangeAsync(specification.BuildOptions(), cancellationToken);

    #endregion

    /// <summary>
    /// Выполняет синхронную операцию в контексте репозитория с возвращением результата.
    /// </summary>
    /// <typeparam name="TResult">Тип возвращаемого значения.</typeparam>
    /// <param name="process">Делегат операции для выполнения.</param>
    /// <param name="useTransaction">Выполнять операцию в рамках транзакции.</param>
    /// <returns>Результат выполнения операции.</returns>
    TResult Execute<TResult>(
        Func<TResult> process,
        bool useTransaction = false);

    /// <summary>
    /// Выполняет синхронную операцию в контексте репозитория.
    /// </summary>
    /// <inheritdoc cref="IRepository{TEntity}.Execute{TResult}"/>
    /// <param name="process"/><param name="useTransaction"/>
    void Execute(
        Action process,
        bool useTransaction = false) =>
        Execute<object?>(() =>
        {
            process();
            return null;
        });

    /// <summary>
    /// Выполняет асинхронную операцию в контексте репозитория с возвращением результата.
    /// </summary>
    /// <typeparam name="TResult">Тип возвращаемого значения.</typeparam>
    /// <param name="process">Асинхронный делегат операции для выполнения.</param>
    /// <param name="useTransaction">Выполнять операцию в рамках транзакции.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Задача выполнения операции.</returns>
    Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> process,
        bool useTransaction = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Выполняет асинхронную операцию в контексте репозитория.
    /// </summary>
    /// <inheritdoc cref="ExecuteAsync{TResult}"/>
    /// <param name="process"/><param name="useTransaction"/><param name="cancellationToken"/>
    Task ExecuteAsync(
        Func<Task> process,
        bool useTransaction = false,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync<object?>(
            async () =>
            {
                await process();
                return null;
            },
            useTransaction,
            cancellationToken);

    /// <summary>
    /// Возвращает <see cref="IQueryable{TEntity}"/> для построения запросов.
    /// </summary>
    /// <param name="options">Настройки запроса. Если <see langword="null"/>, запрос выполняется без дополнительных настроек.</param>
    /// <returns>Запрос <see cref="IQueryable{TEntity}"/> с применёнными настройками.</returns>
    IQueryable<TEntity> Set(QueryOptions<TEntity>? options = null);

    /// <summary>
    /// Синхронно сохраняет все изменения, внесённые в контекст репозитория.
    /// </summary>
    void SaveChanges();

    /// <summary>
    /// Асинхронно сохраняет все изменения, внесённые в контекст репозитория.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Задача выполнения операции сохранения.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
