// ----------------------------------------------------------------------------------------------
// <copyright file="LifecycleEntityRegistry.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.LifecycleAction.Interfaces;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.LifecycleAction;

/// <summary>
/// Хранит отслеживаемые сущности в словаре по <see cref="EntityKey"/>.
/// Не потокобезопасен: предполагается использование в рамках единого
/// scope (scoped-lifetime в DI).
/// </summary>
public sealed class LifecycleEntityRegistry
    : ILifecycleEntityRegistry
{
    private readonly Dictionary<EntityKey, IEntity> _entities = [];

    /// <inheritdoc />
    public void Track(IEnumerable<IEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        foreach (var entity in entities)
        {
            ArgumentNullException.ThrowIfNull(entity, nameof(entities));
            _entities[EntityKey.Of(entity)] = entity;
        }
    }

    /// <inheritdoc />
    public void Untrack(IEnumerable<IEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        foreach (var entity in entities)
        {
            ArgumentNullException.ThrowIfNull(entity, nameof(entities));
            _entities.Remove(EntityKey.Of(entity));
        }
    }

    /// <inheritdoc />
    public IReadOnlyCollection<IEntity> Snapshot() => _entities.Values.ToArray();

    /// <inheritdoc />
    public void Clear() => _entities.Clear();
}
