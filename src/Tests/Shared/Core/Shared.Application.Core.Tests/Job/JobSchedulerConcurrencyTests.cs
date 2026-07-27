// ----------------------------------------------------------------------------------------------
// <copyright file="JobSchedulerConcurrencyTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using Shared.Application.Core.Job.Scheduler;
using Shared.Testing.Doubles.Job;

namespace Shared.Application.Core.Tests.Job;

/// <summary>
/// Проверяет потокобезопасность параллельных вызовов <see cref="FakeJobScheduler.ScheduleAsync"/>.
/// </summary>
public sealed class JobSchedulerConcurrencyTests
{
    /// <summary>
    /// Десять параллельных вызовов с разными ключами фиксируются без потери регистраций.
    /// </summary>
    [Fact]
    public async Task ScheduleAsync_TenConcurrentTasksWithDifferentJobKeys_AllRegistered()
    {
        var scheduler = new FakeJobScheduler();
        var definitions = Enumerable.Range(1, 10)
            .Select(index => CreateDefinition($"job-{index}"))
            .ToArray();

        await Task.WhenAll(definitions.Select(definition =>
            Task.Run(() => scheduler.ScheduleAsync(definition, TestContext.Current.CancellationToken))));

        scheduler.ScheduleAsyncInvocations.Should().HaveCount(10);
        scheduler.ScheduleAsyncInvocations.Select(invocation => invocation.Definition.JobKey)
            .Should().BeEquivalentTo(definitions.Select(definition => definition.JobKey));
    }

    /// <summary>
    /// Десять параллельных вызовов с одним ключом не теряются и сохраняют последнее
    /// зафиксированное определение среди успешно обработанных вызовов.
    /// </summary>
    [Fact]
    public async Task ScheduleAsync_TenConcurrentTasksWithSameJobKey_LastWriteWins()
    {
        var scheduler = new FakeJobScheduler();
        var definitions = Enumerable.Range(1, 10)
            .Select(index => CreateDefinition("shared-job", index))
            .ToArray();

        await Task.WhenAll(definitions.Select(definition =>
            Task.Run(() => scheduler.ScheduleAsync(definition, TestContext.Current.CancellationToken))));

        scheduler.ScheduleAsyncInvocations.Should().HaveCount(10);
        scheduler.ScheduleAsyncInvocations.Select(invocation => invocation.Definition.JobKey)
            .Should().AllBe("shared-job");
        scheduler.ScheduleAsyncInvocations.Select(invocation => invocation.Definition.Action)
            .Should().Contain(definitions[^1].Action);
    }

    /// <summary>
    /// Параллельные вызовы с разными результатами селектора возвращают исключения только
    /// для настроенных ключей, а остальные вызовы успешно фиксируются.
    /// </summary>
    [Fact]
    public async Task ScheduleAsync_ConcurrentWithMixedExceptions_SomeCallsThrowOthersSucceed()
    {
        var scheduler = new FakeJobScheduler
        {
            ExceptionForJobKey = jobKey => jobKey.StartsWith("fail-", StringComparison.Ordinal)
                ? new InvalidOperationException(jobKey)
                : null,
        };
        var definitions = Enumerable.Range(1, 10)
            .Select(index => CreateDefinition(index % 2 == 0 ? $"fail-{index}" : $"success-{index}"))
            .ToArray();

        var results = await Task.WhenAll(definitions.Select(definition =>
            CaptureExceptionAsync(scheduler, definition)));

        results.Count(exception => exception is not null).Should().Be(5);
        scheduler.ScheduleAsyncInvocations.Select(invocation => invocation.Definition.JobKey)
            .Should().BeEquivalentTo(definitions.Where(definition =>
                !definition.JobKey.StartsWith("fail-", StringComparison.Ordinal))
                .Select(definition => definition.JobKey));
    }

    /// <summary>
    /// Параллельные вызовы с одним ключом сохраняют каждый переданный token без подмены.
    /// </summary>
    [Fact]
    public async Task ScheduleAsync_TwoTasksSameJobKeyDifferentCancellationToken_BothCancellationTokensPassed()
    {
        var scheduler = new FakeJobScheduler();
        using var firstSource = new CancellationTokenSource();
        using var secondSource = new CancellationTokenSource();
        var firstDefinition = CreateDefinition("shared-job", 1);
        var secondDefinition = CreateDefinition("shared-job", 2);

        await Task.WhenAll(
            Task.Run(() => scheduler.ScheduleAsync(firstDefinition, firstSource.Token)),
            Task.Run(() => scheduler.ScheduleAsync(secondDefinition, secondSource.Token)));

        scheduler.ScheduleAsyncInvocations.Select(invocation => invocation.Token)
            .Should().Contain(firstSource.Token)
            .And.Contain(secondSource.Token);
    }

    /// <summary>
    /// Параллельный селектор получает именно ключ каждого собственного вызова.
    /// </summary>
    [Fact]
    public async Task ExceptionForJobKey_ConcurrentDifferentKeys_EachTaskGetsOwnException()
    {
        var observedKeys = new ConcurrentBag<string>();
        var scheduler = new FakeJobScheduler
        {
            ExceptionForJobKey = jobKey =>
            {
                observedKeys.Add(jobKey);
                return new InvalidOperationException(jobKey);
            },
        };
        var definitions = Enumerable.Range(1, 10)
            .Select(index => CreateDefinition($"job-{index}"))
            .ToArray();

        var exceptions = await Task.WhenAll(definitions.Select(definition =>
            CaptureExceptionAsync(scheduler, definition)));

        observedKeys.Should().BeEquivalentTo(definitions.Select(definition => definition.JobKey));
        exceptions.Select(exception => exception!.Message)
            .Should().BeEquivalentTo(definitions.Select(definition => definition.JobKey));
    }

    private static JobDefinition CreateDefinition(string jobKey, int actionValue = 0) =>
        new(jobKey, (_, _) => Task.FromResult(actionValue), new JobSchedule.OnStartup());

    private static async Task<Exception?> CaptureExceptionAsync(
        FakeJobScheduler scheduler,
        JobDefinition definition)
    {
        await Task.Yield();

        try
        {
            await scheduler.ScheduleAsync(definition, TestContext.Current.CancellationToken);
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }
}
