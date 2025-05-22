// ----------------------------------------------------------------------------------------------
// <copyright file="SortOption.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Dal.Models;

/// <summary>
/// Модель сортировки.
/// </summary>
/// <param name="key">Ключ по которому будет идти сортировка.</param>
/// <param name="directionType">Сторона сортировки.</param>
public class SortOption(string key, OrderDirectionType directionType)
{
    /// <summary>
    /// Ключ.
    /// </summary>
    public string Key { get; } = key;

    /// <summary>
    /// Направление.
    /// </summary>
    public OrderDirectionType DirectionType { get; } = directionType;
}
