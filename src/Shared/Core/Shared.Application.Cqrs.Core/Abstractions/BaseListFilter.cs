// ----------------------------------------------------------------------------------------------
// <copyright file="BaseListFilter.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dto.Interfaces;

namespace Shared.Application.Cqrs.Core.Abstractions;

/// <summary>
/// Базовый фильтр для сущностей с идентификаторами.
/// </summary>
public abstract record ListFilterBase : IWithIdsFilter<Guid>
{
    /// <inheritdoc />
    public ICollection<Guid>? Ids { get; init; }
}