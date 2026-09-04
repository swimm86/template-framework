// ----------------------------------------------------------------------------------------------
// <copyright file="FakeJobScheduler.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Threading;
using Shared.Application.Core.Job.Scheduler;
using Shared.Application.Core.Job.Scheduler.Interfaces;

namespace Shared.Testing.Doubles.Job;

/// <summary>
/// Тестовая реализация <see cref="IJobScheduler"/>, фиксирующая успешные регистрации задач.
/// </summary>
public sealed class FakeJobScheduler : IJobScheduler
{
    private readonly ConcurrentQueue<(JobDefinition Definition, CancellationToken Token)> _scheduleAsyncInvocations = [];
    private int _scheduleAsyncCallCount;

    /// <summary>
    /// Получает список успешных вызовов регистрации задач.
    /// </summary>
    public IReadOnlyList<(JobDefinition Definition, CancellationToken Token)> ScheduleAsyncInvocations =>
        _scheduleAsyncInvocations.ToArray();

    /// <summary>
    /// Получает количество вызовов метода регистрации задач.
    /// </summary>
    public int ScheduleAsyncCallCount => Volatile.Read(ref _scheduleAsyncCallCount);

    /// <summary>
    /// Получает или задаёт исключение, выбрасываемое при регистрации любой задачи.
    /// </summary>
    public Exception? ExceptionToThrowOnScheduleAsync { get; set; }

    /// <summary>
    /// Получает или задаёт функцию выбора исключения по ключу задачи.
    /// </summary>
    public Func<string, Exception?>? ExceptionForJobKey { get; set; }

    /// <summary>
    /// Регистрирует задачу или выбрасывает настроенное исключение.
    /// </summary>
    /// <param name="definition">Описание задачи.</param>
    /// <param name="ct"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    /// <exception cref="Exception">
    /// Выбрасывается, если функция <see cref="ExceptionForJobKey"/> или свойство
    /// <see cref="ExceptionToThrowOnScheduleAsync"/> возвращает настроенное исключение.
    /// </exception>
    public Task ScheduleAsync(JobDefinition definition, CancellationToken ct = default)
    {
        Interlocked.Increment(ref _scheduleAsyncCallCount);

        var exceptionForJobKey = ExceptionForJobKey?.Invoke(definition.JobKey);
        if (exceptionForJobKey is not null)
        {
            throw exceptionForJobKey;
        }

        if (ExceptionToThrowOnScheduleAsync is not null)
        {
            throw ExceptionToThrowOnScheduleAsync;
        }

        _scheduleAsyncInvocations.Enqueue((definition, ct));
        return Task.CompletedTask;
    }
}
