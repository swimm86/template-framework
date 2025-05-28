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
    Equals,
    NotEquals,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Contains,        // Например, для строк
    StartsWith,
    EndsWith,
    In,              // Для списков значений
    IsNull,
    IsNotNull
}
