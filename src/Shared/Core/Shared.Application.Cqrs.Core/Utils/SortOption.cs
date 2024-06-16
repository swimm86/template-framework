// ----------------------------------------------------------------------------------------------
// <copyright file="SortOption.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dal;

namespace Shared.Application.Cqrs.Core.Utils;

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
