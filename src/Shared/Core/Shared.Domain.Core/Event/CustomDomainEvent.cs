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
public record CustomDomainEvent
    : IDomainEvent
{
    private readonly Func<IServiceProvider, CancellationToken, Task> _action;

    /// <summary>
    /// Консутруктор.
    /// </summary>
    /// <param name="action">Действие, которое реализует событие.</param>
    public CustomDomainEvent(Func<IServiceProvider, CancellationToken, Task> action)
    {
        _action = action;
    }

    /// <inheritdoc />
    public Task ProcessAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken) =>
        _action(serviceProvider, cancellationToken);
}
