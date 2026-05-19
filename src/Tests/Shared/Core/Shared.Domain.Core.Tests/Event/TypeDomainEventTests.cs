using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Event.Interfaces;
using Shared.Domain.Core.Interfaces;
using Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Domain.Core.Tests.Event;

public sealed class TypeDomainEventTests
{
    [Fact]
    public async Task ProcessActionAsync_CallsDelegateWithServiceProviderAndEntities()
    {
        IServiceProvider? receivedServiceProvider = null;
        ICollection<IWithDomainEvents>? receivedEntities = null;
        var stub = new TypeDomainEventStub(
            TestEnum.BeforeCreate,
            (sp, entities, _) =>
            {
                receivedServiceProvider = sp;
                receivedEntities = entities;
                return Task.CompletedTask;
            });

        var serviceProvider = new TestServiceProvider();
        var entities = new List<IWithDomainEvents>();
        await stub.CallProcessActionAsync(serviceProvider, entities, CancellationToken.None);

        receivedServiceProvider.Should().BeSameAs(serviceProvider);
        receivedEntities.Should().BeSameAs(entities);
    }

    [Fact]
    public async Task Entities_CollectionIsPassedToDelegate()
    {
        ICollection<IWithDomainEvents>? receivedEntities = null;
        var stub = new TypeDomainEventStub(
            TestEnum.BeforeCreate,
            (_, entities, _) =>
            {
                receivedEntities = entities;
                return Task.CompletedTask;
            });

        var entities = new List<IWithDomainEvents> { new TestEntityWithDomainEvents() };
        await stub.CallProcessActionAsync(null!, entities, CancellationToken.None);

        receivedEntities.Should().NotBeNull();
        receivedEntities.Should().HaveCount(1);
    }

    [Fact]
    public async Task DisableEntitiesEvents_DisablesEntities()
    {
        var stub = new TypeDomainEventStub(
            TestEnum.BeforeCreate,
            (_, _, _) => Task.CompletedTask);
        stub.Enable();
        var entity = new TestEntityWithDomainEvents();
        entity.AddEvent(DomainEventType.BeforeSave, TestEnum.BeforeCreate, stub);

        await stub.ProcessAsync(DomainEventType.BeforeSave, null!, [entity], CancellationToken.None);

        var disabledEvent = entity.GetEvent(DomainEventType.BeforeSave, TestEnum.BeforeCreate);
        disabledEvent.Should().NotBeNull();
    }

    private sealed class TestEntityWithDomainEvents : IWithDomainEvents
    {
        public string[] RequiredToSaveNavigationPropertiesNames => [];

        private readonly Dictionary<(DomainEventType, Enum), IDomainEvent> _events = new();

        public void AddEvent(DomainEventType eventType, Enum key, IDomainEvent domainEvent)
            => _events[(eventType, key)] = domainEvent;

        public IDomainEvent? GetEvent(DomainEventType eventType, Enum key)
            => _events.TryGetValue((eventType, key), out var e) ? e : null;

        public bool TryGetEvent(DomainEventType domainEventType, Enum key, out IDomainEvent domainEvent)
        {
            if (_events.TryGetValue((domainEventType, key), out var e))
            {
                domainEvent = e;
                return true;
            }

            domainEvent = null!;
            return false;
        }

        public void ResetEvents() { }

        public ICollection<Enum> GetAllKeys(DomainEventType domainEventType)
            => _events.Where(x => x.Key.Item1 == domainEventType).Select(x => x.Key.Item2).ToList();
    }
}
