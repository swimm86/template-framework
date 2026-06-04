// ----------------------------------------------------------------------------------------------
// <copyright file="FakeScheduledJob.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Job.Interfaces;
using Shared.Application.Core.Job.Scheduler;

namespace Shared.Testing.Job;

/// <summary>
/// Минимальный стаб <see cref="IScheduledJob"/> для unit-тестов: ничего не делает,
/// используется как <c>typeof(FakeScheduledJob)</c> в <see cref="JobDefinition.JobType"/>
/// и как generic-параметр в <c>AddSingleton&lt;FakeScheduledJob&gt;()</c>,
/// <c>BuildKeyedServiceProvider&lt;FakeScheduledJob&gt;</c>, <c>Be&lt;FakeScheduledJob&gt;()</c>.
/// <para>
/// Не путать с <see cref="SignalJob"/>: <see cref="SignalJob"/> сигнализирует о фактическом
/// вызове <see cref="IScheduledJob.ExecuteAsync"/> через <see cref="System.Threading.Tasks.TaskCompletionSource{TResult}"/>,
/// а этот тип — пассивный стаб без побочных эффектов.
/// </para>
/// </summary>
public sealed class FakeScheduledJob
    : IScheduledJob
{
    /// <inheritdoc />
    public Task ExecuteAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
