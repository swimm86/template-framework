// ----------------------------------------------------------------------------------------------
// <copyright file="ScheduledJobExecutorTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.Job.Interfaces;
using Shared.Application.Core.Job.Pipeline;
using Shared.Application.Core.Job.Pipeline.Interfaces;

namespace Shared.Application.Core.Tests;

/// <summary>
/// Тесты <see cref="ScheduledJobExecutor"/>: порядок middleware и выполнение терминала.
/// </summary>
public sealed class ScheduledJobExecutorTests
{
    /// <summary>
    /// Первый зарегистрированный middleware выполняется первым и последним.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ThreeMiddlewares_OrderIsOuterFirst()
    {
        // Arrange
        var sequence = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton<IScheduledJobMiddleware>(_ => new SequenceMiddleware("A", sequence));
        services.AddSingleton<IScheduledJobMiddleware>(_ => new SequenceMiddleware("B", sequence));
        services.AddSingleton<IScheduledJobMiddleware>(_ => new SequenceMiddleware("C", sequence));

        var sp = services.BuildServiceProvider();
        var executor = ActivatorUtilities.CreateInstance<ScheduledJobExecutor>(sp);

        var ctx = new ScheduledJobContext(
            jobKey: "test",
            serviceProvider: sp,
            cancellationToken: CancellationToken.None)
        {
            Action = (_, _) =>
            {
                sequence.Add("TERMINAL");
                return Task.CompletedTask;
            },
        };

        // Act
        await executor.ExecuteAsync(ctx);

        // Assert
        sequence.Should().Equal("A:before", "B:before", "C:before", "TERMINAL", "C:after", "B:after", "A:after");
    }

    /// <summary>
    /// Лямбда-действие выполняется, если JobType == null.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_LambdaAction_InvokesAction()
    {
        // Arrange
        var invoked = false;
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();
        var executor = ActivatorUtilities.CreateInstance<ScheduledJobExecutor>(sp);

        var ctx = new ScheduledJobContext("lambda", sp, CancellationToken.None)
        {
            Action = (_, _) =>
            {
                invoked = true;
                return Task.CompletedTask;
            },
        };

        // Act
        await executor.ExecuteAsync(ctx);

        // Assert
        invoked.Should().BeTrue();
    }

    /// <summary>
    /// Классовая джоба резолвится из DI по типу.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ClassJob_ResolvesFromDi()
    {
        // Arrange
        var counter = new Counter();
        var services = new ServiceCollection();
        services.AddSingleton(counter);
        services.AddSingleton<FakeJob>();
        var sp = services.BuildServiceProvider();
        var executor = ActivatorUtilities.CreateInstance<ScheduledJobExecutor>(sp);

        var ctx = new ScheduledJobContext("class", sp, CancellationToken.None)
        {
            JobType = typeof(FakeJob),
        };

        // Act
        await executor.ExecuteAsync(ctx);

        // Assert
        counter.Value.Should().Be(1);
    }

    /// <summary>
    /// Keyed-классовая джоба резолвится из DI по ключу.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ClassJobWithServiceKey_ResolvesKeyed()
    {
        // Arrange
        var counter = new Counter();
        var services = new ServiceCollection();
        services.AddSingleton(counter);
        services.AddKeyedSingleton<IScheduledJob, FakeJob>("key1");
        var sp = services.BuildServiceProvider();
        var executor = ActivatorUtilities.CreateInstance<ScheduledJobExecutor>(sp);

        var ctx = new ScheduledJobContext("keyed", sp, CancellationToken.None)
        {
            JobType = typeof(FakeJob),
            ServiceKey = "key1",
        };

        // Act
        await executor.ExecuteAsync(ctx);

        // Assert
        counter.Value.Should().Be(1);
    }

    /// <summary>
    /// Keyed-сервис не найден в DI — <see cref="InvalidOperationException"/> содержит
    /// <c>JobKey</c>, ключ и тип для упрощения диагностики.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_MissingKeyedService_ErrorContainsJobKey()
    {
        // Arrange
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();
        var executor = ActivatorUtilities.CreateInstance<ScheduledJobExecutor>(sp);

        var ctx = new ScheduledJobContext("billing-job", sp, CancellationToken.None)
        {
            JobType = typeof(FakeJob),
            ServiceKey = "missing",
        };

        // Act
        var act = () => executor.ExecuteAsync(ctx);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*billing-job*")
            .WithMessage("*missing*")
            .WithMessage("*FakeJob*");
    }

    /// <summary>
    /// Счётчик, шаримый между тестом и <see cref="FakeJob"/>.
    /// </summary>
    private sealed class Counter
    {
        public int Value { get; set; }
    }

    /// <summary>
    /// Middleware, логирующий свой вход/выход в общий список.
    /// </summary>
    private sealed class SequenceMiddleware : IScheduledJobMiddleware
    {
        private readonly string _name;
        private readonly List<string> _sequence;

        public SequenceMiddleware(string name, List<string> sequence)
        {
            _name = name;
            _sequence = sequence;
        }

        public async Task InvokeAsync(ScheduledJobContext context, ScheduledJobDelegate next)
        {
            _sequence.Add($"{_name}:before");
            await next(context);
            _sequence.Add($"{_name}:after");
        }
    }

    /// <summary>
    /// Тестовая классовая джоба, считающая число вызовов через инжектируемый <see cref="Counter"/>.
    /// </summary>
    private sealed class FakeJob : IScheduledJob
    {
        private readonly Counter _counter;

        public FakeJob(Counter counter)
        {
            _counter = counter ?? throw new ArgumentNullException(nameof(counter));
        }

        public Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            _counter.Value++;
            return Task.CompletedTask;
        }
    }
}