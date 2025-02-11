// ----------------------------------------------------------------------------------------------
// <copyright file="IIncludable.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Dal.Specification.Interfaces;

/// <summary>
/// Интерфейс для кастомной навигации.
/// </summary>
/// <typeparam name="TEntity">Начальная сущность</typeparam>
/// <typeparam name="TCurrent">Текущая сущность</typeparam>
/// <typeparam name="TNext">Свойство</typeparam>
public interface IIncludable<TEntity, TCurrent, TNext>
{
    /// <summary>
    /// Хранит в себе все навигационные свойства.
    /// </summary>
    List<IncludeNode> Includes { get; }

    /// <summary>
    /// Перегрузка AddInclude для IEnumerable.
    /// </summary>
    /// <typeparam name="TProperty">Параметр навигационного свойства.</typeparam>
    /// <param name="expression">Навигационное свойство.</param>
    /// <returns>Следующий экземпляр IIncludable.</returns>
    IIncludable<TEntity, TEntity, TProperty> AddInclude<TProperty>(
        Expression<Func<TEntity, IEnumerable<TProperty>>> expression);

    /// <summary>
    /// Перегрузка AddInclude для TProperty.
    /// </summary>
    /// <typeparam name="TProperty">Параметр навигационного свойства.</typeparam>
    /// <param name="expression">Навигационное свойство.</param>
    /// <returns>Следующий экземпляр IIncludable.</returns>
    IIncludable<TEntity, TEntity, TProperty> AddInclude<TProperty>(
        Expression<Func<TEntity, TProperty>> expression)
        where TProperty : IEntity;

    /// <summary>
    /// Перегрузка ThenInclude для IEnumerable.
    /// </summary>
    /// <typeparam name="TProperty">Параметр навигационного свойства.</typeparam>
    /// <param name="expression">Навигационное свойство.</param>
    /// <returns>Следующий экземпляр IIncludable.</returns>
    IIncludable<TEntity, TNext, TProperty> ThenInclude<TProperty>(
        Expression<Func<TNext, IEnumerable<TProperty>>> expression);

    /// <summary>
    /// Перегрузка ThenInclude для сущности.
    /// </summary>
    /// <typeparam name="TProperty">Параметр навигационного свойства.</typeparam>
    /// <param name="expression">Навигационное свойство.</param>
    /// <returns>Следующий экземпляр IIncludable.</returns>
    IIncludable<TEntity, TNext, TProperty> ThenInclude<TProperty>(
        Expression<Func<TNext, TProperty>> expression)
        where TProperty : IEntity;
}
