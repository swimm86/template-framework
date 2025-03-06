// ----------------------------------------------------------------------------------------------
// <copyright file="IDomainEvent.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Interfaces;

/// <summary>
/// Интерфейс доменного евента
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Выплняет евент.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/>.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат выполнения асинхронной операции.</returns>
    Task ProcessAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken);
}
