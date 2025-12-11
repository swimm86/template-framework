// ----------------------------------------------------------------------------------------------
// <copyright file="IWithDomainEvents.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Shared.Common.Extensions;
using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Event.Interfaces;

namespace Shared.Domain.Core.Interfaces;

/// <summary>
/// Предоставляет интерфейс для чтения доменных событий.
/// </summary>
public interface IWithDomainEvents
{
    /// <summary>
    /// Имена свойств со связанными сущностями, которые необходимы для сохранения.
    /// </summary>
    string[] RequiredToSaveNavigationPropertiesNames { get; }

    /// <summary>
    /// Попытка извлечения доменного события.
    /// </summary>
    /// <param name="domainEventType">Тип доменного события.</param>
    /// <param name="key">Ключ события.</param>
    /// <param name="domainEvent">Доменное событие.</param>
    /// <returns>Признак успешного выполнения операции.</returns>
    public bool TryGetEvent(
        DomainEventType domainEventType,
        Enum key,
        [MaybeNullWhen(false)] out IDomainEvent domainEvent);

    /// <summary>
    /// Инициализирует очереди событий только обязательными событиями.
    /// </summary>
    public void ResetEvents();

    /// <summary>
    /// Получает все ключи доменных событий заданного типа.
    /// </summary>
    /// <param name="domainEventType">Тип доменного события.</param>
    /// <returns>Все ключи доменных событий заданного типа.</returns>
    public ICollection<Enum> GetAllKeys(DomainEventType domainEventType);

    /// <summary>
    /// Выполняет обработку доменных событий.
    /// </summary>
    /// <param name="eventType">Тип события.</param>
    /// <param name="key">Ключ события.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/>.</param>
    /// <param name="entities">Сущности типа.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Результат выполнения асинхронной операции.</returns>
    public async Task ProcessDomainEventAsync(
        DomainEventType eventType,
        Enum key,
        IServiceProvider serviceProvider,
        ICollection<IWithDomainEvents>? entities = default,
        CancellationToken cancellationToken = default)
    {
        entities ??= [];

        if (TryGetEvent(eventType, key, out var domainEvent))
        {
            await domainEvent.ProcessAsync(eventType, serviceProvider, entities, cancellationToken);
        }
    }

    /// <summary>
    /// Выполняет обработку доменных событий.
    /// </summary>
    /// <param name="eventType">Тип события.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/>.</param>
    /// <param name="entities">Сущности типа.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Результат выполнения асинхронной операции.</returns>
    public Task ProcessDomainEventsAsync(
        DomainEventType eventType,
        IServiceProvider serviceProvider,
        ICollection<IWithDomainEvents>? entities = default,
        CancellationToken cancellationToken = default) =>
        GetAllKeys(eventType).ForeachAsync(
            key => ProcessDomainEventAsync(eventType, key, serviceProvider, entities, cancellationToken),
            cancellationToken);
}
