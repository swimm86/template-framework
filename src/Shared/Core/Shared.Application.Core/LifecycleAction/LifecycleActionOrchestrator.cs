// ----------------------------------------------------------------------------------------------
// <copyright file="LifecycleActionOrchestrator.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.LifecycleAction.Interfaces;
using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.LifecycleAction;

/// <summary>
/// Фасад над <see cref="ILifecycleEntityRegistry"/>,
/// <see cref="ILifecycleActionGate"/> и коллекцией
/// <see cref="ILifecycleActionHandler"/>. Не хранит собственного
/// состояния, кроме координации.
/// </summary>
/// <param name="handlers">Коллекция обработчиков действий перехвата жизненного цикла.</param>
/// <param name="registry">Реестр отслеживаемых сущностей.</param>
/// <param name="gate">Хранилище настроек активности действий.</param>
public class LifecycleActionOrchestrator(
    IEnumerable<ILifecycleActionHandler> handlers,
    ILifecycleEntityRegistry registry,
    ILifecycleActionGate gate)
    : ILifecycleActionOrchestrator
{
    /// <inheritdoc />
    public void AddEntities(IEnumerable<IEntity> entities) => registry.Track(entities);

    /// <inheritdoc />
    public void RemoveEntities(IEnumerable<IEntity> entities)
    {
        registry.Untrack(entities);
        gate.Forget(entities);
    }

    /// <inheritdoc />
    public string[] GetRequiredProperties(Type entityType)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        return handlers
            .Where(handler => handler.EntityType == entityType)
            .SelectMany(handler => handler.RequiredNavigationProperties)
            .Distinct()
            .ToArray();
    }

    /// <inheritdoc />
    public bool IsActionEnabled(IEntity entity, string key, LifecyclePhase phase) =>
        gate.IsEnabled(entity, key, phase);

    /// <inheritdoc />
    public void EnableActions() => gate.Enable();

    /// <inheritdoc />
    public void DisableActions() => gate.Disable();

    /// <inheritdoc />
    public void EnableActions(IReadOnlyList<string> keys) => gate.Enable(keys);

    /// <inheritdoc />
    public void DisableActions(IReadOnlyList<string> keys) => gate.Disable(keys);

    /// <inheritdoc />
    public void EnableActionForEntity(string key, IEntity entity) =>
        gate.EnableForEntity([key], entity);

    /// <inheritdoc />
    public void DisableActionForEntity(string key, IEntity entity) =>
        gate.DisableForEntity([key], entity);

    /// <inheritdoc />
    public void EnableActionsForEntities(IReadOnlyList<string> keys, IReadOnlyList<IEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        foreach (var entity in entities)
        {
            gate.EnableForEntity(keys, entity);
        }
    }

    /// <inheritdoc />
    public void DisableActionsForEntities(IReadOnlyList<string> keys, IReadOnlyList<IEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        foreach (var entity in entities)
        {
            gate.DisableForEntity(keys, entity);
        }
    }

    /// <inheritdoc />
    public void EnablePhase(LifecyclePhase phase) => gate.EnablePhase(phase);

    /// <inheritdoc />
    public void DisablePhase(LifecyclePhase phase) => gate.DisablePhase(phase);

    /// <inheritdoc />
    public void EnablePhaseForEntity(LifecyclePhase phase, IEntity entity) =>
        gate.EnablePhaseForEntity(phase, entity);

    /// <inheritdoc />
    public void DisablePhaseForEntity(LifecyclePhase phase, IEntity entity) =>
        gate.DisablePhaseForEntity(phase, entity);

    /// <inheritdoc />
    public async Task DispatchAsync(
        LifecyclePhase phase,
        CancellationToken cancellationToken)
    {
        var phaseHandlers = handlers
            .Where(h => h.Phase == phase)
            .OrderBy(h => h.Order)
            .ToList();

        if (phaseHandlers.Count == 0)
        {
            return;
        }

        var snapshot = registry.Snapshot();
        foreach (var handler in phaseHandlers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var eligibleEntities = snapshot
                .Where(e => handler.EntityType.IsInstanceOfType(e)
                    && gate.IsEnabled(e, handler.Key, phase))
                .ToArray();

            if (eligibleEntities.Length == 0)
            {
                continue;
            }

            await handler.ExecuteAsync(eligibleEntities, cancellationToken);
        }
    }

    /// <inheritdoc />
    public void ResetAllActions() => gate.Reset();
}
