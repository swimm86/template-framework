// ----------------------------------------------------------------------------------------------
// <copyright file="TypeDomainEvent.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Event;

/// <summary>
/// Доменное событие сущностей типа.
/// </summary>
public class TypeDomainEvent
    : EntityEventBase
{
    private readonly Func<IServiceProvider, ICollection<IWithDomainEvents>, CancellationToken, Task> _action;

    /// <summary>
    /// Консутруктор.
    /// </summary>
    /// <param name="action">Действие, которое реализует событие.</param>
    /// <param name="key">Ключ события.</param>
    public TypeDomainEvent(
        Enum key,
        Func<IServiceProvider, ICollection<IWithDomainEvents>, CancellationToken, Task> action)
        : base(key)
    {
        _action = action;
    }

    /// <inheritdoc />
    protected override Task ProcessActionAsync(
        IServiceProvider serviceProvider,
        ICollection<IWithDomainEvents> entities,
        CancellationToken cancellationToken)
        => _action(serviceProvider, entities, cancellationToken);
}
