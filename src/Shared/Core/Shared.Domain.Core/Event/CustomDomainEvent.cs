// ----------------------------------------------------------------------------------------------
// <copyright file="CustomDomainEvent.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Event;

/// <summary>
/// Кастомное доменное событие.
/// </summary>
public class CustomDomainEvent(
    Enum key,
    Func<IServiceProvider, ICollection<IWithDomainEvents>, CancellationToken, Task> action)
    : EntityEventBase(key)
{
    /// <inheritdoc />
    protected override Task ProcessActionAsync(
        IServiceProvider serviceProvider,
        ICollection<IWithDomainEvents> entities,
        CancellationToken cancellationToken) =>
        action(serviceProvider, entities, cancellationToken);
}
