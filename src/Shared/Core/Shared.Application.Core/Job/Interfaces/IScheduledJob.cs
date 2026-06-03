// ----------------------------------------------------------------------------------------------
// <copyright file="IScheduledJob.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Job.Interfaces;

/// <summary>
/// Интерфейс фоновой задачи, выполняемой по расписанию.
/// Реализация не должна зависеть от конкретных планировщиков (Quartz, Hangfire)
/// и не должна получать <see cref="IServiceProvider"/> в конструкторе — используйте
/// внедрение зависимостей через конструктор.
/// </summary>
public interface IScheduledJob
{
    /// <summary>
    /// Выполняет задачу.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
