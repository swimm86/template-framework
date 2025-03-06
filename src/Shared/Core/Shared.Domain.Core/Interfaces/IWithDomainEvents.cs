// ----------------------------------------------------------------------------------------------
// <copyright file="IWithDomainEvents.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Enums;

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
    /// Возвращает первое доменное событие в очереди.
    /// </summary>
    /// <param name="domainEvent">Доменное событие.</param>
    /// <param name="domainEventType">Тип доменного события.</param>
    /// <returns>Признак успешного выполнения операции.</returns>
    public bool TryDequeueEvent(out IDomainEvent? domainEvent, DomainEventType domainEventType);

    /// <summary>
    /// Инициализирует очереди событий только обязательными событиями.
    /// </summary>
    public void ResetEvents();

    /// <summary>
    /// Выполняет обработку доменных событий.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/>.</param>
    /// <param name="eventType">Тип события.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат выполнения асинхронной операции.</returns>
    public async Task ProcessDomainEventsAsync(
        IServiceProvider serviceProvider,
        DomainEventType eventType,
        CancellationToken cancellationToken = default)
    {
        while (TryDequeueEvent(out var domainEvent, eventType))
        {
            await domainEvent!.ProcessAsync(serviceProvider, cancellationToken);
        }
    }
}
