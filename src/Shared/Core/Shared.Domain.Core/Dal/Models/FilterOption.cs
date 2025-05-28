// ----------------------------------------------------------------------------------------------
// <copyright file="FilterOption.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Dal.Models;

/// <summary>
/// Фильтр по полю.
/// </summary>
public record FilterOption
{
    /// <summary>
    /// Наименование поля.
    /// </summary>
    public required string FieldName { get; init; }

    /// <summary>
    /// Тип операции.
    /// </summary>
    public required FilterOperationType OperationType { get; init; }

    /// <summary>
    /// Значение.
    /// </summary>
    public required object? Value { get; init; }
}
