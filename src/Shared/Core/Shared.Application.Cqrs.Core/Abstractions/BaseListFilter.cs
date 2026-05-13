// ----------------------------------------------------------------------------------------------
// <copyright file="BaseListFilter.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
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