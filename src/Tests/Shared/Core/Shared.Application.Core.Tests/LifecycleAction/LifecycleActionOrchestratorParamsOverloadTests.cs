// ----------------------------------------------------------------------------------------------
// <copyright file="LifecycleActionOrchestratorParamsOverloadTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.LifecycleAction;
using Shared.Application.Core.LifecycleAction.Interfaces;
using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.Tests.LifecycleAction;

/// <summary>
/// Демонстрирует ловушку неоднозначности API orchestrator-а, когда
/// перегрузки <c>EnableActions(params string[])</c> и
/// <c>EnableActions(string[], params IEntity[])</c> конкурируют
/// при вызове с одним строковым аргументом и сущностью.
/// </summary>
/// <remarks>
/// <para>
/// Сценарий проблемы (демонстрируется на ТЕКУЩЕМ API до фикса):
/// </para>
/// <code>
/// orchestrator.AddEntities(new[] { entity });
/// orchestrator.DisableActions("my-key", entity);   // что выберет компилятор?
/// </code>
/// <para>
/// Из-за <c>params string[]</c> компилятор C# привязывает вызов к
/// перегрузке <c>DisableActions(params string[] keys)</c>,
/// <c>entity</c> молча отбрасывается, и попытка отключить действие
/// для конкретной сущности через этот синтаксис не работает.
/// </para>
/// <para>
/// Тест проверяет, что для сущности, отключённой «по задумке» через
/// <c>DisableActions(key, entity)</c>, действие реально отключено —
/// то есть компилятор обязан выбрать перегрузку с entity-аргументом.
/// </para>
/// </remarks>
public sealed class LifecycleActionOrchestratorParamsOverloadTests
{
    /// <summary>
    /// Сущность для теста.
    /// </summary>
    private sealed class TestEntity
        : IEntity
    {
        private Guid Id { get; } = Guid.NewGuid();

        object IEntity.Id => Id;
    }

    /// <summary>
    /// Handler, который фиксирует факт вызова.
    /// </summary>
    private sealed class NoopHandler
        : ILifecycleActionHandler<TestEntity>
    {
        public int CallCount { get; private set; }

        public LifecyclePhase Phase => LifecyclePhase.BeforeSave;

        public string Key => "my-key";

        public int Order => 0;

        Type ILifecycleActionHandler.EntityType => typeof(TestEntity);

        public string[] RequiredNavigationProperties => [];

        public Task ExecuteAsync(
            IEnumerable<IEntity> entities,
            CancellationToken cancellationToken) =>
            ExecuteAsync(
                entities.Cast<TestEntity>(),
                cancellationToken);

        public Task ExecuteAsync(
            IEnumerable<TestEntity> entities,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Фиксирует корректное поведение текущей перегрузки
    /// <c>DisableActions(string[] keys, params IEntity[] entities)</c>:
    /// отключение применяется ТОЛЬКО к указанным сущностям, остальные
    /// остаются активными.
    /// </summary>
    /// <remarks>
    /// Тест одновременно документирует ограничение API: вызов
    /// <c>DisableActions("my-key", target)</c> НЕ КОМПИЛИРУЕТСЯ — приходится
    /// оборачивать ключ в массив. После фикса (замена <c>params</c>
    /// на <c>IReadOnlyList&lt;string&gt;</c> / добавление перегрузки
    /// <c>(string key, IEntity entity)</c>) этот тест нужно дополнить
    /// короткой формой вызова.
    /// </remarks>
    [Fact]
    public async Task DisableActions_WithKeysAndEntity_DisablesOnlyForThatEntity()
    {
        // Arrange
        var target = new TestEntity();
        var other = new TestEntity();
        var handler = new NoopHandler();
        var orchestrator = new LifecycleActionOrchestrator(
            [handler],
            new LifecycleEntityRegistry(),
            new LifecycleActionGate());

        orchestrator.AddEntities([target, other]);

        // Act: короткая форма после фикса API — DisableActionForEntity.
        orchestrator.DisableActionForEntity("my-key", target);

        // Assert
        orchestrator.IsActionEnabled(target, "my-key", LifecyclePhase.BeforeSave)
            .Should().BeFalse("для target действие должно быть отключено персонально");
        orchestrator.IsActionEnabled(other, "my-key", LifecyclePhase.BeforeSave)
            .Should().BeTrue("для other действие НЕ должно быть затронуто");

        await orchestrator.DispatchAsync(LifecyclePhase.BeforeSave, CancellationToken.None);

        handler.CallCount.Should().Be(1, "handler должен быть вызван ровно один раз — для other");
    }

    /// <summary>
    /// Подтверждает, что после фикса API короткая форма
    /// <c>DisableActionForEntity(string, IEntity)</c> компилируется
    /// и работает корректно: действие отключается ТОЛЬКО для указанной
    /// сущности, остальные остаются активными.
    /// </summary>
    [Fact]
    public void DisableActionForEntity_ShortForm_DisablesOnlyForThatEntity()
    {
        // Arrange
        var target = new TestEntity();
        var other = new TestEntity();
        var handler = new NoopHandler();
        var orchestrator = new LifecycleActionOrchestrator(
            [handler],
            new LifecycleEntityRegistry(),
            new LifecycleActionGate());
        orchestrator.AddEntities([target, other]);

        // Act: короткая форма — без обёртки ключа в массив.
        orchestrator.DisableActionForEntity("my-key", target);

        // Assert
        orchestrator.IsActionEnabled(target, "my-key", LifecyclePhase.BeforeSave)
            .Should().BeFalse("для target действие должно быть отключено персонально");
        orchestrator.IsActionEnabled(other, "my-key", LifecyclePhase.BeforeSave)
            .Should().BeTrue("для other действие НЕ должно быть затронуто");
    }

    /// <summary>
    /// Документирует поведение <c>EnableActionForEntity</c>:
    /// снимает ТОЛЬКО ранее установленный per-entity disable.
    /// Не отменяет глобальный <c>DisableActions</c> для конкретной сущности —
    /// per-entity state в gate-е хранится только если был установлен явно.
    /// </summary>
    [Fact]
    public void EnableActionForEntity_ShortForm_WithoutPriorPerEntityDisable_IsNoOp()
    {
        // Arrange
        var target = new TestEntity();
        var other = new TestEntity();
        var orchestrator = new LifecycleActionOrchestrator(
            [],
            new LifecycleEntityRegistry(),
            new LifecycleActionGate());
        orchestrator.AddEntities([target, other]);
        orchestrator.DisableActions(["k"]);

        // Act
        orchestrator.EnableActionForEntity("k", target);

        // Assert
        orchestrator.IsActionEnabled(target, "k", LifecyclePhase.BeforeSave)
            .Should().BeFalse("EnableActionForEntity не отменяет глобальный disable — он снимает только per-entity");
        orchestrator.IsActionEnabled(other, "k", LifecyclePhase.BeforeSave)
            .Should().BeFalse("для other глобальный disable сохраняется");
    }

    /// <summary>
    /// Симметричная проверка для per-entity phase.
    /// </summary>
    [Fact]
    public void DisablePhaseForEntity_ShortForm_DisablesOnlyForThatEntity()
    {
        // Arrange
        var target = new TestEntity();
        var other = new TestEntity();
        var orchestrator = new LifecycleActionOrchestrator(
            [],
            new LifecycleEntityRegistry(),
            new LifecycleActionGate());
        orchestrator.AddEntities([target, other]);

        // Act
        orchestrator.DisablePhaseForEntity(LifecyclePhase.BeforeSave, target);

        // Assert
        orchestrator.IsActionEnabled(target, "any", LifecyclePhase.BeforeSave).Should().BeFalse();
        orchestrator.IsActionEnabled(target, "any", LifecyclePhase.AfterSave).Should().BeTrue();
        orchestrator.IsActionEnabled(other, "any", LifecyclePhase.BeforeSave).Should().BeTrue();
    }

    /// <summary>
    /// Контрольный тест: глобальный <c>DisableActions("my-key")</c>
    /// действительно отключает для всех — фиксирует текущее поведение,
    /// чтобы не сломать его при фиксе.
    /// </summary>
    [Fact]
    public void DisableActions_GlobalKeyOnly_DisablesForAll()
    {
        // Arrange
        var e1 = new TestEntity();
        var e2 = new TestEntity();
        var handler = new NoopHandler();
        var orchestrator = new LifecycleActionOrchestrator(
            [handler],
            new LifecycleEntityRegistry(),
            new LifecycleActionGate());
        orchestrator.AddEntities([e1, e2]);

        // Act
        orchestrator.DisableActions(["my-key"]);

        // Assert
        orchestrator.IsActionEnabled(e1, "my-key", LifecyclePhase.BeforeSave).Should().BeFalse();
        orchestrator.IsActionEnabled(e2, "my-key", LifecyclePhase.BeforeSave).Should().BeFalse();
    }
}
