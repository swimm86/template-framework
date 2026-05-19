using Shared.Domain.Core.Event;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

public class CustomDomainEventStub : CustomDomainEvent
{
    public CustomDomainEventStub(Enum key, Func<IServiceProvider, ICollection<IWithDomainEvents>, CancellationToken, Task> action)
        : base(key, action)
    {
    }

    public Task CallProcessActionAsync(
        IServiceProvider serviceProvider,
        ICollection<IWithDomainEvents> entities,
        CancellationToken cancellationToken)
        => ProcessActionAsync(serviceProvider, entities, cancellationToken);
}
