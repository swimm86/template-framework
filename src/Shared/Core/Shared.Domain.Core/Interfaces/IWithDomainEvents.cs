// ----------------------------------------------------------------------------------------------
// <copyright file="IWithDomainEvents.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Enums;

namespace Shared.Domain.Core.Interfaces;

/// <summary>
/// Предоставляет интерфейс для чтения доменных эвентов.
/// </summary>
public interface IWithDomainEvents
{
    /// <summary>
    /// Чтение доменных событий по типу.
    /// </summary>
    /// <param name="domainEventType"> Тип доменного события. </param>
    /// <returns> Коллекцию доменных событий выбранного типа. </returns>
    public IReadOnlyCollection<IDomainEvent> GetDomainEvents(DomainEventType domainEventType);

    /// <summary>
    /// Попытка прочитать и убрать элемент с начала очереди.
    /// </summary>
    /// <param name="domainEvent"> Доменное событие. </param>
    /// <param name="domainEventType"> Тип доменного события. </param>
    /// <returns> Признак успешного выполнения операции. </returns>
    public bool TryDequeueEvent(out IDomainEvent? domainEvent, DomainEventType domainEventType);
}
