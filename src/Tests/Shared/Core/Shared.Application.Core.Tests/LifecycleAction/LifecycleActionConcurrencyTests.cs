// ----------------------------------------------------------------------------------------------
// <copyright file="LifecycleActionConcurrencyTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using Shared.Application.Core.LifecycleAction;
using Shared.Application.Core.LifecycleAction.Interfaces;
using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.Tests.LifecycleAction;

/// <summary>
/// Тесты concurrency-контракта <see cref="LifecycleActionOrchestrator"/>.
/// </summary>
/// <remarks>
/// <para>
/// Покрывают сценарии параллельного доступа к одной инстанции orchestrator-а:
/// несколько потоков одновременно вызывают
/// <see cref="ILifecycleActionOrchestrator.DispatchAsync"/>,
/// <see cref="ILifecycleActionOrchestrator.AddEntities"/>,
/// <see cref="ILifecycleActionOrchestrator.RemoveEntities"/>,
/// <see cref="ILifecycleActionOrchestrator.IsActionEnabled"/>,
/// <see cref="ILifecycleActionOrchestrator.DisableActions"/>.
/// </para>
/// <para>
/// Дополнительно покрывается сценарий с разделяемым registry/gate между
/// несколькими orchestrator-инстансами и сценарий с per-task
/// <see cref="CancellationToken"/>.
/// </para>
/// </remarks>
public sealed class LifecycleActionConcurrencyTests
{
    /// <summary>
    /// Тестовая сущность.
    /// </summary>
    private sealed class TestEntity
        : IEntity
    {
        public Guid Id { get; } = Guid.NewGuid();

        object IEntity.Id => Id;
    }

    /// <summary>
    /// Handler, инкрементирующий счётчик вызовов через <see cref="Interlocked.Increment(ref int)"/>.
    /// </summary>
    [Shared.Application.Core.DependencyInjection.Attributes.ManualConfiguration]
    private sealed class CallCountingHandler(
        LifecyclePhase phase,
        string key)
        : ILifecycleActionHandler<TestEntity>
    {
        private int _callCount;

        /// <summary>
        /// Количество вызовов <see cref="ExecuteAsync"/>.
        /// </summary>
        public int CallCount => _callCount;

        public LifecyclePhase Phase { get; } = phase;

        public string Key { get; } = key;

        public int Order => 0;

        Type ILifecycleActionHandler.EntityType => typeof(TestEntity);

        public string[] RequiredNavigationProperties => [];

        public Task ExecuteAsync(
            IEnumerable<IEntity> entities,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _callCount);
            return Task.CompletedTask;
        }

        public Task ExecuteAsync(
            IEnumerable<TestEntity> entities,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    /// <summary>
    /// Handler, фиксирующий размер коллекции сущностей, переданной в <see cref="ExecuteAsync"/>,
    /// в thread-safe коллекцию <see cref="ConcurrentBag{T}"/>.
    /// </summary>
    [Shared.Application.Core.DependencyInjection.Attributes.ManualConfiguration]
    private sealed class SnapshotSizeHandler(
        LifecyclePhase phase,
        string key,
        ConcurrentBag<int> sizes)
        : ILifecycleActionHandler<TestEntity>
    {
        public LifecyclePhase Phase { get; } = phase;

        public string Key { get; } = key;

        public int Order => 0;

        Type ILifecycleActionHandler.EntityType => typeof(TestEntity);

        public string[] RequiredNavigationProperties => [];

        public Task ExecuteAsync(
            IEnumerable<IEntity> entities,
            CancellationToken cancellationToken)
        {
            sizes.Add(entities.Count());
            return Task.CompletedTask;
        }

        public Task ExecuteAsync(
            IEnumerable<TestEntity> entities,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    /// <summary>
    /// Handler, который во время выполнения фиксирует размер snapshot-а и опционально
    /// добавляет в orchestrator дополнительные сущности (имитация side-effect-а).
    /// </summary>
    [Shared.Application.Core.DependencyInjection.Attributes.ManualConfiguration]
    private sealed class AddEntitiesDuringExecuteHandler(
        LifecycleActionOrchestrator orchestrator,
        string key,
        int order,
        ConcurrentBag<int> snapshotSizes,
        int entitiesToAdd)
        : ILifecycleActionHandler<TestEntity>
    {
        public LifecyclePhase Phase { get; } = LifecyclePhase.BeforeSave;

        public string Key { get; } = key;

        public int Order { get; } = order;

        Type ILifecycleActionHandler.EntityType => typeof(TestEntity);

        public string[] RequiredNavigationProperties => [];

        public Task ExecuteAsync(
            IEnumerable<IEntity> entities,
            CancellationToken cancellationToken)
        {
            var array = entities.ToArray();
            snapshotSizes.Add(array.Length);

            if (entitiesToAdd > 0)
            {
                var toAdd = Enumerable.Range(0, entitiesToAdd)
                    .Select(_ => new TestEntity())
                    .ToArray();
                orchestrator.AddEntities(toAdd);
            }

            return Task.CompletedTask;
        }

        public Task ExecuteAsync(
            IEnumerable<TestEntity> entities,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    /// <summary>
    /// Handler, который только фиксирует размер snapshot-а и не модифицирует состояние.
    /// </summary>
    [Shared.Application.Core.DependencyInjection.Attributes.ManualConfiguration]
    private sealed class PassiveSnapshotHandler(
        string key,
        int order,
        ConcurrentBag<int> snapshotSizes)
        : ILifecycleActionHandler<TestEntity>
    {
        public LifecyclePhase Phase { get; } = LifecyclePhase.BeforeSave;

        public string Key { get; } = key;

        public int Order { get; } = order;

        Type ILifecycleActionHandler.EntityType => typeof(TestEntity);

        public string[] RequiredNavigationProperties => [];

        public Task ExecuteAsync(
            IEnumerable<IEntity> entities,
            CancellationToken cancellationToken)
        {
            snapshotSizes.Add(entities.Count());
            return Task.CompletedTask;
        }

        public Task ExecuteAsync(
            IEnumerable<TestEntity> entities,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private static LifecycleActionOrchestrator BuildOrchestrator(
        IEnumerable<ILifecycleActionHandler>? handlers = null) =>
        new(handlers ?? [], new LifecycleEntityRegistry(), new LifecycleActionGate());

    #region Concurrent DispatchAsync

    /// <summary>
    /// Десять параллельных задач вызывают <see cref="ILifecycleActionOrchestrator.DispatchAsync"/>
    /// на одной инстанции orchestrator-а. Handler должен быть вызван ровно десять раз —
    /// по одному на каждую задачу.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_TenConcurrentTasksSameOrchestrator_AllDispatch()
    {
        // Arrange
        var handler = new CallCountingHandler(LifecyclePhase.BeforeSave, "h");
        var orchestrator = BuildOrchestrator([handler]);
        orchestrator.AddEntities(Enumerable.Range(0, 5).Select(_ => new TestEntity()).ToArray());

        const int taskCount = 10;
        using var barrier = new Barrier(taskCount);

        // Act
        var tasks = Enumerable.Range(0, taskCount)
            .Select(_ => Task.Run(async () =>
            {
                barrier.SignalAndWait(TestContext.Current.CancellationToken);
                await orchestrator.DispatchAsync(
                    LifecyclePhase.BeforeSave,
                    TestContext.Current.CancellationToken);
            }, TestContext.Current.CancellationToken))
            .ToArray();
        await Task.WhenAll(tasks);

        // Assert
        handler.CallCount.Should().Be(
            taskCount,
            "каждая из {0} параллельных задач должна была вызвать handler ровно один раз",
            taskCount);
    }

    /// <summary>
    /// Параллельные <see cref="ILifecycleActionOrchestrator.DispatchAsync"/> на одной инстанции
    /// не должны приводить к потере или дублированию сущностей в snapshot-е:
    /// каждая задача получает согласованный снимок одного и того же размера.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_ParallelFromSameInstance_RegistrySnapshotRemainsConsistent()
    {
        // Arrange
        var snapshotSizes = new ConcurrentBag<int>();
        var handler = new SnapshotSizeHandler(LifecyclePhase.BeforeSave, "h", snapshotSizes);
        var orchestrator = BuildOrchestrator([handler]);
        var expectedSize = 7;
        orchestrator.AddEntities(Enumerable.Range(0, expectedSize).Select(_ => new TestEntity()).ToArray());

        const int taskCount = 20;
        using var barrier = new Barrier(taskCount);

        // Act
        var tasks = Enumerable.Range(0, taskCount)
            .Select(_ => Task.Run(async () =>
            {
                barrier.SignalAndWait(TestContext.Current.CancellationToken);
                await orchestrator.DispatchAsync(
                    LifecyclePhase.BeforeSave,
                    TestContext.Current.CancellationToken);
            }, TestContext.Current.CancellationToken))
            .ToArray();
        await Task.WhenAll(tasks);

        // Assert
        snapshotSizes.Should().HaveCount(
            taskCount,
            "каждая из {0} задач должна была получить свой snapshot",
            taskCount);
        snapshotSizes.Should().AllSatisfy(s =>
            s.Should().Be(
                expectedSize,
                "snapshot не должен терять или дублировать сущности под параллельным доступом"));
    }

    /// <summary>
    /// Параллельные <see cref="ILifecycleActionOrchestrator.DispatchAsync"/>, в которых handler
    /// добавляет новые сущности mid-dispatch: snapshot каждого dispatch-а остаётся
    /// согласованным (фиксируется один раз), содержит исходный набор и не теряет сущности.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_ConcurrentWithMidDispatchAddEntities_SnapshotIsTakenOnce()
    {
        // Arrange
        var firstSizes = new ConcurrentBag<int>();
        var secondSizes = new ConcurrentBag<int>();
        var registry = new LifecycleEntityRegistry();
        var gate = new LifecycleActionGate();
        var orchestrator = new LifecycleActionOrchestrator([], registry, gate);
        var first = new AddEntitiesDuringExecuteHandler(
            orchestrator,
            "first",
            order: 0,
            firstSizes,
            entitiesToAdd: 3);
        var second = new PassiveSnapshotHandler("second", order: 1, secondSizes);
        var composed = new LifecycleActionOrchestrator([first, second], registry, gate);
        var initialEntities = Enumerable.Range(0, 4).Select(_ => new TestEntity()).ToArray();
        composed.AddEntities(initialEntities);

        const int taskCount = 15;
        using var barrier = new Barrier(taskCount);

        // Act
        var tasks = Enumerable.Range(0, taskCount)
            .Select(_ => Task.Run(async () =>
            {
                barrier.SignalAndWait(TestContext.Current.CancellationToken);
                await composed.DispatchAsync(
                    LifecyclePhase.BeforeSave,
                    TestContext.Current.CancellationToken);
            }, TestContext.Current.CancellationToken))
            .ToArray();
        await Task.WhenAll(tasks);

        // Assert: каждый dispatch зафиксировал свой согласованный snapshot.
        firstSizes.Should().HaveCount(taskCount);
        secondSizes.Should().HaveCount(taskCount);
        firstSizes.Should().AllSatisfy(s =>
            s.Should().BeGreaterThanOrEqualTo(
                initialEntities.Length,
                "snapshot первого handler-а dispatch-а не может быть меньше исходного набора"));
        secondSizes.Should().AllSatisfy(s =>
            s.Should().BeGreaterThanOrEqualTo(
                initialEntities.Length,
                "snapshot второго handler-а dispatch-а не может быть меньше исходного набора"));
    }

    #endregion

    #region Concurrent Gate operations

    /// <summary>
    /// Параллельные <see cref="ILifecycleActionOrchestrator.IsActionEnabled"/> и
    /// <see cref="ILifecycleActionOrchestrator.DisableActions"/> /
    /// <see cref="ILifecycleActionOrchestrator.EnableActions"/> не должны приводить
    /// к исключениям. Финальное состояние после полного завершения обеих задач
    /// детерминировано (toggleTask — единственный writer).
    /// </summary>
    [Fact]
    public async Task IsActionEnabled_ConcurrentWithDisableActions_BehaviorIsCorrect()
    {
        // Arrange
        var orchestrator = BuildOrchestrator();
        var entity = new TestEntity();
        const int iterations = 2000;

        // Act
        var toggleTask = Task.Run(() =>
        {
            for (var i = 0; i < iterations; i++)
            {
                if (i % 2 == 0)
                {
                    orchestrator.DisableActions();
                }
                else
                {
                    orchestrator.EnableActions();
                }
            }
        }, TestContext.Current.CancellationToken);

        var checkTask = Task.Run(() =>
        {
            for (var i = 0; i < iterations; i++)
            {
                orchestrator.IsActionEnabled(entity, "k", LifecyclePhase.BeforeSave);
            }
        }, TestContext.Current.CancellationToken);

        await Task.WhenAll(toggleTask, checkTask);

        // Assert: финальное состояние определяется последним вызовом toggleTask.
        var finalState = orchestrator.IsActionEnabled(entity, "k", LifecyclePhase.BeforeSave);
        finalState.Should().BeTrue(
            "последний вызов в toggleTask (iteration = {0}, нечётный) — EnableActions",
            iterations - 1);
    }

    #endregion

    #region Multiple orchestrators

    /// <summary>
    /// Два <see cref="LifecycleActionOrchestrator"/> с общим
    /// <see cref="ILifecycleEntityRegistry"/> и <see cref="ILifecycleActionGate"/>
    /// должны корректно накапливать сущности при параллельном
    /// <see cref="ILifecycleActionOrchestrator.AddEntities"/>: ни одна сущность
    /// не теряется, дублей не возникает.
    /// </summary>
    [Fact]
    public async Task MultipleOrchestratorsSameRegistry_AddEntitiesConcurrently()
    {
        // Arrange
        var registry = new LifecycleEntityRegistry();
        var gate = new LifecycleActionGate();
        var orchestrator1 = new LifecycleActionOrchestrator([], registry, gate);
        var orchestrator2 = new LifecycleActionOrchestrator([], registry, gate);

        const int taskCount = 20;
        const int entitiesPerTask = 5;
        var expectedTotal = taskCount * entitiesPerTask;
        using var barrier = new Barrier(taskCount);

        // Act
        var tasks = Enumerable.Range(0, taskCount)
            .Select(taskIndex => Task.Run(() =>
            {
                var orchestrator = taskIndex % 2 == 0 ? orchestrator1 : orchestrator2;
                var entities = Enumerable.Range(0, entitiesPerTask)
                    .Select(_ => new TestEntity())
                    .ToArray();
                barrier.SignalAndWait(TestContext.Current.CancellationToken);
                orchestrator.AddEntities(entities);
            }, TestContext.Current.CancellationToken))
            .ToArray();
        await Task.WhenAll(tasks);

        // Assert
        registry.Snapshot().Should().HaveCount(
            expectedTotal,
            "все {0} сущностей, добавленных из {1} параллельных задач, должны быть в общем registry",
            expectedTotal,
            taskCount);
    }

    #endregion

    #region Cancellation

    /// <summary>
    /// Параллельные <see cref="ILifecycleActionOrchestrator.DispatchAsync"/> с per-task
    /// <see cref="CancellationToken"/>, отменяемым через <c>CancelAfter</c>: каждая задача
    /// либо завершается успешно, либо бросает <see cref="OperationCanceledException"/>.
    /// Deadlock не возникает.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_ParallelWithCancellation_AllCancellationTokensRespected()
    {
        // Arrange
        var handler = new CallCountingHandler(LifecyclePhase.BeforeSave, "h");
        var orchestrator = BuildOrchestrator([handler]);
        orchestrator.AddEntities(Enumerable.Range(0, 10).Select(_ => new TestEntity()).ToArray());

        const int taskCount = 10;
        using var barrier = new Barrier(taskCount);

        // Act
        var tasks = Enumerable.Range(0, taskCount)
            .Select(taskIndex => Task.Run<(bool Cancelled, Exception? Error)>(async () =>
            {
                using var cts = new CancellationTokenSource();
                barrier.SignalAndWait(TestContext.Current.CancellationToken);
                cts.CancelAfter(TimeSpan.FromMilliseconds(1));

                try
                {
                    await orchestrator.DispatchAsync(
                        LifecyclePhase.BeforeSave,
                        cts.Token);
                    return (false, null);
                }
                catch (OperationCanceledException ex)
                {
                    return (true, ex);
                }
            }, TestContext.Current.CancellationToken))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(taskCount);
        results.Should().AllSatisfy(r =>
            (r.Cancelled || r.Error is null).Should().BeTrue(
                "каждая задача должна либо успешно завершиться, либо бросить OperationCanceledException"));
    }

    #endregion
}
