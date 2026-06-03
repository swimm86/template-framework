// ----------------------------------------------------------------------------------------------
// <copyright file="IScheduledJobMiddleware.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Job.Pipeline.Interfaces;

/// <summary>
/// Middleware pipeline для фоновой задачи.
/// Реализации — cross-cutting concerns (logging, correlation, retry, etc.).
/// </summary>
public interface IScheduledJobMiddleware
{
    /// <summary>
    /// Выполняет middleware и вызывает <paramref name="next"/> для передачи управления дальше.
    /// </summary>
    /// <param name="context">Контекст выполнения задачи.</param>
    /// <param name="next">Следующий делегат в pipeline.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    Task InvokeAsync(ScheduledJobContext context, ScheduledJobDelegate next);
}
