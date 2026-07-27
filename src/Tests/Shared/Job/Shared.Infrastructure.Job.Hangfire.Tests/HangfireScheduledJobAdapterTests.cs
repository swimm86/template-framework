// ----------------------------------------------------------------------------------------------
// <copyright file="HangfireScheduledJobAdapterTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Application.Core.Job.Interfaces;
using Shared.Application.Core.Job.Pipeline;
using Shared.Application.Core.Job.Pipeline.Interfaces;
using Shared.Testing.Doubles.DependencyInjection;
using Shared.Testing.Doubles.Job;
using Shared.Testing.Job;

namespace Shared.Infrastructure.Job.Hangfire.Tests;

/// <summary>
/// Тесты <see cref="HangfireScheduledJobAdapter"/>: валидация входных параметров, резолв
/// <see cref="IScheduledJob"/> из DI (включая keyed-сервисы), построение
/// <see cref="ScheduledJobContext"/> и проброс исключений из executor-а.
/// </summary>
public sealed class HangfireScheduledJobAdapterTests
{
    /// <summary>
    /// Валидный <c>jobTypeName</c> с разными значениями <c>serviceKey</c>
    /// (включая <c>null</c>) резолвит <see cref="IScheduledJob"/> и передаёт
    /// в executor корректный <see cref="ScheduledJobContext"/>.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("alpha")]
    public async Task ResolvesAndExecutes_WhenJobTypeIsValid(string? serviceKey)
    {
        // Arrange
        IServiceProvider provider;
        if (serviceKey is null)
        {
            var services = new ServiceCollection();
            services.AddSingleton<FakeScheduledJob>();
            provider = services.BuildServiceProvider();
        }
        else
        {
            provider = BuildKeyedServiceProvider<FakeScheduledJob>(serviceKey);
        }

        await using var disposable = provider as IAsyncDisposable;
        var keyed = provider as MockKeyedServiceProvider;

        var executor = new FakeScheduledJobExecutor();

        var adapter = new HangfireScheduledJobAdapter(
            provider,
            executor,
            NullLogger<HangfireScheduledJobAdapter>.Instance);

        // Act
        await adapter.RunScheduledJobAsync(
            typeof(FakeScheduledJob).AssemblyQualifiedName!,
            serviceKey: serviceKey,
            retryOptions: null,
            CancellationToken.None);

        // Assert
        executor.ExecuteAsyncCallCount.Should().Be(1);
        var captured = executor.LastContext;
        captured.Should().NotBeNull();
        captured!.JobType.Should().Be<FakeScheduledJob>();
        captured.ServiceKey.Should().Be(serviceKey);
        captured.JobKey.Should().Be(typeof(FakeScheduledJob).FullName);
        captured.CancellationToken.Should().Be(CancellationToken.None);
        captured.RetryOptions.Should().BeNull("retryOptions: null не пробрасывается в контекст");

        if (keyed is not null)
        {
            keyed.GetKeyedCallCount.Should().Be(1);
        }
    }

    /// <summary>
    /// Невалидный вход (пустая строка / неразрешимое имя типа / тип не реализует
    /// <see cref="IScheduledJob"/>) даёт <see cref="InvalidOperationException"/>
    /// с осмысленным сообщением и не доходит до executor-а.
    /// </summary>
    /// <param name="jobTypeName">Значение, передаваемое в <c>jobTypeName</c>.</param>
    /// <param name="setupServices">
    /// <c>null</c> — DI не настраивается; иначе — делегат настройки <see cref="IServiceCollection"/>.
    /// </param>
    /// <param name="expectedMessageFragment">Подстрока, которая должна быть в сообщении исключения.</param>
    [Theory]
    [InlineData("", null, "jobTypeName")]
    [InlineData("Definitely.Not.Real.Type, Definitely.Not.Real.Assembly", null, "Failed to resolve type")]
    [InlineData("System.String, System.Runtime", "register-string", "does not implement IScheduledJob")]
    public async Task ThrowsInvalidOperation_WhenJobTypeIsInvalid(
        string jobTypeName,
        string? setupServices,
        string expectedMessageFragment)
    {
        // Arrange
        var sp = setupServices switch
        {
            "register-string" => BuildServiceProviderWithString(),
            _ => new ServiceCollection().BuildServiceProvider(),
        };

        var executor = new FakeScheduledJobExecutor();
        var adapter = new HangfireScheduledJobAdapter(
            sp,
            executor,
            NullLogger<HangfireScheduledJobAdapter>.Instance);

        // Act
        var act = () => adapter.RunScheduledJobAsync(jobTypeName, serviceKey: null, retryOptions: null, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{expectedMessageFragment}*");
        executor.ExecuteAsyncCallCount.Should().Be(0);
    }

    /// <summary>
    /// Исключение из <see cref="IScheduledJobExecutor.ExecuteAsync"/> пробрасывается
    /// вызывающему коду без обёртки.
    /// </summary>
    [Fact]
    public async Task PropagatesExecutorException_WhenExecutorThrows()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<FakeScheduledJob>();
        await using var sp = services.BuildServiceProvider();

        var boom = new InvalidOperationException("boom from executor");
        var executor = new FakeScheduledJobExecutor
        {
            ExceptionToThrowOnExecuteAsync = boom,
        };

        var adapter = new HangfireScheduledJobAdapter(sp, executor, NullLogger<HangfireScheduledJobAdapter>.Instance);

        // Act
        var act = () => adapter.RunScheduledJobAsync(
            typeof(FakeScheduledJob).AssemblyQualifiedName!,
            serviceKey: null,
            retryOptions: null,
            TestContext.Current.CancellationToken);

        // Assert
        var thrown = await act.Should().ThrowAsync<InvalidOperationException>();
        thrown.Which.Should().BeSameAs(boom);
    }

    /// <summary>
    /// <see cref="CancellationToken"/>, переданный в адаптер, попадает в
    /// <see cref="ScheduledJobContext.CancellationToken"/> без изменений.
    /// </summary>
    [Fact]
    public async Task PropagatesCancellationTokenToContext_WhenTokenProvided()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<FakeScheduledJob>();
        await using var sp = services.BuildServiceProvider();

        using var cts = new CancellationTokenSource();
        var expectedToken = cts.Token;

        var executor = new FakeScheduledJobExecutor();

        var adapter = new HangfireScheduledJobAdapter(sp, executor, NullLogger<HangfireScheduledJobAdapter>.Instance);

        // Act
        await adapter.RunScheduledJobAsync(
            typeof(FakeScheduledJob).AssemblyQualifiedName!,
            serviceKey: null,
            retryOptions: null,
            expectedToken);

        // Assert
        executor.LastContext.Should().NotBeNull();
        executor.LastContext!.CancellationToken.Should().Be(expectedToken);
    }

    /// <summary>
    /// <see cref="RetryOptions"/>, переданный в адаптер, попадает в
    /// <see cref="ScheduledJobContext.RetryOptions"/> без изменений.
    /// </summary>
    [Fact]
    public async Task PropagatesRetryOptionsToContext_WhenOptionsProvided()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<FakeScheduledJob>();
        await using var sp = services.BuildServiceProvider();

        var retryOptions = new RetryOptions
        {
            MaxAttempts = 7,
            Delay = TimeSpan.FromMinutes(2),
        };

        var executor = new FakeScheduledJobExecutor();

        var adapter = new HangfireScheduledJobAdapter(sp, executor, NullLogger<HangfireScheduledJobAdapter>.Instance);

        // Act
        await adapter.RunScheduledJobAsync(
            typeof(FakeScheduledJob).AssemblyQualifiedName!,
            serviceKey: null,
            retryOptions: retryOptions,
            TestContext.Current.CancellationToken);

        // Assert
        executor.LastContext.Should().NotBeNull();
        executor.LastContext!.RetryOptions.Should().BeSameAs(retryOptions);
    }

    /// <summary>
    /// Явный null-контракт: если <c>retryOptions: null</c> передан в
    /// <see cref="HangfireScheduledJobAdapter.RunScheduledJobAsync"/>,
    /// <see cref="ScheduledJobContext.RetryOptions"/> остаётся <c>null</c> —
    /// <c>RetryMiddleware</c> интерпретирует это как «без retry».
    /// <para>
    /// Зеркалит тест <c>QuartzScheduledJobAdapterTests.Execute_WithoutRetryOptions_LeavesContextRetryOptionsNull</c>
    /// для подтверждения, что Hangfire-адаптер ведёт себя консистентно с Quartz-адаптером
    /// в части per-execution <see cref="RetryOptions"/>.
    /// </para>
    /// </summary>
    [Fact]
    public async Task PropagatesNullRetryOptionsToContext_WhenOptionsNotProvided()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<FakeScheduledJob>();
        await using var sp = services.BuildServiceProvider();

        var executor = new FakeScheduledJobExecutor();

        var adapter = new HangfireScheduledJobAdapter(
            sp,
            executor,
            NullLogger<HangfireScheduledJobAdapter>.Instance);

        // Act
        await adapter.RunScheduledJobAsync(
            typeof(FakeScheduledJob).AssemblyQualifiedName!,
            serviceKey: null,
            retryOptions: null,
            TestContext.Current.CancellationToken);

        // Assert
        executor.LastContext.Should().NotBeNull();
        executor.LastContext!.RetryOptions.Should().BeNull("retryOptions: null должен давать RetryOptions = null в контексте");
    }

    /// <summary>
    /// Строит <see cref="IServiceProvider"/>, в котором <see cref="string"/>
    /// зарегистрирован как singleton. Используется в кейсе, когда <c>Type.GetType</c>
    /// успешно резолвит тип, но экземпляр не реализует <see cref="IScheduledJob"/>.
    /// </summary>
    private static IServiceProvider BuildServiceProviderWithString()
    {
        var services = new ServiceCollection();
        services.AddSingleton("not a job");
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Строит <see cref="IKeyedServiceProvider"/> с <typeparamref name="TJob"/>,
    /// зарегистрированным под указанным ключом. Используется для кейса
    /// <c>serviceKey != null</c> в <see cref="ResolvesAndExecutes_WhenJobTypeIsValid"/>.
    /// </summary>
    private static IKeyedServiceProvider BuildKeyedServiceProvider<TJob>(string serviceKey)
        where TJob : class
    {
        var job = new FakeScheduledJob();
        var keyed = new MockKeyedServiceProvider();
        keyed.Register(typeof(TJob), serviceKey, job);
        return keyed;
    }
}