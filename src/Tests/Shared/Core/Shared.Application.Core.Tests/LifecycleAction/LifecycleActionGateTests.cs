// ----------------------------------------------------------------------------------------------
// <copyright file="LifecycleActionGateTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.LifecycleAction;
using Shared.Application.Core.LifecycleAction.Interfaces;
using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.Tests.LifecycleAction;

/// <summary>
/// Тесты для <see cref="LifecycleActionGate"/>: цепочка приоритетов
/// (per-entity → per-key → per-phase → global), null-safety,
/// <see cref="ILifecycleActionGate.Forget"/>, <see cref="ILifecycleActionGate.Reset"/>.
/// </summary>
public sealed class LifecycleActionGateTests
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
    /// Свежий gate по умолчанию разрешает все действия.
    /// </summary>
    [Fact]
    public void IsEnabled_FreshGate_ReturnsTrue()
    {
        // Arrange
        var gate = new LifecycleActionGate();
        var entity = new TestEntity();

        // Act
        var enabled = gate.IsEnabled(entity, "any-key", LifecyclePhase.BeforeSave);

        // Assert
        enabled.Should().BeTrue();
    }

    /// <summary>
    /// <see cref="ILifecycleActionGate.IsEnabled"/> выбрасывает
    /// <see cref="ArgumentNullException"/> при <c>null</c> сущности.
    /// </summary>
    [Fact]
    public void IsEnabled_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var gate = new LifecycleActionGate();

        // Act
        Action act = () => gate.IsEnabled(null!, "k", LifecyclePhase.BeforeSave);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Глобальный <see cref="ILifecycleActionGate.Disable()"/> отключает
    /// любые действия.
    /// </summary>
    [Fact]
    public void Disable_DisablesAll()
    {
        // Arrange
        var gate = new LifecycleActionGate();
        var entity = new TestEntity();
        gate.Disable();

        // Act
        var enabled = gate.IsEnabled(entity, "k", LifecyclePhase.BeforeSave);

        // Assert
        enabled.Should().BeFalse();
    }

    /// <summary>
    /// <see cref="ILifecycleActionGate.Enable()"/> восстанавливает
    /// глобальный флаг.
    /// </summary>
    [Fact]
    public void Enable_AfterDisable_ReEnablesAll()
    {
        // Arrange
        var gate = new LifecycleActionGate();
        var entity = new TestEntity();
        gate.Disable();
        gate.Enable();

        // Act
        var enabled = gate.IsEnabled(entity, "k", LifecyclePhase.BeforeSave);

        // Assert
        enabled.Should().BeTrue();
    }

    /// <summary>
    /// <see cref="ILifecycleActionGate.Disable(IReadOnlyList{string})"/> отключает
    /// только указанный ключ, остальные остаются активными.
    /// </summary>
    [Fact]
    public void Disable_ByKey_DisablesOnlyThatKey()
    {
        // Arrange
        var gate = new LifecycleActionGate();
        var entity = new TestEntity();
        gate.Disable(["a"]);

        // Act
        var aEnabled = gate.IsEnabled(entity, "a", LifecyclePhase.BeforeSave);
        var bEnabled = gate.IsEnabled(entity, "b", LifecyclePhase.BeforeSave);

        // Assert
        aEnabled.Should().BeFalse();
        bEnabled.Should().BeTrue();
    }

    /// <summary>
    /// <see cref="ILifecycleActionGate.Enable(IReadOnlyList{string})"/> возвращает
    /// ранее отключённый ключ.
    /// </summary>
    [Fact]
    public void Enable_ByKey_RestoresThatKey()
    {
        // Arrange
        var gate = new LifecycleActionGate();
        var entity = new TestEntity();
        gate.Disable(["a"]);
        gate.Enable(["a"]);

        // Act
        var enabled = gate.IsEnabled(entity, "a", LifecyclePhase.BeforeSave);

        // Assert
        enabled.Should().BeTrue();
    }

    /// <summary>
    /// <see cref="ILifecycleActionGate.Enable(IReadOnlyList{string})"/> для ключа,
    /// который не был отключён — безопасная no-op.
    /// </summary>
    [Fact]
    public void Enable_ByKey_NotPreviouslyDisabled_IsNoOp()
    {
        // Arrange
        var gate = new LifecycleActionGate();
        var entity = new TestEntity();

        // Act
        Action act = () => gate.Enable(["never-disabled"]);

        // Assert
        act.Should().NotThrow();
        gate.IsEnabled(entity, "never-disabled", LifecyclePhase.BeforeSave).Should().BeTrue();
    }

    /// <summary>
    /// <see cref="ILifecycleActionGate.Disable(IReadOnlyList{string})"/> выбрасывает
    /// <see cref="ArgumentNullException"/> при <c>null</c>.
    /// </summary>
    [Fact]
    public void Disable_ByKey_Null_ThrowsArgumentNullException()
    {
        // Arrange
        var gate = new LifecycleActionGate();

        // Act
        Action act = () => gate.Disable(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// <see cref="ILifecycleActionGate.DisablePhase"/> отключает
    /// только указанную фазу.
    /// </summary>
    [Fact]
    public void DisablePhase_DisablesOnlyThatPhase()
    {
        // Arrange
        var gate = new LifecycleActionGate();
        var entity = new TestEntity();
        gate.DisablePhase(LifecyclePhase.BeforeSave);

        // Act
        var before = gate.IsEnabled(entity, "k", LifecyclePhase.BeforeSave);
        var after = gate.IsEnabled(entity, "k", LifecyclePhase.AfterSave);

        // Assert
        before.Should().BeFalse();
        after.Should().BeTrue();
    }

    /// <summary>
    /// <see cref="ILifecycleActionGate.EnablePhase"/> восстанавливает фазу.
    /// </summary>
    [Fact]
    public void EnablePhase_RestoresThatPhase()
    {
        // Arrange
        var gate = new LifecycleActionGate();
        var entity = new TestEntity();
        gate.DisablePhase(LifecyclePhase.BeforeSave);
        gate.EnablePhase(LifecyclePhase.BeforeSave);

        // Act
        var enabled = gate.IsEnabled(entity, "k", LifecyclePhase.BeforeSave);

        // Assert
        enabled.Should().BeTrue();
    }

    /// <summary>
    /// <see cref="ILifecycleActionGate.DisableForEntity"/> отключает ключ
    /// только для указанной сущности; другие сущности остаются активными.
    /// </summary>
    [Fact]
    public void DisableForEntity_DisablesOnlyForThatEntity()
    {
        // Arrange
        var gate = new LifecycleActionGate();
        var target = new TestEntity();
        var other = new TestEntity();

        // Act
        gate.DisableForEntity(["k"], target);

        // Assert
        gate.IsEnabled(target, "k", LifecyclePhase.BeforeSave).Should().BeFalse();
        gate.IsEnabled(other, "k", LifecyclePhase.BeforeSave).Should().BeTrue();
    }

    /// <summary>
    /// <see cref="ILifecycleActionGate.EnableForEntity"/> возвращает ключ
    /// только для указанной сущности; остальные сохраняют состояние.
    /// </summary>
    [Fact]
    public void EnableForEntity_OnlyRestoresThatEntity()
    {
        // Arrange
        var gate = new LifecycleActionGate();
        var target = new TestEntity();
        var other = new TestEntity();
        gate.DisableForEntity(["k"], target);
        gate.DisableForEntity(["k"], other);

        // Act
        gate.EnableForEntity(["k"], target);

        // Assert
        gate.IsEnabled(target, "k", LifecyclePhase.BeforeSave).Should().BeTrue();
        gate.IsEnabled(other, "k", LifecyclePhase.BeforeSave).Should().BeFalse();
    }

    /// <summary>
    /// <see cref="ILifecycleActionGate.DisableForEntity"/> выбрасывает
    /// <see cref="ArgumentNullException"/> при <c>null</c> сущности.
    /// </summary>
    [Fact]
    public void DisableForEntity_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var gate = new LifecycleActionGate();

        // Act
        Action act = () => gate.DisableForEntity(["k"], null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// <see cref="ILifecycleActionGate.DisablePhaseForEntity"/> отключает
    /// фазу только для конкретной сущности.
    /// </summary>
    [Fact]
    public void DisablePhaseForEntity_OnlyAffectsThatEntity()
    {
        // Arrange
        var gate = new LifecycleActionGate();
        var target = new TestEntity();
        var other = new TestEntity();
        gate.DisablePhaseForEntity(LifecyclePhase.BeforeSave, target);

        // Act
        var targetBefore = gate.IsEnabled(target, "any", LifecyclePhase.BeforeSave);
        var otherBefore = gate.IsEnabled(other, "any", LifecyclePhase.BeforeSave);

        // Assert
        targetBefore.Should().BeFalse();
        otherBefore.Should().BeTrue();
    }

    /// <summary>
    /// <see cref="ILifecycleActionGate.EnablePhaseForEntity"/> восстанавливает
    /// фазу только для указанной сущности.
    /// </summary>
    [Fact]
    public void EnablePhaseForEntity_OnlyRestoresThatEntity()
    {
        // Arrange
        var gate = new LifecycleActionGate();
        var target = new TestEntity();
        var other = new TestEntity();
        gate.DisablePhaseForEntity(LifecyclePhase.BeforeSave, target);
        gate.DisablePhaseForEntity(LifecyclePhase.BeforeSave, other);

        // Act
        gate.EnablePhaseForEntity(LifecyclePhase.BeforeSave, target);

        // Assert
        gate.IsEnabled(target, "any", LifecyclePhase.BeforeSave).Should().BeTrue();
        gate.IsEnabled(other, "any", LifecyclePhase.BeforeSave).Should().BeFalse();
    }

    /// <summary>
    /// <see cref="ILifecycleActionGate.DisablePhaseForEntity"/> выбрасывает
    /// <see cref="ArgumentNullException"/> при <c>null</c> сущности.
    /// </summary>
    [Fact]
    public void DisablePhaseForEntity_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var gate = new LifecycleActionGate();

        // Act
        Action act = () => gate.DisablePhaseForEntity(LifecyclePhase.BeforeSave, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Per-entity key disable перекрывает глобальный key disable —
    /// после <c>Enable("a")</c> глобально для target остаётся false,
    /// для остальных становится true.
    /// </summary>
    [Fact]
    public void PerEntityKey_OverridesGlobalKey_Enable()
    {
        // Arrange
        var gate = new LifecycleActionGate();
        var target = new TestEntity();
        var other = new TestEntity();
        gate.Disable(["a"]);
        gate.DisableForEntity(["a"], target);

        // Act
        gate.Enable(["a"]);

        // Assert
        gate.IsEnabled(target, "a", LifecyclePhase.BeforeSave)
            .Should().BeFalse("per-entity disable перекрывает глобальный enable");
        gate.IsEnabled(other, "a", LifecyclePhase.BeforeSave)
            .Should().BeTrue("глобальный enable распространяется на other");
    }

    /// <summary>
    /// <see cref="ILifecycleActionGate.EnableForEntity"/> действует только
    /// на per-entity настройки, установленные через
    /// <see cref="ILifecycleActionGate.DisableForEntity"/>. Если per-entity
    /// disable не вызывался, <c>EnableForEntity</c> — no-op: глобальный
    /// disable продолжает действовать.
    /// </summary>
    [Fact]
    public void EnableForEntity_WithoutPriorDisableForEntity_IsNoOp()
    {
        // Arrange
        var gate = new LifecycleActionGate();
        var target = new TestEntity();
        gate.Disable(["a"]);

        // Act
        gate.EnableForEntity(["a"], target);

        // Assert
        gate.IsEnabled(target, "a", LifecyclePhase.BeforeSave)
            .Should().BeFalse("EnableForEntity не устанавливает per-entity override — он снимает только существующий");
    }

    /// <summary>
    /// Глобальный <see cref="ILifecycleActionGate.Disable()"/> имеет
    /// наименьший приоритет — per-entity settings не могут его «пробить».
    /// </summary>
    [Fact]
    public void GlobalDisabled_OverridesNothingAbove()
    {
        // Arrange
        var gate = new LifecycleActionGate();
        var target = new TestEntity();
        var other = new TestEntity();
        gate.Disable();
        gate.EnableForEntity(["a"], target);
        gate.EnablePhaseForEntity(LifecyclePhase.BeforeSave, other);

        // Act
        var targetResult = gate.IsEnabled(target, "a", LifecyclePhase.BeforeSave);
        var otherResult = gate.IsEnabled(other, "a", LifecyclePhase.BeforeSave);

        // Assert
        targetResult.Should().BeFalse("глобальный disable непробиваем");
        otherResult.Should().BeFalse("глобальный disable непробиваем");
    }

    /// <summary>
    /// <see cref="ILifecycleActionGate.Forget"/> сбрасывает все
    /// per-entity настройки для указанной сущности.
    /// </summary>
    [Fact]
    public void Forget_RemovesAllEntityScopedState()
    {
        // Arrange
        var gate = new LifecycleActionGate();
        var entity = new TestEntity();
        gate.DisableForEntity(["a"], entity);
        gate.DisablePhaseForEntity(LifecyclePhase.BeforeSave, entity);

        // Act
        gate.Forget([entity]);

        // Assert
        gate.IsEnabled(entity, "a", LifecyclePhase.BeforeSave)
            .Should().BeTrue("per-entity настройки сброшены");
    }

    /// <summary>
    /// <see cref="ILifecycleActionGate.Forget"/> действует только
    /// на перечисленные сущности.
    /// </summary>
    [Fact]
    public void Forget_OnlyAffectsListedEntities()
    {
        // Arrange
        var gate = new LifecycleActionGate();
        var e1 = new TestEntity();
        var e2 = new TestEntity();
        gate.DisableForEntity(["a"], e1);
        gate.DisableForEntity(["a"], e2);

        // Act
        gate.Forget([e1]);

        // Assert
        gate.IsEnabled(e1, "a", LifecyclePhase.BeforeSave).Should().BeTrue();
        gate.IsEnabled(e2, "a", LifecyclePhase.BeforeSave).Should().BeFalse();
    }

    /// <summary>
    /// <see cref="ILifecycleActionGate.Forget"/> для незарегистрированной
    /// сущности — безопасная no-op.
    /// </summary>
    [Fact]
    public void Forget_UnknownEntity_DoesNotThrow()
    {
        // Arrange
        var gate = new LifecycleActionGate();
        var stranger = new TestEntity();

        // Act
        Action act = () => gate.Forget([stranger]);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// <see cref="ILifecycleActionGate.Forget"/> выбрасывает
    /// <see cref="ArgumentNullException"/> при <c>null</c> коллекции.
    /// </summary>
    [Fact]
    public void Forget_NullCollection_ThrowsArgumentNullException()
    {
        // Arrange
        var gate = new LifecycleActionGate();

        // Act
        Action act = () => gate.Forget(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Тест на ключевую инвариантность <see cref="EntityKey"/>: две разные
    /// instance одного типа с одинаковым <c>Id</c> — это один и тот же
    /// ключ (value-семантика). <see cref="ILifecycleActionGate.Forget"/>
    /// для одной из них должен сбросить per-entity настройки и для другой.
    /// </summary>
    [Fact]
    public void Forget_DifferentInstancesSameEntityKey_ClearsStateForBoth()
    {
        // Arrange
        var gate = new LifecycleActionGate();
        var id = Guid.NewGuid();
        var first = new TestEntityWithId(id);
        var second = new TestEntityWithId(id);

        gate.DisableForEntity(["a"], first);
        gate.IsEnabled(first, "a", LifecyclePhase.BeforeSave).Should().BeFalse();
        gate.IsEnabled(second, "a", LifecyclePhase.BeforeSave)
            .Should().BeFalse("EntityKey value-семантика: разные instance с одинаковым (Type,Id) — один ключ");

        // Act
        gate.Forget([second]);

        // Assert
        gate.IsEnabled(first, "a", LifecyclePhase.BeforeSave)
            .Should().BeTrue("Forget(second) сбросил per-entity настройки, т.к. EntityKey совпадает");
    }

    /// <summary>
    /// Тестовая сущность с управляемым <see cref="IEntity.Id"/>
    /// для проверки value-семантики <see cref="EntityKey"/>.
    /// </summary>
    private sealed class TestEntityWithId(Guid id)
        : IEntity
    {
        object IEntity.Id => id;
    }

    /// <summary>
    /// <see cref="ILifecycleActionGate.Reset"/> сбрасывает все настройки
    /// в состояние по умолчанию (всё разрешено).
    /// </summary>
    [Fact]
    public void Reset_ClearsAllState()
    {
        // Arrange
        var gate = new LifecycleActionGate();
        var entity = new TestEntity();
        gate.Disable();
        gate.Disable(["a"]);
        gate.DisablePhase(LifecyclePhase.BeforeSave);
        gate.DisableForEntity(["a"], entity);
        gate.DisablePhaseForEntity(LifecyclePhase.BeforeSave, entity);

        // Act
        gate.Reset();

        // Assert
        gate.IsEnabled(entity, "a", LifecyclePhase.BeforeSave).Should().BeTrue();
        gate.IsEnabled(entity, "a", LifecyclePhase.AfterSave).Should().BeTrue();
    }

    /// <summary>
    /// <see cref="ILifecycleActionGate.Reset"/> на свежем gate — безопасная no-op.
    /// </summary>
    [Fact]
    public void Reset_OnFreshGate_DoesNotThrow()
    {
        // Arrange
        var gate = new LifecycleActionGate();

        // Act
        Action act = () => gate.Reset();

        // Assert
        act.Should().NotThrow();
    }
}
