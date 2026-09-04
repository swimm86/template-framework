// ----------------------------------------------------------------------------------------------
// <copyright file="FakeSchedulerFactory.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using NSubstitute;
using Quartz;

namespace Shared.Infrastructure.Job.Quartz.Tests.Fakes;

/// <summary>
/// Заглушка <see cref="ISchedulerFactory"/>, которая всегда возвращает один и тот же
/// <see cref="IScheduler"/> из <see cref="Scheduler"/>.
/// Используется только в unit-тестах.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IScheduler"/> сгенерирован через <c>Substitute.For&lt;IScheduler&gt;()</c> —
/// это снимает необходимость поддерживать руками полный stub при каждом обновлении версии Quartz.
/// </para>
/// <para>
/// Тест получает <see cref="Scheduler"/>, настраивает expectations и
/// верифицирует вызовы через стандартный API NSubstitute (<see cref="NSubstitute.SubstituteExtensions"/>
/// и <c>ReceivedCalls()</c>).
/// </para>
/// </remarks>
internal sealed class FakeSchedulerFactory : ISchedulerFactory
{
    /// <summary>
    /// Substitute, описывающий поведение возвращаемого планировщика.
    /// </summary>
    public IScheduler Scheduler { get; } = Substitute.For<IScheduler>();

    /// <summary>
    /// Журнал вызовов <see cref="GetScheduler(CancellationToken)"/>.
    /// </summary>
    public List<CancellationToken> GetSchedulerCalls { get; } = new();

    /// <inheritdoc />
    public Task<IScheduler> GetScheduler(CancellationToken cancellationToken = default)
    {
        GetSchedulerCalls.Add(cancellationToken);
        return Task.FromResult(Scheduler);
    }

    /// <inheritdoc />
    public Task<IScheduler?> GetScheduler(string schedName, CancellationToken cancellationToken = default) =>
        GetScheduler(cancellationToken).ContinueWith(t => (IScheduler?)t.Result, cancellationToken);

    /// <summary>
    /// <see cref="ISchedulerFactory"/> имеет несколько членов, не используемых
    /// QuartzJobScheduler/QuartzJobSchedulerBootstrapper; возвращаем фиктивные данные.
    /// </summary>
    public IReadOnlyList<string> GetAllSchedulerIds() => ["fake"];

    /// <summary>
    /// Не используется в тестируемых сценариях; возвращаем фиктивные данные.
    /// </summary>
    public Task<IReadOnlyList<IScheduler>> GetAllSchedulers(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IScheduler>>([Scheduler]);
}
