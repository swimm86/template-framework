// ----------------------------------------------------------------------------------------------
// <copyright file="BaseEntity.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Shared.Common.Extensions;
using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Event.Interfaces;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Base;

/// <summary>
/// Абстрактный базовый класс сущности.
/// </summary>
/// <typeparam name="TKey"> Тип ключа сущности. </typeparam>
public abstract class BaseEntity<TKey> : IEntity<TKey>, IWithDomainEvents
{
    /// <summary>
    /// Словарь доменных эвентов до сохранения.
    /// </summary>
    [NotMapped]
    private ReadOnlyDictionary<Enum, IDomainEvent> _domainEventsBeforeSave = default!;

    /// <summary>
    /// Словарь доменных эвентов после сохранения.
    /// </summary>
    [NotMapped]
    private ReadOnlyDictionary<Enum, IDomainEvent> _domainEventsAfterSave = default!;

    /// <inheritdoc />
    public virtual TKey Id { get; set; } = default!;

    /// <inheritdoc />
    public virtual string[] RequiredToSaveNavigationPropertiesNames => [];

    /// <summary>
    /// События, выполняемые перед сохранением.
    /// </summary>
    protected virtual IDomainEvent[] BeforeSaveEvents => [];

    /// <summary>
    /// События, выполняемые после сохранения.
    /// </summary>
    protected virtual IDomainEvent[] AfterSaveEvents => [];

    /// <summary>
    /// Консруктор.
    /// </summary>
    protected BaseEntity()
    {
        CreateEvents();
    }

    /// <inheritdoc />
    public bool TryGetEvent(
        DomainEventType domainEventType,
        Enum key,
        [MaybeNullWhen(false)] out IDomainEvent domainEvent)
        => GetCurrentDomainEvents(domainEventType).TryGetValue(key, out domainEvent);

    /// <inheritdoc />
    public void ResetEvents() =>
        EnableDomainEvents();

    /// <inheritdoc />
    public ICollection<Enum> GetAllKeys(DomainEventType domainEventType)
        => GetCurrentDomainEvents(domainEventType).Keys;

    /// <summary>
    /// Отключает доменные события.
    /// </summary>
    public void DisableDomainEvents()
    {
        DisableDomainEvents(DomainEventType.BeforeSave);
        DisableDomainEvents(DomainEventType.AfterSave);
    }

    /// <summary>
    /// Отключает доменные события.
    /// </summary>
    /// <param name="domainEventType">Тип доменного события.</param>
    /// <param name="flags">Флаги события (если <see langword="null"/>, то берутся все события типа).</param>
    public void DisableDomainEvents(DomainEventType domainEventType, Enum? flags = default)
        => UpdateDomainEvents(domainEventType, flags, x => x.Disable());

    /// <summary>
    /// Включает доменные события.
    /// </summary>
    public void EnableDomainEvents()
    {
        EnableDomainEvents(DomainEventType.BeforeSave);
        EnableDomainEvents(DomainEventType.AfterSave);
    }

    /// <summary>
    /// Включает доменные события.
    /// </summary>
    /// <param name="domainEventType">Тип доменного события.</param>
    /// <param name="flags">Флаги события (если <see langword="null"/>, то берутся все события типа).</param>
    public void EnableDomainEvents(DomainEventType domainEventType, Enum? flags = default)
        => UpdateDomainEvents(domainEventType, flags, x => x.Enable());

    /// <summary>
    /// Создает доменные события.
    /// </summary>
    private void CreateEvents()
    {
        _domainEventsBeforeSave = new ReadOnlyDictionary<Enum, IDomainEvent>(BeforeSaveEvents.ToDictionary(x => x.Key));
        _domainEventsAfterSave = new ReadOnlyDictionary<Enum, IDomainEvent>(AfterSaveEvents.ToDictionary(x => x.Key));
    }

    /// <summary>
    /// Обновляет доменные события.
    /// </summary>
    /// <param name="domainEventType">Тип доменного события.</param>
    /// <param name="flags">Флаги события (если <see langword="null"/>, то берутся все события типа).</param>
    /// <param name="eventAction">Действие над событием.</param>
    private void UpdateDomainEvents(DomainEventType domainEventType, Enum? flags, Action<IDomainEvent> eventAction)
    {
        var events = GetCurrentDomainEvents(domainEventType);

        events.Where(x => flags?.HasFlag(x.Key) ?? true)
            .ForEach(x => eventAction(x.Value));
    }

    /// <summary>
    /// Чтение доменных событий по типу.
    /// </summary>
    /// <param name="domainEventType"> Тип доменного события. </param>
    /// <returns> Коллекцию доменных событий выбранного типа. </returns>
    private ReadOnlyDictionary<Enum, IDomainEvent> GetCurrentDomainEvents(DomainEventType domainEventType)
        => domainEventType == DomainEventType.AfterSave ? _domainEventsAfterSave : _domainEventsBeforeSave;
}
