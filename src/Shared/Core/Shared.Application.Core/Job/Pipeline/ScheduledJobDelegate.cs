// ----------------------------------------------------------------------------------------------
// <copyright file="ScheduledJobDelegate.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Job.Pipeline;

/// <summary>
/// Делегат, представляющий следующий шаг pipeline выполнения задачи.
/// </summary>
/// <param name="context">Контекст выполнения задачи.</param>
/// <returns>Задача, представляющая асинхронную операцию.</returns>
public delegate Task ScheduledJobDelegate(ScheduledJobContext context);
