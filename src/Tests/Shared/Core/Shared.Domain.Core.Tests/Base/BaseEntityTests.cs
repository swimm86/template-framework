using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;
using Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Domain.Core.Tests.Base;

public class BaseEntityTests
{
    [Fact]
    public void Events_AreInitializedInConstructor()
    {
        var entity = new TestBaseEntity();

        var beforeKeys = entity.GetAllKeys(DomainEventType.BeforeSave);
        var afterKeys = entity.GetAllKeys(DomainEventType.AfterSave);

        beforeKeys.Should().ContainSingle().Which.Should().Be(TestEventKey.Before);
        afterKeys.Should().ContainSingle().Which.Should().Be(TestEventKey.After);
    }

    [Fact]
    public void TryGetEvent_ExistingKey_ReturnsTrueAndEvent()
    {
        var entity = new TestBaseEntity();

        var result = entity.TryGetEvent(DomainEventType.BeforeSave, TestEventKey.Before, out var domainEvent);

        result.Should().BeTrue();
        domainEvent.Should().NotBeNull();
        domainEvent.Key.Should().Be(TestEventKey.Before);
    }

    [Fact]
    public void TryGetEvent_NonExistingKey_ReturnsFalse()
    {
        var entity = new TestBaseEntity();

        var result = entity.TryGetEvent(DomainEventType.BeforeSave, TestEventKey.After, out var domainEvent);

        result.Should().BeFalse();
        domainEvent.Should().BeNull();
    }

    [Fact]
    public async Task DisableDomainEvents_PreventsEventDispatch()
    {
        var entity = new TestBaseEntity();
        entity.DisableDomainEvents();

        var serviceProvider = new StubServiceProvider();
        var entities = new List<IWithDomainEvents> { entity };

        await ((IWithDomainEvents)entity).ProcessDomainEventAsync(DomainEventType.BeforeSave, TestEventKey.Before, serviceProvider, entities);

        entity.BeforeActionCalled.Should().BeFalse();
    }

    [Fact]
    public async Task EnableDomainEvents_AllowsDispatchAgain()
    {
        var entity = new TestBaseEntity();
        entity.DisableDomainEvents();
        entity.EnableDomainEvents();

        var serviceProvider = new StubServiceProvider();
        var entities = new List<IWithDomainEvents> { entity };

        await ((IWithDomainEvents)entity).ProcessDomainEventAsync(DomainEventType.BeforeSave, TestEventKey.Before, serviceProvider, entities);

        entity.BeforeActionCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ResetEvents_EnablesEvents()
    {
        var entity = new TestBaseEntity();
        entity.DisableDomainEvents();
        entity.ResetEvents();

        var serviceProvider = new StubServiceProvider();
        var entities = new List<IWithDomainEvents> { entity };

        await ((IWithDomainEvents)entity).ProcessDomainEventAsync(DomainEventType.BeforeSave, TestEventKey.Before, serviceProvider, entities);

        entity.BeforeActionCalled.Should().BeTrue();
    }

    private sealed class StubServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
