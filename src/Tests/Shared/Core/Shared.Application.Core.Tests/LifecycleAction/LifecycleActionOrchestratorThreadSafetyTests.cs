// ----------------------------------------------------------------------------------------------
// <copyright file="LifecycleActionOrchestratorThreadSafetyTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.LifecycleAction;
using Shared.Application.Core.LifecycleAction.Interfaces;
using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.Tests.LifecycleAction;

/// <summary>
/// Тесты для thread-safety контракта <see cref="LifecycleActionOrchestrator"/>.
/// </summary>
public sealed class LifecycleActionOrchestratorThreadSafetyTests
{
    /// <summary>
    /// Тестовая сущность.
    /// </summary>
    private sealed class TestEntity
        : IEntity
    {
        private Guid Id { get; } = Guid.NewGuid();

        object IEntity.Id => Id;
    }

    /// <summary>
    /// Handler, фиксирующий количество вызовов через <see cref="Interlocked"/>
    /// (для возможной будущей параллельной валидации, см. remarks класса).
    /// </summary>
    [Shared.Application.Core.DependencyInjection.Attributes.ManualConfiguration]
    private sealed class RecordingHandler
        : ILifecycleActionHandler<TestEntity>
    {
        private int _callCount;

        public int CallCount => _callCount;

        public LifecyclePhase Phase => LifecyclePhase.BeforeSave;

        public string Key => "recording";

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
    /// Single-threaded smoke-test: orchestrator работает в рамках
    /// одного потока и сохраняет инвариант состояния (handler вызван
    /// ровно один раз). Это контрольный baseline для контракта —
    /// single-threaded использование полностью поддерживается.
    /// </summary>
    [Fact]
    public async Task Orchestrator_SingleThreadedUse_HandlerInvokedOnce()
    {
        // Arrange
        var handler = new RecordingHandler();
        var orchestrator = new LifecycleActionOrchestrator(
            [handler],
            new LifecycleEntityRegistry(),
            new LifecycleActionGate());
        orchestrator.AddEntities([new TestEntity(), new TestEntity()]);

        // Act
        await orchestrator.DispatchAsync(LifecyclePhase.BeforeSave, TestContext.Current.CancellationToken);

        // Assert
        handler.CallCount.Should().Be(1, "один handler с одним Order должен быть вызван один раз");
    }
}

