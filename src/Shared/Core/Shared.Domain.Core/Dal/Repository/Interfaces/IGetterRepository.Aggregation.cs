// ----------------------------------------------------------------------------------------------
// <copyright file="IGetterRepository.Aggregation.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Dal.Specification.Interfaces;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Dal.Repository.Interfaces;

/// <inheritdoc cref="IGetterRepository{TEntity}"/>
public partial interface IGetterRepository<TEntity>
    where TEntity : class, IEntity
{
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

    /// <inheritdoc cref="IGetterRepository{TEntity}.CountGroupsAsync{TKey}(Expression{Func{TEntity, TKey}}, QueryOptions{TEntity}?, CancellationToken)"/>
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
    /// <inheritdoc cref="IGetterRepository{TEntity}.MaxAsync{TOut}(Expression{Func{TEntity, TOut}}, CancellationToken)"/>
    /// <param name="selector"/><param name="cancellationToken"/>
    Task<TOut?> MaxAsync<TOut>(
        Expression<Func<TEntity, TOut>> selector,
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="IGetterRepository{TEntity}.MaxAsync{TOut}(Expression{Func{TEntity, TOut}}, QueryOptions{TEntity}?, CancellationToken)"/>
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
    /// <inheritdoc cref="IGetterRepository{TEntity}.MinAsync{TOut}(Expression{Func{TEntity, TOut}}, CancellationToken)"/>
    /// <param name="selector"/><param name="cancellationToken"/>
    Task<TOut?> MinAsync<TOut>(
        Expression<Func<TEntity, TOut>> selector,
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="IGetterRepository{TEntity}.MinAsync{TOut}(Expression{Func{TEntity, TOut}}, QueryOptions{TEntity}?, CancellationToken)"/>
    /// <param name="specification">Спецификация.</param>
    /// <param name="selector"/><param name="cancellationToken"/><typeparam name="TOut"/>
    Task<TOut?> MinAsync<TOut>(
        Expression<Func<TEntity, TOut>> selector,
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default) =>
        MinAsync(selector, specification.BuildOptions(), cancellationToken);
}
