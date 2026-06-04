// ----------------------------------------------------------------------------------------------
// <copyright file="SignalJob.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Job.Interfaces;

namespace Shared.Testing.Job;

/// <summary>
/// Тестовая джоба, которая через <see cref="TaskCompletionSource{TResult}"/>
/// сигнализирует о вызове <see cref="IScheduledJob.ExecuteAsync"/>.
/// <para>
/// Используется в интеграционных тестах адаптеров Job Scheduler (Quartz, Hangfire):
/// тест регистрирует <see cref="SignalJob"/> в DI, планирует её через
/// <c>AddJobs(opts =&gt; opts.AddJob&lt;SignalJob&gt;(new JobSchedule.OnStartup()))</c>
/// и ждёт <see cref="ExecuteCalled"/> после старта bootstrapper-а. Это позволяет
/// поймать регрессию, при которой адаптер не подключён к DI и джоба не
/// выполняется фактически (а не только регистрируется).
/// </para>
/// </summary>
public sealed class SignalJob : IScheduledJob
{
    private readonly TaskCompletionSource _executeCalled = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// <see cref="Task"/>, который завершается при вызове <see cref="ExecuteAsync"/>.
    /// </summary>
    public Task ExecuteCalled => _executeCalled.Task;

    /// <inheritdoc />
    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _executeCalled.TrySetResult();
        return Task.CompletedTask;
    }
}
