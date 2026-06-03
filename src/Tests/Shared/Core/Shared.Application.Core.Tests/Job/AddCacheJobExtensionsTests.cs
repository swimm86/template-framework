// ----------------------------------------------------------------------------------------------
// <copyright file="AddCacheJobExtensionsTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shared.Application.Core.Cache;
using Shared.Application.Core.Cache.Interfaces;
using Shared.Application.Core.Job.Cache;
using Shared.Application.Core.Job.Cache.Extensions;
using Shared.Application.Core.Job.Enums;
using Shared.Application.Core.Job.Pipeline;
using Shared.Application.Core.Job.Pipeline.Interfaces;
using Shared.Application.Core.Job.Pipeline.Middlewares;
using Shared.Application.Core.Job.Scheduler;

namespace Shared.Application.Core.Tests.Job;

/// <summary>
/// Тесты расширений <see cref="ServiceCollectionExtensions"/> для регистрации фоновых задач
/// кэширования: CRON и Flags-расписания, лямбда и typed-варианты, выполнение действий
/// через конвейер middleware (логирование, correlationId, retry), отмена операций.
/// </summary>
public sealed class AddCacheJobExtensionsTests
{
    private const string CronEveryMinute = "0 * * * * ?";
    private const string TestCacheKey = "test-cache";

    // ─── AddCronCacheJob<TData> (lambda) ────────────────────────────────────

    /// <summary>
    /// <see cref="ServiceCollectionExtensions.AddCronCacheJob{TData}(IServiceCollection, string, string, Func{IServiceProvider, Task{TData}})"/>
    /// регистрирует <see cref="ICacheService{TData}"/> как keyed-singleton.
    /// </summary>
    [Fact]
    public void AddCronCacheJob_Lambda_RegistersKeyedCacheService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCronCacheJob<string>(
            TestCacheKey,
            CronEveryMinute,
            _ => Task.FromResult("cached"));

        var sp = services.BuildServiceProvider();

        // Assert
        var cache = sp.GetKeyedService<ICacheService<string>>(TestCacheKey);
        cache.Should().NotBeNull();
    }

    /// <summary>
    /// <see cref="ServiceCollectionExtensions.AddCronCacheJob{TData}(IServiceCollection, string, string, Func{IServiceProvider, Task{TData}})"/>
    /// регистрирует <see cref="JobSchedulerOptions"/> с одним определением задачи
    /// типа <see cref="JobSchedule.Cron"/>.
    /// </summary>
    [Fact]
    public void AddCronCacheJob_Lambda_RegistersCronJobDefinition()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCronCacheJob<string>(
            TestCacheKey,
            CronEveryMinute,
            _ => Task.FromResult("data"));

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<JobSchedulerOptions>();

        // Assert
        options.Definitions.Should().ContainSingle();
        var def = options.Definitions[0];
        def.JobKey.Should().Be(TestCacheKey);
        def.Schedule.Should().BeOfType<JobSchedule.Cron>()
            .Which.Expression.Should().Be(CronEveryMinute);
        def.Action.Should().NotBeNull();
        def.JobType.Should().BeNull();
    }

    /// <summary>
    /// При выполнении действия задачи через конвейер, кэш обновляется —
    /// <see cref="ICacheService{TData}.GetCachedDataAsync"/> возвращает актуальные данные.
    /// </summary>
    [Fact]
    public async Task AddCronCacheJob_Lambda_ActionUpdatesCache()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        var dataSource = "initial";
        services.AddCronCacheJob<string>(
            TestCacheKey,
            CronEveryMinute,
            _ => Task.FromResult(dataSource));

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<JobSchedulerOptions>();
        var executor = sp.GetRequiredService<IScheduledJobExecutor>();
        var ctx = new ScheduledJobContext(
            options.Definitions[0].JobKey,
            sp,
            CancellationToken.None)
        {
            Action = options.Definitions[0].Action,
        };

        // Act
        dataSource = "updated";
        await executor.ExecuteAsync(ctx);
        var cached = await sp.GetCachedDataAsync<string>(TestCacheKey);

        // Assert
        cached.Should().Be("updated");
    }

    /// <summary>
    /// <c>null</c> в качестве <c>services</c> выбрасывает <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void AddCronCacheJob_Lambda_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var act = () => services.AddCronCacheJob<string>(
            TestCacheKey,
            CronEveryMinute,
            _ => Task.FromResult("x"));

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    /// <summary>
    /// <c>null</c> в качестве <c>getOrCreateFunc</c> выбрасывает <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void AddCronCacheJob_Lambda_NullFunc_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<IServiceProvider, Task<string>> func = null!;

        // Act
        var act = () => services.AddCronCacheJob(
            TestCacheKey,
            CronEveryMinute,
            func);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("getOrCreateFunc");
    }

    // ─── AddFlagsCacheJob<TData> (lambda) ───────────────────────────────────

    /// <summary>
    /// <see cref="ServiceCollectionExtensions.AddFlagsCacheJob{TData}(IServiceCollection, string, JobTriggerFlags, TimeSpan, Func{IServiceProvider, Task{TData}})"/>
    /// регистрирует <see cref="ICacheService{TData}"/> как keyed-singleton.
    /// </summary>
    [Fact]
    public void AddFlagsCacheJob_Lambda_RegistersKeyedCacheService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddFlagsCacheJob<string>(
            TestCacheKey,
            JobTriggerFlags.Daily,
            TimeSpan.FromHours(3),
            _ => Task.FromResult("flags-data"));

        var sp = services.BuildServiceProvider();

        // Assert
        var cache = sp.GetKeyedService<ICacheService<string>>(TestCacheKey);
        cache.Should().NotBeNull();
    }

    /// <summary>
    /// <see cref="ServiceCollectionExtensions.AddFlagsCacheJob{TData}(IServiceCollection, string, JobTriggerFlags, TimeSpan, Func{IServiceProvider, Task{TData}})"/>
    /// регистрирует <see cref="JobSchedulerOptions"/> с определением задачи
    /// типа <see cref="JobSchedule.Flags"/>.
    /// </summary>
    [Fact]
    public void AddFlagsCacheJob_Lambda_RegistersFlagsJobDefinition()
    {
        // Arrange
        var services = new ServiceCollection();
        var flags = JobTriggerFlags.Daily | JobTriggerFlags.OnStartup;
        var time = TimeSpan.FromHours(6);

        // Act
        services.AddFlagsCacheJob<string>(
            TestCacheKey,
            flags,
            time,
            _ => Task.FromResult("data"));

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<JobSchedulerOptions>();

        // Assert
        options.Definitions.Should().ContainSingle();
        var def = options.Definitions[0];
        def.JobKey.Should().Be(TestCacheKey);
        var schedule = def.Schedule.Should().BeOfType<JobSchedule.Flags>().Subject;
        schedule.TriggerFlags.Should().HaveFlag(JobTriggerFlags.Daily);
        schedule.TriggerFlags.Should().HaveFlag(JobTriggerFlags.OnStartup);
        schedule.SpecificTime.Should().Be(time);
        def.Action.Should().NotBeNull();
    }

    /// <summary>
    /// При выполнении действия задачи с флагами через конвейер, кэш обновляется.
    /// </summary>
    [Fact]
    public async Task AddFlagsCacheJob_Lambda_ActionUpdatesCache()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        var dataSource = "before";
        services.AddFlagsCacheJob<string>(
            TestCacheKey,
            JobTriggerFlags.EveryHour,
            TimeSpan.FromMinutes(30),
            _ => Task.FromResult(dataSource));

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<JobSchedulerOptions>();
        var executor = sp.GetRequiredService<IScheduledJobExecutor>();
        var ctx = new ScheduledJobContext(
            options.Definitions[0].JobKey,
            sp,
            CancellationToken.None)
        {
            Action = options.Definitions[0].Action,
        };

        // Act
        dataSource = "after";
        await executor.ExecuteAsync(ctx);
        var cached = await sp.GetCachedDataAsync<string>(TestCacheKey);

        // Assert
        cached.Should().Be("after");
    }

    /// <summary>
    /// <c>null</c> в качестве <c>services</c> для Flags-варианта выбрасывает
    /// <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void AddFlagsCacheJob_Lambda_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var act = () => services.AddFlagsCacheJob<string>(
            TestCacheKey,
            JobTriggerFlags.Daily,
            TimeSpan.FromHours(1),
            _ => Task.FromResult("x"));

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    /// <summary>
    /// <c>null</c> в качестве <c>getOrCreateFunc</c> для Flags-варианта выбрасывает
    /// <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void AddFlagsCacheJob_Lambda_NullFunc_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<IServiceProvider, Task<string>> func = null!;

        // Act
        var act = () => services.AddFlagsCacheJob(
            TestCacheKey,
            JobTriggerFlags.Daily,
            TimeSpan.FromHours(1),
            func);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("getOrCreateFunc");
    }

    // ─── AddCronCacheJob<TJob, TData> (typed) ───────────────────────────────

    /// <summary>
    /// <see cref="ServiceCollectionExtensions.AddCronCacheJob{TJob, TData}(IServiceCollection, string, string)"/>
    /// регистрирует <see cref="ICacheService{TData}"/> через <see cref="CacheUpdateJob{TData}.GetCacheDataAsync"/>.
    /// </summary>
    [Fact]
    public void AddCronCacheJob_Typed_RegistersKeyedCacheService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<StubCacheJob>();

        // Act
        services.AddCronCacheJob<StubCacheJob, string>(TestCacheKey, CronEveryMinute);

        var sp = services.BuildServiceProvider();

        // Assert
        var cache = sp.GetKeyedService<ICacheService<string>>(TestCacheKey);
        cache.Should().NotBeNull();
    }

    /// <summary>
    /// <see cref="ServiceCollectionExtensions.AddCronCacheJob{TJob, TData}(IServiceCollection, string, string)"/>
    /// регистрирует <see cref="JobSchedulerOptions"/> с <c>JobType = typeof(TJob)</c>.
    /// </summary>
    [Fact]
    public void AddCronCacheJob_Typed_RegistersTypedJobDefinition()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<StubCacheJob>();

        // Act
        services.AddCronCacheJob<StubCacheJob, string>(TestCacheKey, CronEveryMinute);

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<JobSchedulerOptions>();

        // Assert
        options.Definitions.Should().ContainSingle();
        var def = options.Definitions[0];
        def.JobType.Should().Be(typeof(StubCacheJob));
        def.Schedule.Should().BeOfType<JobSchedule.Cron>()
            .Which.Expression.Should().Be(CronEveryMinute);
    }

    /// <summary>
    /// При выполнении typed CRON-задачи через конвейер, executor резолвит
    /// задачу из DI и вызывает <see cref="CacheUpdateJob{TData}.ExecuteAsync"/>.
    /// Последующий вызов <see cref="ICacheService{TData}.GetCachedDataAsync"/> возвращает
    /// данные, загруженные джобой.
    /// </summary>
    [Fact]
    public async Task AddCronCacheJob_Typed_ExecutorRunsJobAndUpdatesCache()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<StubCacheJob>();

        services.AddCronCacheJob<StubCacheJob, string>(TestCacheKey, CronEveryMinute);

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<JobSchedulerOptions>();
        var executor = sp.GetRequiredService<IScheduledJobExecutor>();
        var ctx = new ScheduledJobContext(TestCacheKey, sp, CancellationToken.None)
        {
            JobType = options.Definitions[0].JobType,
        };

        // Act
        await executor.ExecuteAsync(ctx);
        var cached = await sp.GetCachedDataAsync<string>(TestCacheKey);

        // Assert
        cached.Should().Be(StubCacheJob.ReturnValue);
    }

    // ─── AddFlagsCacheJob<TJob, TData> (typed) ──────────────────────────────

    /// <summary>
    /// <see cref="ServiceCollectionExtensions.AddFlagsCacheJob{TJob, TData}(IServiceCollection, string, JobTriggerFlags, TimeSpan, Func{IServiceProvider, Task{TData}})"/>
    /// регистрирует одновременно <see cref="ICacheService{TData}"/> и typed-задачу.
    /// </summary>
    [Fact]
    public void AddFlagsCacheJob_Typed_RegistersCacheAndTypedJob()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<StubCacheJob>();
        var flags = JobTriggerFlags.EveryMinute;
        var time = TimeSpan.FromMinutes(5);

        // Act
        services.AddFlagsCacheJob<StubCacheJob, string>(
            TestCacheKey,
            flags,
            time,
            _ => Task.FromResult("fallback"));

        var sp = services.BuildServiceProvider();

        // Assert
        var cache = sp.GetKeyedService<ICacheService<string>>(TestCacheKey);
        cache.Should().NotBeNull();

        var options = sp.GetRequiredService<JobSchedulerOptions>();
        options.Definitions.Should().ContainSingle();
        var def = options.Definitions[0];
        def.JobType.Should().Be(typeof(StubCacheJob));
        def.Schedule.Should().BeOfType<JobSchedule.Flags>();
    }

    // ─── Retry ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Действие задачи выполняется через <see cref="RetryMiddleware"/>, который
    /// повторяет попытки при неудаче вплоть до <see cref="RetryOptions.MaxAttempts"/>.
    /// </summary>
    [Fact]
    public async Task AddCronCacheJob_Lambda_RetriesOnFailure()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(
            Options.Create(new RetryOptions { MaxAttempts = 3, Delay = TimeSpan.FromMilliseconds(1) }));

        var attempts = 0;
        services.AddCronCacheJob<string>(
            TestCacheKey,
            CronEveryMinute,
            _ =>
            {
                attempts++;
                if (attempts < 3)
                {
                    throw new InvalidOperationException($"fail {attempts}");
                }

                return Task.FromResult("finally");
            });

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<JobSchedulerOptions>();
        var executor = sp.GetRequiredService<IScheduledJobExecutor>();
        var ctx = new ScheduledJobContext(
            TestCacheKey,
            sp,
            CancellationToken.None)
        {
            Action = options.Definitions[0].Action,
        };

        // Act
        await executor.ExecuteAsync(ctx);

        // Assert
        attempts.Should().Be(3);
    }

    /// <summary>
    /// При исчерпании всех попыток retry-middleware пробрасывает последнее исключение.
    /// </summary>
    [Fact]
    public async Task AddCronCacheJob_Lambda_ThrowsAfterMaxRetries()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(
            Options.Create(new RetryOptions { MaxAttempts = 2, Delay = TimeSpan.FromMilliseconds(1) }));

        services.AddCronCacheJob<string>(
            TestCacheKey,
            CronEveryMinute,
            _ => throw new InvalidOperationException("always fail"));

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<JobSchedulerOptions>();
        var executor = sp.GetRequiredService<IScheduledJobExecutor>();
        var ctx = new ScheduledJobContext(
            TestCacheKey,
            sp,
            CancellationToken.None)
        {
            Action = options.Definitions[0].Action,
        };

        // Act
        var act = () => executor.ExecuteAsync(ctx);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("always fail");
    }

    // ─── Cancellation ───────────────────────────────────────────────────────

    /// <summary>
    /// <see cref="CancellationToken"/> пробрасывается из
    /// <see cref="ScheduledJobContext"/> в typed-задачу через
    /// <see cref="CacheUpdateJob{TData}.ExecuteAsync"/>.
    /// </summary>
    [Fact]
    public async Task AddCronCacheJob_Typed_CancellationTokenPropagated()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var job = new TokenCapturingCacheJob();
        services.AddSingleton<TokenCapturingCacheJob>(job);

        services.AddCronCacheJob<TokenCapturingCacheJob, string>(TestCacheKey, CronEveryMinute);

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<JobSchedulerOptions>();
        var executor = sp.GetRequiredService<IScheduledJobExecutor>();

        using var cts = new CancellationTokenSource();
        var ctx = new ScheduledJobContext(TestCacheKey, sp, cts.Token)
        {
            JobType = options.Definitions[0].JobType,
        };

        // Act
        await executor.ExecuteAsync(ctx);

        // Assert
        job.ReceivedToken.Should().Be(cts.Token);
    }

    /// <summary>
    /// Отмена <see cref="CancellationToken"/> во время retry-задержки пробрасывает
    /// <see cref="OperationCanceledException"/>.
    /// </summary>
    [Fact]
    public async Task AddCronCacheJob_Lambda_CancellationDuringRetryDelayThrows()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(
            Options.Create(new RetryOptions { MaxAttempts = 5, Delay = TimeSpan.FromSeconds(30) }));

        services.AddCronCacheJob<string>(
            TestCacheKey,
            CronEveryMinute,
            _ => throw new InvalidOperationException("fail fast"));

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<JobSchedulerOptions>();
        var executor = sp.GetRequiredService<IScheduledJobExecutor>();

        using var cts = new CancellationTokenSource();
        var ctx = new ScheduledJobContext(
            TestCacheKey,
            sp,
            cts.Token)
        {
            Action = options.Definitions[0].Action,
        };

        // Act — cancel after a short delay, while middleware is in its Task.Delay.
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));
        var act = () => executor.ExecuteAsync(ctx);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // ─── Multiple jobs ──────────────────────────────────────────────────────

    /// <summary>
    /// Повторные вызовы методов регистрации: <see cref="JobSchedulerOptions"/>
    /// заменяется последним вызовом (как документировано в
    /// <c>AddJobs_CalledTwice_OptionsReflectsLastCall</c>), но все
    /// <see cref="ICacheService{TData}"/> остаются независимо зарегистрированными.
    /// </summary>
    [Fact]
    public void AddMultipleCacheJobs_LastJobDefinitionWins()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services
            .AddCronCacheJob<string>("cache-a", CronEveryMinute, _ => Task.FromResult("a"))
            .AddFlagsCacheJob<int>("cache-b", JobTriggerFlags.Daily, TimeSpan.FromHours(1), _ => Task.FromResult(42));

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<JobSchedulerOptions>();

        // Assert — последний AddJobs перезаписывает JobSchedulerOptions.
        options.Definitions.Should().ContainSingle()
            .Which.JobKey.Should().Be("cache-b");

        // Оба ICacheService зарегистрированы независимо.
        sp.GetKeyedService<ICacheService<string>>("cache-a").Should().NotBeNull();
        sp.GetKeyedService<ICacheService<int>>("cache-b").Should().NotBeNull();
    }

    /// <summary>
    /// Микширование lambda и typed-вариантов — кэш-сервисы регистрируются
    /// независимо для разных ключей.
    /// </summary>
    [Fact]
    public void AddMixedCacheJobs_DifferentKeys_IndependentCacheServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<StubCacheJob>();

        // Act
        services
            .AddCronCacheJob<string>("lambda-cache", CronEveryMinute, _ => Task.FromResult("lambda"))
            .AddCronCacheJob<StubCacheJob, string>("typed-cache", CronEveryMinute);

        var sp = services.BuildServiceProvider();

        // Assert
        var lambdaCache = sp.GetKeyedService<ICacheService<string>>("lambda-cache");
        var typedCache = sp.GetKeyedService<ICacheService<string>>("typed-cache");
        lambdaCache.Should().NotBeNull();
        typedCache.Should().NotBeNull();
        lambdaCache.Should().NotBeSameAs(typedCache);
    }

    // ─── Pipeline registration ──────────────────────────────────────────────

    /// <summary>
    /// Вызов любого из методов расширения регистрирует полный конвейер:
    /// <see cref="IScheduledJobExecutor"/>, три middleware
    /// (<see cref="LoggingMiddleware"/>, <see cref="CorrelationIdMiddleware"/>,
    /// <see cref="RetryMiddleware"/>) и <see cref="RetryOptions"/>.
    /// </summary>
    [Fact]
    public void AddCronCacheJob_Lambda_RegistersFullPipeline()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddCronCacheJob<string>(
            TestCacheKey,
            CronEveryMinute,
            _ => Task.FromResult("data"));

        var sp = services.BuildServiceProvider();

        // Assert
        sp.GetRequiredService<IScheduledJobExecutor>().Should().NotBeNull();
        sp.GetRequiredService<RetryOptions>().Should().NotBeNull();
        var middlewares = sp.GetServices<IScheduledJobMiddleware>().ToArray();
        middlewares.OfType<LoggingMiddleware>().Should().HaveCount(1);
        middlewares.OfType<CorrelationIdMiddleware>().Should().HaveCount(1);
        middlewares.OfType<RetryMiddleware>().Should().HaveCount(1);
    }

    // ─── Typed job resolves via DI and uses GetCacheDataAsync ───────────────

    /// <summary>
    /// <see cref="CacheService{TData}"/>, зарегистрированный через typed-вариант,
    /// получает данные через <see cref="CacheUpdateJob{TData}.GetCacheDataAsync"/>.
    /// </summary>
    [Fact]
    public async Task AddCronCacheJob_Typed_CacheServiceUsesJobGetCacheDataAsync()
    {
        // Arrange
        var services = new ServiceCollection();
        var job = new CountingCacheJob();
        services.AddSingleton<CountingCacheJob>(job);

        services.AddCronCacheJob<CountingCacheJob, string>("count-cache", CronEveryMinute);

        var sp = services.BuildServiceProvider();

        // Act
        var cache = sp.GetKeyedService<ICacheService<string>>("count-cache");
        cache.Should().NotBeNull();
        var result = await cache!.GetCachedDataAsync();

        // Assert
        result.Should().Be(CountingCacheJob.ReturnValue);
        job.UpdateDataCallCount.Should().Be(1);
    }

    // ─── Test doubles ───────────────────────────────────────────────────────

    /// <summary>
    /// Заглушка <see cref="CacheUpdateJob{TData}"/>, возвращающая фиксированное значение.
    /// </summary>
    private sealed class StubCacheJob
        : CacheUpdateJob<string>
    {
        public const string ReturnValue = "stub-data";

        protected override Task<string> UpdateDataAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult(ReturnValue);
    }

    /// <summary>
    /// Заглушка <see cref="CacheUpdateJob{TData}"/> с подсчётом вызовов
    /// <see cref="CacheUpdateJob{TData}.UpdateDataAsync"/>.
    /// </summary>
    private sealed class CountingCacheJob
        : CacheUpdateJob<string>
    {
        public const string ReturnValue = "counted";

        public int UpdateDataCallCount { get; private set; }

        protected override Task<string> UpdateDataAsync(
            CancellationToken cancellationToken = default)
        {
            UpdateDataCallCount++;
            return Task.FromResult(ReturnValue);
        }
    }

    /// <summary>
    /// Заглушка <see cref="CacheUpdateJob{TData}"/>, сохраняющая
    /// переданный <see cref="CancellationToken"/>.
    /// </summary>
    private sealed class TokenCapturingCacheJob
        : CacheUpdateJob<string>
    {
        public CancellationToken ReceivedToken { get; private set; }

        protected override Task<string> UpdateDataAsync(
            CancellationToken cancellationToken = default)
        {
            ReceivedToken = cancellationToken;
            return Task.FromResult(string.Empty);
        }
    }
}
