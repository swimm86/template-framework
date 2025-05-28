// ----------------------------------------------------------------------------------------------
// <copyright file="SortOption.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Dal.Models;

/// <summary>
/// Модель сортировки.
/// </summary>
/// <param name="Key">Ключ по которому будет идти сортировка.</param>
/// <param name="DirectionType">Сторона сортировки.</param>
public record SortOption(string Key, OrderDirectionType DirectionType);
