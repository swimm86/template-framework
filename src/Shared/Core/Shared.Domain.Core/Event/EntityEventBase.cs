// ----------------------------------------------------------------------------------------------
// <copyright file="EntityEventBase.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Common.Extensions;
using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Event.Interfaces;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Event;

/// <summary>
/// Базовый класс событий при создании сущности, реализующей интерфейс <see cref="IEntity"/>.
/// </summary>
/// <param name="key">Ключ события.</param>
public abstract class EntityEventBase(Enum key)
    : IDomainEvent
{
    /// <summary>
    /// Признак того, что событие включено.
    /// </summary>
    private bool _enabled = true;

    /// <inheritdoc />
    public Enum Key { get; } = key;

    /// <inheritdoc />
    public async Task ProcessAsync(
        DomainEventType eventType,
        IServiceProvider serviceProvider,
        ICollection<IWithDomainEvents> entities,
        CancellationToken cancellationToken)
    {
        if (_enabled)
            await ProcessActionAsync(serviceProvider, entities, cancellationToken);

        Disable();
        DisableEntitiesEvents(eventType, entities);
    }

    /// <inheritdoc />
    public void Enable() => _enabled = true;

    /// <inheritdoc />
    public void Disable() => _enabled = false;

    /// <summary>
    /// Выполняет действия события.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/>.</param>
    /// <param name="entities">Сущности для обработки.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Результат выполнения асинхронной операции.</returns>
    protected abstract Task ProcessActionAsync(
        IServiceProvider serviceProvider,
        ICollection<IWithDomainEvents> entities,
        CancellationToken cancellationToken);

    /// <summary>
    /// Отключает события, которые обработались вместе с текущим.
    /// </summary>
    /// <param name="eventType">Тип события.</param>
    /// <param name="entities">Сущности для обработки.</param>
    protected virtual void DisableEntitiesEvents(
        DomainEventType eventType,
        ICollection<IWithDomainEvents> entities) =>
        entities.ForEach(x =>
        {
            if (x.TryGetEvent(eventType, Key, out var domainEvent))
                domainEvent.Disable();
        });
}
