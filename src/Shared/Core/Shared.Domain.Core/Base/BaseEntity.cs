// ----------------------------------------------------------------------------------------------
// <copyright file="BaseEntity.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Base;

/// <summary>
/// Абстрактный базовый класс сущности.
/// </summary>
/// <typeparam name="TKey"> Тип ключа сущности. </typeparam>
public abstract class BaseEntity<TKey> : IEntity<TKey>, IWithDomainEvents
{
    /// <summary>
    /// Потокобезопасная очередь доменных эвентов после сохранения.
    /// </summary>
    [NotMapped]
    private readonly ConcurrentQueue<IDomainEvent> _domainEventsAfterSave =
        new ConcurrentQueue<IDomainEvent>();

    /// <summary>
    /// Потокобезопасная очередь доменных эвентов до сохранения.
    /// </summary>
    [NotMapped]
    private readonly ConcurrentQueue<IDomainEvent> _domainEventsBeforeSave =
        new ConcurrentQueue<IDomainEvent>();

    /// <inheritdoc />
    public TKey Id { get; set; } = default!;

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> GetDomainEvents(DomainEventType domainEventType)
        => GetCurrentDomainEvents(domainEventType);

    /// <summary>
    /// Попытка извлечения доменного события.
    /// </summary>
    /// <param name="domainEvent">Доменное событие.</param>
    /// <param name="domainEventType">Тип доменного события.</param>
    /// <returns>Признак успешного извлечения.</returns>
    public bool TryDequeueEvent(out IDomainEvent? domainEvent, DomainEventType domainEventType)
        => domainEventType == DomainEventType.AfterSave
            ? _domainEventsAfterSave.TryDequeue(out domainEvent)
            : _domainEventsBeforeSave.TryDequeue(out domainEvent);

    /// <summary>
    /// Добавляет эвент в очередь.
    /// </summary>
    /// <param name="domainEvents">Эвент.</param>
    /// <param name="domainEventType"> Тип доменного события. </param>
    protected void AddEvent(IDomainEvent domainEvents, DomainEventType domainEventType)
    {
        GetCurrentDomainEvents(domainEventType).Enqueue(domainEvents);
    }

    /// <summary>
    /// Очищает очередь.
    /// </summary>
    /// <param name="domainEventType">Тип очереди.</param>
    protected void ClearDomainEvent(DomainEventType domainEventType)
    {
        GetCurrentDomainEvents(domainEventType).Clear();
    }

    /// <summary>
    /// Чтение доменных событий по типу.
    /// </summary>
    /// <param name="domainEventType"> Тип доменного события. </param>
    /// <returns> Коллекцию доменных событий выбранного типа. </returns>
    private ConcurrentQueue<IDomainEvent> GetCurrentDomainEvents(DomainEventType domainEventType)
        => domainEventType == DomainEventType.AfterSave ? _domainEventsAfterSave : _domainEventsBeforeSave;
}
