// ----------------------------------------------------------------------------------------------
// <copyright file="FilterBase.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Dal.Models;

namespace Shared.Application.Core.Dto.Requests;

/// <summary>
/// Базовый фильтр.
/// </summary>
public record FilterBase
{
    /// <summary>
    /// Настройки фильтрации.
    /// </summary>
    public ICollection<FilterOption>? Fields { get; init; } = [];
}
