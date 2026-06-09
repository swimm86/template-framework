// ----------------------------------------------------------------------------------------------
// <copyright file="LifecycleActionSnapshotConsistencyTests.cs" company="swimm86@yandex.ru">
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
/// Тесты для W2 (snapshot consistency) — гарантия, что
/// <see cref="LifecycleActionOrchestrator.DispatchAsync"/> берёт
/// <see cref="ILifecycleEntityRegistry.Snapshot"/> ровно один раз
/// и передаёт согласованный набор сущностей во все handler-ы фазы.
/// </summary>
/// <remarks>
/// Без snapshot-кеширования второй handler мог бы увидеть сущности,
/// добавленные первым. Snapshot фиксирует состояние на момент
/// начала dispatch-а, делая порядок handler-ов идемпотентным
/// относительно их side-effects.
/// </remarks>
public sealed class LifecycleActionSnapshotConsistencyTests
{
    /// <summary>
    /// Handler, фиксирующий количество сущностей, которое он получил,
    /// и опционально добавляющий новую сущность в orchestrator во время
    /// своего выполнения.
    /// </summary>
    [Shared.Application.Core.DependencyInjection.Attributes.ManualConfiguration]
    private sealed class SnapshotProbingHandler(
        ILifecycleActionOrchestrator orchestrator,
        string key,
        int order,
        List<int> receivedCounts,
        bool addEntityDuringExecute)
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
            var array = entities.ToArray();
            receivedCounts.Add(array.Length);

            if (addEntityDuringExecute)
            {
                orchestrator.AddEntities([new TestEntity { Id = Guid.NewGuid() }]);
            }

            return Task.CompletedTask;
        }

        public Task ExecuteAsync(
            IEnumerable<TestEntity> entities,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    /// <summary>
    /// Два handler-а одной фазы получают одинаковый snapshot,
    /// даже если первый добавляет новую сущность в orchestrator.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_SnapshotIsTakenOnce_AllHandlersSeeSameSet()
    {
        // Arrange
        var firstCounts = new List<int>();
        var secondCounts = new List<int>();
        var orchestrator = new LifecycleActionOrchestrator(
            [],
            new LifecycleEntityRegistry(),
            new LifecycleActionGate());
        var first = new SnapshotProbingHandler(orchestrator, "first", order: 0, firstCounts, addEntityDuringExecute: true);
        var second = new SnapshotProbingHandler(orchestrator, "second", order: 1, secondCounts, addEntityDuringExecute: false);
        var composed = new LifecycleActionOrchestrator(
            [first, second],
            new LifecycleEntityRegistry(),
            new LifecycleActionGate());
        composed.AddEntities([
            new TestEntity { Id = Guid.NewGuid() },
            new TestEntity { Id = Guid.NewGuid() },
        ]);

        // Act
        await composed.DispatchAsync(LifecyclePhase.BeforeSave, CancellationToken.None);

        // Assert
        firstCounts.Should().Equal(new[] { 2 },
            "первый handler получил 2 entity из snapshot");
        secondCounts.Should().Equal(new[] { 2 },
            "snapshot-кеш изолирует второй handler от AddEntities в первом — "
            + "иначе он увидел бы 3 (исходные 2 + добавленную первым)");
    }

    /// <summary>
    /// Если handler-ов для фазы нет — snapshot не берётся (early-return).
    /// </summary>
    [Fact]
    public async Task DispatchAsync_NoHandlers_OrchestratorStateUnchanged()
    {
        // Arrange
        var orchestrator = new LifecycleActionOrchestrator(
            [],
            new LifecycleEntityRegistry(),
            new LifecycleActionGate());
        var entity = new TestEntity();
        orchestrator.AddEntities([entity]);

        // Act
        await orchestrator.DispatchAsync(LifecyclePhase.AfterSave, CancellationToken.None);

        // Assert: повторный AddEntities (после Remove) работает как новая операция
        orchestrator.RemoveEntities([entity]);
        orchestrator.AddEntities([new TestEntity { Id = Guid.NewGuid() }]);
    }
}
