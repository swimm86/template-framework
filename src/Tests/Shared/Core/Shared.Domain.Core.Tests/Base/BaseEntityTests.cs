using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;
using Shared.Domain.Core.LifecycleAction;
using Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Domain.Core.Tests.Base;

/// <summary>
/// Тесты для базовой сущности, проверяющие инициализацию и жизненный цикл доменных событий.
/// </summary>
public class BaseEntityTests
{
    /// <summary>
    /// Проверяет, что при создании сущности в конструкторе инициализируются действия BeforeSave и AfterSave.
    /// </summary>
    [Fact]
    public void Actions_AreInitializedInConstructor()
    {
        // Arrange
        var entity = new TestBaseEntity();

        // Act
        var beforeKeys = entity.GetAllKeys(LifecycleHookType.BeforeSave);
        var afterKeys = entity.GetAllKeys(LifecycleHookType.AfterSave);

        // Assert
        beforeKeys.Should().ContainSingle().Which.Should().Be(TestEventKey.Before);
        afterKeys.Should().ContainSingle().Which.Should().Be(TestEventKey.After);
    }

    /// <summary>
    /// Проверяет, что TryGetAction возвращает true и действие при существующем ключе.
    /// </summary>
    [Fact]
    public void TryGetAction_ExistingKey_ReturnsTrueAndAction()
    {
        // Arrange
        var entity = new TestBaseEntity();

        // Act
        var result = entity.TryGetAction(
            LifecycleHookType.BeforeSave,
            TestEventKey.Before,
            out var lifecycleAction);

        // Assert
        result.Should().BeTrue();
        lifecycleAction.Should().NotBeNull();
        lifecycleAction.Key.Should().Be(TestEventKey.Before);
    }

    /// <summary>
    /// Проверяет, что TryGetAction возвращает false при несуществующем ключе.
    /// </summary>
    [Fact]
    public void TryGetAction_NonExistingKey_ReturnsFalse()
    {
        // Arrange
        var entity = new TestBaseEntity();

        // Act
        var result = entity.TryGetAction(
            LifecycleHookType.BeforeSave,
            TestEventKey.After,
            out var lifecycleAction);

        // Assert
        result.Should().BeFalse();
        lifecycleAction.Should().BeNull();
    }

    /// <summary>
    /// Проверяет, что после вызова DisableLifecycleActions обработка действий не выполняется.
    /// </summary>
    [Fact]
    public async Task DisableLifecycleActions_PreventsActionDispatch()
    {
        // Arrange
        var entity = new TestBaseEntity();
        entity.DisableLifecycleActions();

        var serviceProvider = new StubServiceProvider();
        var entities = new List<IWithLifecycleActions> { entity };

        // Act
        await ((IWithLifecycleActions)entity).ProcessLifecycleActionAsync(
            LifecycleHookType.BeforeSave,
            TestEventKey.Before,
            serviceProvider,
            entities,
            TestContext.Current.CancellationToken);

        // Assert
        entity.BeforeActionCalled.Should().BeFalse();
    }

    /// <summary>
    /// Проверяет, что после вызова EnableLifecycleActions обработка действий возобновляется.
    /// </summary>
    [Fact]
    public async Task EnableLifecycleActions_AllowsDispatchAgain()
    {
        // Arrange
        var entity = new TestBaseEntity();
        entity.DisableLifecycleActions();
        entity.EnableLifecycleActions();

        var serviceProvider = new StubServiceProvider();
        var entities = new List<IWithLifecycleActions> { entity };

        // Act
        await ((IWithLifecycleActions)entity).ProcessLifecycleActionAsync(
            LifecycleHookType.BeforeSave,
            TestEventKey.Before,
            serviceProvider,
            entities,
            TestContext.Current.CancellationToken);

        // Assert
        entity.BeforeActionCalled.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что вызов ResetActions повторно включает обработку действий.
    /// </summary>
    [Fact]
    public async Task ResetActions_EnablesActions()
    {
        // Arrange
        var entity = new TestBaseEntity();
        entity.DisableLifecycleActions();
        entity.ResetActions();

        var serviceProvider = new StubServiceProvider();
        var entities = new List<IWithLifecycleActions> { entity };

        // Act
        await ((IWithLifecycleActions)entity).ProcessLifecycleActionAsync(
            LifecycleHookType.BeforeSave,
            TestEventKey.Before,
            serviceProvider,
            entities,
            TestContext.Current.CancellationToken);

        // Assert
        entity.BeforeActionCalled.Should().BeTrue();
    }

    /// <summary>
    /// Подтверждает, что дублирующиеся ключи действий в BeforeSaveActions
    /// приводят к ArgumentException при инициализации сущности (ToDictionary).
    /// </summary>
    [Fact]
    public void Constructor_WithDuplicateActionKeys_ThrowsArgumentException()
    {
        // Act
        var act = () => new TestBaseEntityWithDuplicateActionKeys();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*same key*");
    }

    private sealed class StubServiceProvider
        : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
