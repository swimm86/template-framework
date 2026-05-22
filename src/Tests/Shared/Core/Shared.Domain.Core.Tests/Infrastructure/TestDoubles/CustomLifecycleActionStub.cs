using Shared.Domain.Core.LifecycleAction;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

public class CustomLifecycleActionStub(
    Enum key,
    Func<IServiceProvider, ICollection<IWithLifecycleActions>, CancellationToken, Task> action)
    : CustomLifecycleAction(key, action)
{
    public Task CallExecuteActionAsync(
        IServiceProvider serviceProvider,
        ICollection<IWithLifecycleActions> entities,
        CancellationToken cancellationToken)
        => ExecuteActionAsync(serviceProvider, entities, cancellationToken);
}
