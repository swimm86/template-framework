// ----------------------------------------------------------------------------------------------
// <copyright file="EntityBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Base;

/// <summary>
/// Абстрактный базовый класс сущности.
/// </summary>
/// <typeparam name="TKey">Тип идентификатора сущности.</typeparam>
public abstract class EntityBase<TKey>
    : IEntity<TKey>
{
    /// <inheritdoc />
    public virtual TKey Id { get; init; } = default!;
}
