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
/// <remarks>
/// <para>
/// Контракт (см. XML-doc в <see cref="ILifecycleActionOrchestrator"/>):
/// orchestrator <b>не потокобезопасен</b> и должен использоваться
/// в рамках одного scoped-контекста. Эти тесты <b>не валидируют</b>
/// thread-safety как таковой — они лишь фиксируют single-threaded baseline
/// и документируют, что параллельное использование не поддерживается
/// контрактом.
/// </para>
/// <para>
/// Если в будущем потребуется параллельная обработка:
/// </para>
/// <list type="number">
///   <item>Сериализовать доступ на стороне caller-а (lock/SemaphoreSlim);</item>
///   <item>Либо реализовать отдельный thread-safe вариант.</item>
/// </list>
/// </remarks>
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
        await orchestrator.DispatchAsync(LifecyclePhase.BeforeSave, CancellationToken.None);

        // Assert
        handler.CallCount.Should().Be(1, "один handler с одним Order должен быть вызван один раз");
    }

    /// <summary>
    /// Документирует контракт: orchestrator спроектирован для scoped-lifetime
    /// и не предоставляет API для concurrent dispatch. Это guard-тест: если
    /// кто-то в будущем добавит, например, <c>DispatchConcurrentAsync</c>,
    /// этот тест напомнит о необходимости пересмотреть thread-safety контракт.
    /// </summary>
    /// <remarks>
    /// Не запускает реальный параллельный код (это было бы flaky).
    /// Проверяет структурный инвариант контракта.
    /// </remarks>
    [Fact]
    public void Orchestrator_DoesNotExposeConcurrentDispatchApi()
    {
        // Act
        var methodNames = typeof(ILifecycleActionOrchestrator)
            .GetMethods()
            .Select(m => m.Name)
            .Distinct()
            .ToArray();

        // Assert
        methodNames.Should().NotContain(n => n.Contains("Parallel", StringComparison.Ordinal));
        methodNames.Should().NotContain(n => n.Contains("Concurrent", StringComparison.Ordinal));
    }
}

