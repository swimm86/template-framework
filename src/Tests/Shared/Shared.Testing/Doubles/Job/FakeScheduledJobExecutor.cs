// ----------------------------------------------------------------------------------------------
// <copyright file="FakeScheduledJobExecutor.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Job.Pipeline;
using Shared.Application.Core.Job.Pipeline.Interfaces;

namespace Shared.Testing.Doubles.Job;

/// <summary>
/// Тестовая реализация <see cref="IScheduledJobExecutor"/>, фиксирующая контексты выполнения.
/// </summary>
public sealed class FakeScheduledJobExecutor : IScheduledJobExecutor
{
    private readonly List<ScheduledJobContext> _capturedContexts = [];

    /// <summary>
    /// Получает последний переданный контекст выполнения.
    /// </summary>
    public ScheduledJobContext? LastContext { get; private set; }

    /// <summary>
    /// Получает все переданные контексты выполнения.
    /// </summary>
    public IReadOnlyList<ScheduledJobContext> CapturedContexts => _capturedContexts;

    /// <summary>
    /// Получает количество вызовов метода выполнения.
    /// </summary>
    public int ExecuteAsyncCallCount { get; private set; }

    /// <summary>
    /// Получает или задаёт исключение, выбрасываемое при выполнении.
    /// </summary>
    public Exception? ExceptionToThrowOnExecuteAsync { get; set; }

    /// <summary>
    /// Фиксирует контекст и выполняет настроенное поведение.
    /// </summary>
    /// <param name="context">Контекст выполнения фоновой задачи.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    /// <exception cref="Exception">
    /// Выбрасывается, если задано свойство <see cref="ExceptionToThrowOnExecuteAsync"/>.
    /// </exception>
    public Task ExecuteAsync(ScheduledJobContext context)
    {
        ExecuteAsyncCallCount++;
        LastContext = context;
        _capturedContexts.Add(context);

        if (ExceptionToThrowOnExecuteAsync is not null)
        {
            throw ExceptionToThrowOnExecuteAsync;
        }

        return Task.CompletedTask;
    }
}
