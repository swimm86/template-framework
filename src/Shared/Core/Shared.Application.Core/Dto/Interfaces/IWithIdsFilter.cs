// ----------------------------------------------------------------------------------------------
// <copyright file="IWithIdsFilter.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Dto.Interfaces;

/// <summary>
/// Интерфейс для фильтрации сущностей по идентификаторам.
/// </summary>
public interface IWithIdsFilter
{
    /// <summary>
    /// Идентификаторы сущностей.
    /// </summary>
    public ICollection<object>? Ids { get; init; }
}

/// <summary>
/// Интерфейс для фильтрации сущностей по идентификаторам.
/// </summary>
/// <typeparam name="TKey">Тип идентификатора.</typeparam>
public interface IWithIdsFilter<TKey> : IWithIdsFilter
{
    ICollection<object>? IWithIdsFilter.Ids
    {
        get => Ids?.OfType<object>().ToArray();
        init => Ids = value?.OfType<TKey>().ToArray();
    }
    /// <summary>
    /// Идентификаторы сущностей.
    /// </summary>
    public new ICollection<TKey>? Ids { get; init; }
}
