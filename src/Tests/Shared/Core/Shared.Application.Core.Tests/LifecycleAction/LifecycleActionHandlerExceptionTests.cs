// ----------------------------------------------------------------------------------------------
// <copyright file="LifecycleActionHandlerExceptionTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.LifecycleAction;
using Shared.Application.Core.LifecycleAction.Interfaces;
using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;
using Shared.Testing.Entities;

namespace Shared.Application.Core.Tests.LifecycleAction;

/// <summary>
/// Тесты для error-propagation в <see cref="LifecycleActionOrchestrator.DispatchAsync"/>:
/// что происходит, когда handler выбрасывает исключение.
/// </summary>
/// <remarks>
/// <para>
/// Контракт: orchestrator <b>не глотает</b> исключения handler-ов —
/// проброс наружу, чтобы caller (<c>EfUnitOfWork</c>) мог откатить
/// транзакцию и сбросить состояние.
/// </para>
/// <para>
/// При исключении в handler-е с <c>Order=N</c> handler-ы с
/// <c>Order &gt; N</c> не вызываются (fail-fast).
/// </para>
/// </remarks>
public sealed class LifecycleActionHandlerExceptionTests
{
    /// <summary>
    /// Handler, который бросает исключение при вызове.
    /// </summary>
    [Shared.Application.Core.DependencyInjection.Attributes.ManualConfiguration]
    private sealed class FailingHandler
        : ILifecycleActionHandler<TestEntity>
    {
        public LifecyclePhase Phase => LifecyclePhase.BeforeSave;

        public string Key => "failing";

        public int Order => 0;

        Type ILifecycleActionHandler.EntityType => typeof(TestEntity);

        public string[] RequiredNavigationProperties => [];

        public Task ExecuteAsync(
            IEnumerable<IEntity> entities,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("simulated handler failure");

        public Task ExecuteAsync(
            IEnumerable<TestEntity> entities,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("simulated handler failure");
    }

    /// <summary>
    /// Handler, который бросает исключение при вызове.
    /// </summary>
    [Shared.Application.Core.DependencyInjection.Attributes.ManualConfiguration]
    private sealed class ParameterizedFailingHandler(int order)
        : ILifecycleActionHandler<TestEntity>
    {
        public LifecyclePhase Phase => LifecyclePhase.BeforeSave;

        public string Key => "failing";

        public int Order => order;

        Type ILifecycleActionHandler.EntityType => typeof(TestEntity);

        public string[] RequiredNavigationProperties => [];

        public Task ExecuteAsync(
            IEnumerable<IEntity> entities,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("simulated handler failure");

        public Task ExecuteAsync(
            IEnumerable<TestEntity> entities,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("simulated handler failure");
    }

    /// <summary>
    /// Handler, фиксирующий вызовы в логе.
    /// </summary>
    [Shared.Application.Core.DependencyInjection.Attributes.ManualConfiguration]
    private sealed class RecordingHandler(
        string key,
        int order,
        List<string> callLog)
        : ILifecycleActionHandler<TestEntity>
    {
        public LifecyclePhase Phase => LifecyclePhase.BeforeSave;

        public string Key => key;

        public int Order => order;

        Type ILifecycleActionHandler.EntityType => typeof(TestEntity);

        public string[] RequiredNavigationProperties => [];

        public Task ExecuteAsync(
            IEnumerable<IEntity> entities,
            CancellationToken cancellationToken)
        {
            callLog.Add(Key);
            return Task.CompletedTask;
        }

        public Task ExecuteAsync(
            IEnumerable<TestEntity> entities,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    /// <summary>
    /// <see cref="LifecycleActionOrchestrator.DispatchAsync"/> пробрасывает
    /// исключение handler-а наружу — не глотает, не оборачивает.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_HandlerThrows_ExceptionPropagatesToCaller()
    {
        // Arrange
        var orchestrator = new LifecycleActionOrchestrator(
            [new FailingHandler()],
            new LifecycleEntityRegistry(),
            new LifecycleActionGate());
        orchestrator.AddEntities([new TestEntity { Id = Guid.NewGuid() }]);

        // Act
        var act = () => orchestrator.DispatchAsync(LifecyclePhase.BeforeSave, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("simulated handler failure");
    }

    /// <summary>
    /// Теория: при исключении в одном из handler-ов с Order=N
    /// handler-ы с Order &gt; N не вызываются. Параметризуем
    /// индекс failing-handler-а (0 или 1), чтобы покрыть
    /// начало/середину списка handler-ов.
    /// </summary>
    /// <param name="failingOrder">Order failing-handler-а.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task DispatchAsync_HandlerThrows_SubsequentHandlersNotInvoked(int failingOrder)
    {
        // Arrange
        var callLog = new List<string>();
        var handlers = new List<ILifecycleActionHandler>
        {
            new RecordingHandler("0", order: 0, callLog),
            new ParameterizedFailingHandler(order: failingOrder),
            new RecordingHandler("2", order: 2, callLog),
        };
        var orchestrator = new LifecycleActionOrchestrator(
            handlers,
            new LifecycleEntityRegistry(),
            new LifecycleActionGate());
        orchestrator.AddEntities([new TestEntity { Id = Guid.NewGuid() }]);

        // Act
        var act = () => orchestrator.DispatchAsync(LifecyclePhase.BeforeSave, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        callLog.Should().Equal(["0"],
            "handler с Order=0 уже выполнился до того, как FailingHandler бросил исключение");
    }

    /// <summary>
    /// При исключении handler-ы с меньшим Order уже выполнились —
    /// caller (EfUnitOfWork) откатывает транзакцию.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_HandlerThrows_HandlersWithLowerOrderAlreadyCompleted()
    {
        // Arrange
        var callLog = new List<string>();
        var orchestrator = new LifecycleActionOrchestrator(
        [
            new RecordingHandler("first", order: 0, callLog),
            new FailingHandler(),
        ],
            new LifecycleEntityRegistry(),
            new LifecycleActionGate());
        orchestrator.AddEntities([new TestEntity { Id = Guid.NewGuid() }]);

        // Act
        var act = () => orchestrator.DispatchAsync(LifecyclePhase.BeforeSave, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        callLog.Should().Equal(["first"],
            "первый handler с Order=0 выполнился до того, как FailingHandler бросил исключение");
    }

    /// <summary>
    /// DispatchAsync с пустым реестром и failing-handler-ом —
    /// handler не вызывается, исключения нет.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_NoEligibleEntities_HandlerNotInvoked_NoException()
    {
        // Arrange
        var orchestrator = new LifecycleActionOrchestrator(
            [new FailingHandler()],
            new LifecycleEntityRegistry(),
            new LifecycleActionGate());

        // Act / Assert
        await orchestrator.DispatchAsync(LifecyclePhase.BeforeSave, CancellationToken.None);
    }

    /// <summary>
    /// <see cref="CancellationToken"/>, отменённый ДО старта <c>DispatchAsync</c>:
    /// первый же handler (на итерации) бросает <see cref="OperationCanceledException"/>.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_CancellationTokenAlreadyCancelled_StopsImmediately()
    {
        // Arrange
        var callLog = new List<string>();
        var handlers = new List<ILifecycleActionHandler>
        {
            new RecordingHandler("0", order: 0, callLog),
            new RecordingHandler("1", order: 1, callLog),
        };
        var orchestrator = new LifecycleActionOrchestrator(
            handlers,
            new LifecycleEntityRegistry(),
            new LifecycleActionGate());
        orchestrator.AddEntities([new TestEntity { Id = Guid.NewGuid() }]);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = () => orchestrator.DispatchAsync(LifecyclePhase.BeforeSave, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    /// <see cref="CancellationToken"/>, отменённый ВНУТРИ handler-а с Order=0:
    /// handler-ы с Order &gt; 0 не вызываются.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_CancellationTriggeredInsideHandler_StopsSubsequentHandlers()
    {
        // Arrange
        var callLog = new List<string>();
        using var cts = new CancellationTokenSource();

        var handlers = new List<ILifecycleActionHandler>
        {
            new CancellingHandler("0", order: 0, callLog, cts),
            new RecordingHandler("1", order: 1, callLog),
            new RecordingHandler("2", order: 2, callLog),
        };
        var orchestrator = new LifecycleActionOrchestrator(
            handlers,
            new LifecycleEntityRegistry(),
            new LifecycleActionGate());
        orchestrator.AddEntities([new TestEntity { Id = Guid.NewGuid() }]);

        // Act
        var act = () => orchestrator.DispatchAsync(LifecyclePhase.BeforeSave, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        callLog.Should().Equal(["0"],
            "handler с Order=0 вызвался, отменил токен, dispatcher проверил токен и остановился");
    }

    /// <summary>
    /// Handler, который отменяет переданный <see cref="CancellationToken"/>
    /// при первом вызове.
    /// </summary>
    [Shared.Application.Core.DependencyInjection.Attributes.ManualConfiguration]
    private sealed class CancellingHandler(
        string key,
        int order,
        List<string> callLog,
        CancellationTokenSource cts)
        : ILifecycleActionHandler<TestEntity>
    {
        public LifecyclePhase Phase => LifecyclePhase.BeforeSave;

        public string Key => key;

        public int Order => order;

        Type ILifecycleActionHandler.EntityType => typeof(TestEntity);

        public string[] RequiredNavigationProperties => [];

        public Task ExecuteAsync(
            IEnumerable<IEntity> entities,
            CancellationToken cancellationToken)
        {
            callLog.Add(key);
            cts.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public Task ExecuteAsync(
            IEnumerable<TestEntity> entities,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
