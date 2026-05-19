using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Event;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

public class TypeDomainEventStub : TypeDomainEvent
{
    public TypeDomainEventStub(Enum key, Func<IServiceProvider, ICollection<IWithDomainEvents>, CancellationToken, Task> action)
        : base(key, action)
    {
    }

    public Task CallProcessActionAsync(
        IServiceProvider serviceProvider,
        ICollection<IWithDomainEvents> entities,
        CancellationToken cancellationToken)
        => ProcessActionAsync(serviceProvider, entities, cancellationToken);

    public void CallDisableEntitiesEvents(
        DomainEventType eventType,
        ICollection<IWithDomainEvents> entities)
        => DisableEntitiesEvents(eventType, entities);
}
