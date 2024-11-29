// ----------------------------------------------------------------------------------------------
// <copyright file="IIncludable.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Dal.Specification.Interfaces;

/// <summary>
/// Кастомное представлени инклюда.
/// </summary>
/// <typeparam name="TProperty">Возвращаемый проперти.</typeparam>
public interface IIncludable<TProperty>
{
    /// <summary>
    /// Includes.
    /// </summary>
    List<string> Includes { get; }

    /// <summary>
    /// Метод добавления в массив Includes.
    /// </summary>
    /// <param name="include">Include.</param>
    void AddInclude(string include);
}
