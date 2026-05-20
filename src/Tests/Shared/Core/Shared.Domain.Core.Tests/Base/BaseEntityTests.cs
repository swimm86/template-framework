using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;
using Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Domain.Core.Tests.Base;

/// <summary>
/// Тесты для базовой сущности, проверяющие инициализацию и жизненный цикл доменных событий.
/// </summary>
public class BaseEntityTests
{
    /// <summary>
    /// Проверяет, что при создании сущности в конструкторе инициализируются события BeforeSave и AfterSave.
    /// </summary>
    [Fact]
    public void Events_AreInitializedInConstructor()
    {
        // Arrange
        var entity = new TestBaseEntity();

        // Act
        var beforeKeys = entity.GetAllKeys(DomainEventType.BeforeSave);
        var afterKeys = entity.GetAllKeys(DomainEventType.AfterSave);

        // Assert
        beforeKeys.Should().ContainSingle().Which.Should().Be(TestEventKey.Before);
        afterKeys.Should().ContainSingle().Which.Should().Be(TestEventKey.After);
    }

    /// <summary>
    /// Проверяет, что TryGetEvent возвращает true и событие при существующем ключе.
    /// </summary>
    [Fact]
    public void TryGetEvent_ExistingKey_ReturnsTrueAndEvent()
    {
        // Arrange
        var entity = new TestBaseEntity();

        // Act
        var result = entity.TryGetEvent(DomainEventType.BeforeSave, TestEventKey.Before, out var domainEvent);

        // Assert
        result.Should().BeTrue();
        domainEvent.Should().NotBeNull();
        domainEvent.Key.Should().Be(TestEventKey.Before);
    }

    /// <summary>
    /// Проверяет, что TryGetEvent возвращает false при несуществующем ключе.
    /// </summary>
    [Fact]
    public void TryGetEvent_NonExistingKey_ReturnsFalse()
    {
        // Arrange
        var entity = new TestBaseEntity();

        // Act
        var result = entity.TryGetEvent(DomainEventType.BeforeSave, TestEventKey.After, out var domainEvent);

        // Assert
        result.Should().BeFalse();
        domainEvent.Should().BeNull();
    }

    /// <summary>
    /// Проверяет, что после вызова DisableDomainEvents обработка событий не выполняется.
    /// </summary>
    [Fact]
    public async Task DisableDomainEvents_PreventsEventDispatch()
    {
        // Arrange
        var entity = new TestBaseEntity();
        entity.DisableDomainEvents();

        var serviceProvider = new StubServiceProvider();
        var entities = new List<IWithDomainEvents> { entity };

        // Act
        await ((IWithDomainEvents)entity).ProcessDomainEventAsync(DomainEventType.BeforeSave, TestEventKey.Before, serviceProvider, entities);

        // Assert
        entity.BeforeActionCalled.Should().BeFalse();
    }

    /// <summary>
    /// Проверяет, что после вызова EnableDomainEvents обработка событий возобновляется.
    /// </summary>
    [Fact]
    public async Task EnableDomainEvents_AllowsDispatchAgain()
    {
        // Arrange
        var entity = new TestBaseEntity();
        entity.DisableDomainEvents();
        entity.EnableDomainEvents();

        var serviceProvider = new StubServiceProvider();
        var entities = new List<IWithDomainEvents> { entity };

        // Act
        await ((IWithDomainEvents)entity).ProcessDomainEventAsync(DomainEventType.BeforeSave, TestEventKey.Before, serviceProvider, entities);

        // Assert
        entity.BeforeActionCalled.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что вызов ResetEvents повторно включает обработку событий.
    /// </summary>
    [Fact]
    public async Task ResetEvents_EnablesEvents()
    {
        // Arrange
        var entity = new TestBaseEntity();
        entity.DisableDomainEvents();
        entity.ResetEvents();

        var serviceProvider = new StubServiceProvider();
        var entities = new List<IWithDomainEvents> { entity };

        // Act
        await ((IWithDomainEvents)entity).ProcessDomainEventAsync(DomainEventType.BeforeSave, TestEventKey.Before, serviceProvider, entities);

        // Assert
        entity.BeforeActionCalled.Should().BeTrue();
    }

    private sealed class StubServiceProvider
        : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
