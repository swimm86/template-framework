// ----------------------------------------------------------------------------------------------
// <copyright file="BaseEntity.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Common.Extensions;
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
    /// События, которые обьязательно должны быть выполнены до сохранения.
    /// </summary>
    protected virtual IDomainEvent[] RequiredEventsBeforeSave { get; } = [];

    /// <summary>
    /// События, которые обьязательно должны быть выполнены после сохранения.
    /// </summary>
    protected virtual IDomainEvent[] RequiredEventsAfterSave { get; } = [];

    /// <summary>
    /// Консруктор.
    /// </summary>
    protected BaseEntity()
    {
        ResetEvents();
    }

    /// <summary>
    /// Потокобезопасная очередь доменных эвентов до сохранения.
    /// </summary>
    [NotMapped]
    private readonly ConcurrentQueue<IDomainEvent> _domainEventsBeforeSave = [];

    /// <summary>
    /// Потокобезопасная очередь доменных эвентов после сохранения.
    /// </summary>
    [NotMapped]
    private readonly ConcurrentQueue<IDomainEvent> _domainEventsAfterSave = [];

    /// <inheritdoc />
    public TKey Id { get; set; } = default!;

    /// <inheritdoc />
    public virtual string[] RequiredToSaveNavigationPropertiesNames => [];

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

    /// <inheritdoc />
    public void ResetEvents()
    {
        ClearDomainEvent(DomainEventType.BeforeSave);
        ClearDomainEvent(DomainEventType.AfterSave);
        RequiredEventsBeforeSave.ForEach(e => AddEvent(e, DomainEventType.BeforeSave));
        RequiredEventsAfterSave.ForEach(e => AddEvent(e, DomainEventType.AfterSave));
    }

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
