// ----------------------------------------------------------------------------------------------
// <copyright file="Enums.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.ComponentModel;

namespace Shared.Domain.Core.Dal;

/// <summary>
/// Перечислевние направлений сортировки
/// </summary>
public enum OrderDirectionType
{
    /// <summary>
    /// Направление сортировки по возрастанию
    /// </summary>
    [Description("asc")]
    Ascending,

    /// <summary>
    /// Направление сортировки по убыванию
    /// </summary>
    [Description("desc")]
    Descending,
}

/// <summary>
/// Операции для фильтрации.
/// </summary>
public enum FilterOperationType
{
    /// <summary>
    /// Равно.
    /// </summary>
    Equals,

    /// <summary>
    /// Не равно.
    /// </summary>
    NotEquals,

    /// <summary>
    /// Больше, чем.
    /// </summary>
    GreaterThan,

    /// <summary>
    /// Больше, чем, или равно.
    /// </summary>
    GreaterThanOrEqual,

    /// <summary>
    /// Меньше, чем.
    /// </summary>
    LessThan,

    /// <summary>
    /// Меньше, чем, или равно.
    /// </summary>
    LessThanOrEqual,

    /// <summary>
    /// Содержится.
    /// </summary>
    /// <remarks>Подходит для строк.</remarks>
    Contains,

    /// <summary>
    /// Начинается с.
    /// </summary>
    StartsWith,

    /// <summary>
    /// Заканчивается на.
    /// </summary>
    EndsWith,

    /// <summary>
    /// Содержится в коллекции.
    /// </summary>
    /// <remarks>Подходит для списков значений.</remarks>
    In,

    /// <summary>
    /// Равно <see langword="null"/>.
    /// </summary>
    IsNull,

    /// <summary>
    /// Ну равно <see langword="null"/>.
    /// </summary>
    IsNotNull
}
