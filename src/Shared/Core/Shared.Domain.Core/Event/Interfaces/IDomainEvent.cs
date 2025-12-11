// ----------------------------------------------------------------------------------------------
// <copyright file="IDomainEvent.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Event.Interfaces;

/// <summary>
/// Интерфейс доменного евента.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Ключ события.
    /// </summary>
    Enum Key { get; }

    /// <summary>
    /// Выполняет событие.
    /// </summary>
    /// <param name="eventType">Тип доменного события.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/>.</param>
    /// <param name="entities">Сущности для обработки.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Результат выполнения асинхронной операции.</returns>
    Task ProcessAsync(
        DomainEventType eventType,
        IServiceProvider serviceProvider,
        ICollection<IWithDomainEvents> entities,
        CancellationToken cancellationToken);

    /// <summary>
    /// Сбрасывает статус события.
    /// </summary>
    void Enable();

    /// <summary>
    /// Отключает событие.
    /// </summary>
    void Disable();
}
