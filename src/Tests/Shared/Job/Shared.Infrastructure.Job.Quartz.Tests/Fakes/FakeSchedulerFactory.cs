// ----------------------------------------------------------------------------------------------
// <copyright file="FakeSchedulerFactory.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Moq;
using Quartz;

namespace Shared.Infrastructure.Job.Quartz.Tests.Fakes;

/// <summary>
/// Заглушка <see cref="ISchedulerFactory"/>, которая всегда возвращает один и тот же
/// <see cref="IScheduler"/> из <see cref="SchedulerMock"/>.
/// Используется только в unit-тестах.
/// </summary>
/// <remarks>
/// <para>
/// Вся <see cref="IScheduler"/> описана через <c>Mock&lt;IScheduler&gt;</c> — это снимает
/// необходимость поддерживать руками полный stub при каждом обновлении версии Quartz.
/// </para>
/// <para>
/// Тест получает <see cref="SchedulerMock"/>, настраивает expectations и
/// верифицирует вызовы через стандартный API Moq.
/// </para>
/// </remarks>
internal sealed class FakeSchedulerFactory : ISchedulerFactory
{
    /// <summary>
    /// Mock, описывающий поведение возвращаемого планировщика.
    /// </summary>
    public Mock<IScheduler> SchedulerMock { get; } = new();

    /// <summary>
    /// Журнал вызовов <see cref="GetScheduler(CancellationToken)"/>.
    /// </summary>
    public List<CancellationToken> GetSchedulerCalls { get; } = new();

    /// <inheritdoc />
    public Task<IScheduler> GetScheduler(CancellationToken cancellationToken = default)
    {
        GetSchedulerCalls.Add(cancellationToken);
        return Task.FromResult(SchedulerMock.Object);
    }

    /// <inheritdoc />
    public Task<IScheduler> GetScheduler(string schedName, CancellationToken cancellationToken = default) =>
        GetScheduler(cancellationToken);

    /// <summary>
    /// <see cref="ISchedulerFactory"/> имеет несколько членов, не используемых
    /// QuartzJobScheduler/QuartzJobSchedulerBootstrapper; возвращаем фиктивные данные.
    /// </summary>
    public IReadOnlyList<string> GetAllSchedulerIds() => new[] { "fake" };

    /// <summary>
    /// Не используется в тестируемых сценариях; возвращаем фиктивные данные.
    /// </summary>
    public Task<IReadOnlyList<IScheduler>> GetAllSchedulers(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IScheduler>>(new[] { SchedulerMock.Object });
}
