// ----------------------------------------------------------------------------------------------
// <copyright file="HangfireScheduledJobAdapterTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.Application.Core.Job.Interfaces;
using Shared.Application.Core.Job.Pipeline;
using Shared.Application.Core.Job.Pipeline.Interfaces;
using Shared.Testing.Doubles.DependencyInjection;
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

        var executor = new Mock<IScheduledJobExecutor>();
        ScheduledJobContext? captured = null;
        executor
            .Setup(e => e.ExecuteAsync(It.IsAny<ScheduledJobContext>()))
            .Callback<ScheduledJobContext>(ctx => captured = ctx)
            .Returns(Task.CompletedTask);

        var adapter = new HangfireScheduledJobAdapter(
            provider,
            executor.Object,
            NullLogger<HangfireScheduledJobAdapter>.Instance);

        // Act
        await adapter.RunScheduledJobAsync(
            typeof(FakeScheduledJob).AssemblyQualifiedName!,
            serviceKey: serviceKey,
            CancellationToken.None);

        // Assert
        executor.Verify(e => e.ExecuteAsync(It.IsAny<ScheduledJobContext>()), Times.Once);
        captured.Should().NotBeNull();
        captured!.JobType.Should().Be<FakeScheduledJob>();
        captured.ServiceKey.Should().Be(serviceKey);
        captured.JobKey.Should().Be(typeof(FakeScheduledJob).FullName);
        captured.CancellationToken.Should().Be(CancellationToken.None);

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
            _ => new Mock<IServiceProvider>().Object,
        };

        var executor = new Mock<IScheduledJobExecutor>();
        var adapter = new HangfireScheduledJobAdapter(
            sp,
            executor.Object,
            NullLogger<HangfireScheduledJobAdapter>.Instance);

        // Act
        var act = () => adapter.RunScheduledJobAsync(jobTypeName, serviceKey: null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{expectedMessageFragment}*");
        executor.VerifyNoOtherCalls();
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
        var executor = new Mock<IScheduledJobExecutor>();
        executor
            .Setup(e => e.ExecuteAsync(It.IsAny<ScheduledJobContext>()))
            .ThrowsAsync(boom);

        var adapter = new HangfireScheduledJobAdapter(sp, executor.Object, NullLogger<HangfireScheduledJobAdapter>.Instance);

        // Act
        var act = () => adapter.RunScheduledJobAsync(
            typeof(FakeScheduledJob).AssemblyQualifiedName!,
            serviceKey: null,
            CancellationToken.None);

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

        ScheduledJobContext? captured = null;
        var executor = new Mock<IScheduledJobExecutor>();
        executor
            .Setup(e => e.ExecuteAsync(It.IsAny<ScheduledJobContext>()))
            .Callback<ScheduledJobContext>(ctx => captured = ctx)
            .Returns(Task.CompletedTask);

        var adapter = new HangfireScheduledJobAdapter(sp, executor.Object, NullLogger<HangfireScheduledJobAdapter>.Instance);

        // Act
        await adapter.RunScheduledJobAsync(
            typeof(FakeScheduledJob).AssemblyQualifiedName!,
            serviceKey: null,
            expectedToken);

        // Assert
        captured.Should().NotBeNull();
        captured!.CancellationToken.Should().Be(expectedToken);
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
        var job = new Mock<IScheduledJob>();
        job.Setup(j => j.ExecuteAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var keyed = new MockKeyedServiceProvider();
        keyed.Register(typeof(TJob), serviceKey, job.Object);
        return keyed;
    }
}
