// ----------------------------------------------------------------------------------------------
// <copyright file="Enums.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.ComponentModel;

namespace Shared.Domain.Core.Dal;

/// <summary>
/// Перечисление направлений сортировки.
/// </summary>
public enum OrderDirectionType
{
    /// <summary>
    /// Сортировка по возрастанию.
    /// </summary>
    [Description("asc")]
    Ascending,

    /// <summary>
    /// Сортировка по убыванию.
    /// </summary>
    [Description("desc")]
    Descending,
}
