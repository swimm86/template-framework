// ----------------------------------------------------------------------------------------------
// <copyright file="LifecycleActionGate.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.LifecycleAction.Interfaces;
using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.LifecycleAction;

/// <summary>
/// Реализация <see cref="ILifecycleActionGate"/>. Хранит настройки
/// активности действий перехвата жизненного цикла с приоритетом:
/// per-entity (высший) → per-key/per-phase (средний) → global (низший).
/// Не потокобезопасна.
/// </summary>
public sealed class LifecycleActionGate
    : ILifecycleActionGate
{
    private readonly Dictionary<EntityKey, HashSet<string>> _entityDisabledKeys = [];
    private readonly Dictionary<EntityKey, HashSet<LifecyclePhase>> _entityDisabledPhases = [];
    private readonly HashSet<string> _disabledKeys = [];
    private readonly HashSet<LifecyclePhase> _disabledPhases = [];

    private bool _globalEnabled = true;

    /// <inheritdoc />
    public bool IsEnabled(IEntity entity, string key, LifecyclePhase phase)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var entityKey = EntityKey.Of(entity);

        return _globalEnabled
            && !IsDisabled(entityKey, key, phase);
    }

    /// <inheritdoc />
    public void Enable() => _globalEnabled = true;

    /// <inheritdoc />
    public void Disable() => _globalEnabled = false;

    /// <inheritdoc />
    public void Enable(IReadOnlyList<string> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        foreach (var key in keys)
        {
            _disabledKeys.Remove(key);
        }
    }

    /// <inheritdoc />
    public void Disable(IReadOnlyList<string> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        foreach (var key in keys)
        {
            _disabledKeys.Add(key);
        }
    }

    /// <inheritdoc />
    public void EnableForEntity(IReadOnlyList<string> keys, IEntity entity)
    {
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentNullException.ThrowIfNull(entity);

        var key2 = EntityKey.Of(entity);
        if (!_entityDisabledKeys.TryGetValue(key2, out var disabled))
        {
            return;
        }

        foreach (var key in keys)
        {
            disabled.Remove(key);
        }

        if (disabled.Count == 0)
        {
            _entityDisabledKeys.Remove(key2);
        }
    }

    /// <inheritdoc />
    public void DisableForEntity(IReadOnlyList<string> keys, IEntity entity)
    {
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentNullException.ThrowIfNull(entity);

        var key2 = EntityKey.Of(entity);
        if (!_entityDisabledKeys.TryGetValue(key2, out var disabled))
        {
            disabled = [];
            _entityDisabledKeys[key2] = disabled;
        }

        foreach (var key in keys)
        {
            disabled.Add(key);
        }
    }

    /// <inheritdoc />
    public void EnablePhase(LifecyclePhase phase) => _disabledPhases.Remove(phase);

    /// <inheritdoc />
    public void DisablePhase(LifecyclePhase phase) => _disabledPhases.Add(phase);

    /// <inheritdoc />
    public void EnablePhaseForEntity(LifecyclePhase phase, IEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var key2 = EntityKey.Of(entity);
        if (!_entityDisabledPhases.TryGetValue(key2, out var disabled))
        {
            return;
        }

        disabled.Remove(phase);

        if (disabled.Count == 0)
        {
            _entityDisabledPhases.Remove(key2);
        }
    }

    /// <inheritdoc />
    public void DisablePhaseForEntity(LifecyclePhase phase, IEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var key2 = EntityKey.Of(entity);
        if (!_entityDisabledPhases.TryGetValue(key2, out var disabled))
        {
            disabled = [];
            _entityDisabledPhases[key2] = disabled;
        }

        disabled.Add(phase);
    }

    /// <inheritdoc />
    public void Forget(IEnumerable<IEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        foreach (var entity in entities)
        {
            ArgumentNullException.ThrowIfNull(entity);
            var key2 = EntityKey.Of(entity);
            _entityDisabledKeys.Remove(key2);
            _entityDisabledPhases.Remove(key2);
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        _globalEnabled = true;
        _disabledKeys.Clear();
        _disabledPhases.Clear();
        _entityDisabledKeys.Clear();
        _entityDisabledPhases.Clear();
    }

    private bool IsDisabled(EntityKey entityKey, string key, LifecyclePhase phase) =>
        IsKeyDisabledByEntity(entityKey, key)
        || IsPhaseDisabledByEntity(entityKey, phase)
        || _disabledKeys.Contains(key)
        || _disabledPhases.Contains(phase);

    private bool IsKeyDisabledByEntity(EntityKey entityKey, string key) =>
        _entityDisabledKeys.TryGetValue(entityKey, out var keys) && keys.Contains(key);

    private bool IsPhaseDisabledByEntity(EntityKey entityKey, LifecyclePhase phase) =>
        _entityDisabledPhases.TryGetValue(entityKey, out var phases) && phases.Contains(phase);
}
