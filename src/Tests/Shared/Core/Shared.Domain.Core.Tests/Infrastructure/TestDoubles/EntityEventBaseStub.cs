using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Event;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

public class EntityEventBaseStub : EntityEventBase
{
    public bool ProcessActionAsyncCalled { get; set; }
    public bool DisableEntitiesEventsCalled { get; set; }
    public DomainEventType? LastEventType { get; private set; }
    public ICollection<IWithDomainEvents>? LastEntities { get; private set; }

    public EntityEventBaseStub(Enum key)
        : base(key)
    {
    }

    protected override Task ProcessActionAsync(
        IServiceProvider serviceProvider,
        ICollection<IWithDomainEvents> entities,
        CancellationToken cancellationToken)
    {
        ProcessActionAsyncCalled = true;
        return Task.CompletedTask;
    }

    protected override void DisableEntitiesEvents(
        DomainEventType eventType,
        ICollection<IWithDomainEvents> entities)
    {
        DisableEntitiesEventsCalled = true;
        LastEventType = eventType;
        LastEntities = entities;
        base.DisableEntitiesEvents(eventType, entities);
    }
}
