// ----------------------------------------------------------------------------------------------
// <copyright file="EntityDomainEvent.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Event;

/// <summary>
/// Доменное событие сущности.
/// </summary>
public class EntityDomainEvent
    : EntityEventBase
{
    private readonly Func<IServiceProvider, CancellationToken, Task> _action;

    /// <summary>
    /// Консутруктор.
    /// </summary>
    /// <param name="action">Действие, которое реализует событие.</param>
    /// <param name="key">Ключ события.</param>
    public EntityDomainEvent(
        Enum key,
        Func<IServiceProvider, CancellationToken, Task> action)
        : base(key)
    {
        _action = action;
    }

    /// <inheritdoc />
    protected override Task ProcessActionAsync(
        IServiceProvider serviceProvider,
        ICollection<IWithDomainEvents> entities,
        CancellationToken cancellationToken)
        => _action(serviceProvider, cancellationToken);

    /// <inheritdoc />
    protected override void DisableEntitiesEvents(
        DomainEventType eventType,
        ICollection<IWithDomainEvents> entities)
    {
    }
}