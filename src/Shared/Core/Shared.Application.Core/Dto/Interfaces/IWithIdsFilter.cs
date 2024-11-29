// ----------------------------------------------------------------------------------------------
// <copyright file="IWithIdsFilter.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Dto.Interfaces;

/// <summary>
/// Интерфейс для фильтрации сущностей по идентификаторам.
/// </summary>
/// <typeparam name="TKey">Тип идентификатора.</typeparam>
public interface IWithIdsFilter<TKey>
{
    /// <summary>
    /// Идентификаторы сущностей.
    /// </summary>
    public ICollection<TKey>? Ids { get; init; }
}
