// ----------------------------------------------------------------------------------------------
// <copyright file="IScheduledJobExecutor.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Job.Pipeline.Interfaces;

/// <summary>
/// Исполнитель фоновой задачи. Собирает pipeline из зарегистрированных middleware
/// и применяет его к переданному <see cref="ScheduledJobContext"/>.
/// </summary>
public interface IScheduledJobExecutor
{
    /// <summary>
    /// Выполняет задачу через pipeline middleware.
    /// </summary>
    /// <param name="context">Контекст выполнения.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    Task ExecuteAsync(ScheduledJobContext context);
}
