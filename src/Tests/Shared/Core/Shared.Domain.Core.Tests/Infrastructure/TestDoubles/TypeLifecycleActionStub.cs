using Shared.Domain.Core.Enums;
using Shared.Domain.Core.LifecycleAction;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

public class TypeLifecycleActionStub(
    Enum key,
    Func<IServiceProvider, ICollection<IWithLifecycleActions>, CancellationToken, Task> action)
    : TypeLifecycleAction(key, action)
{
    public Task CallExecuteActionAsync(
        IServiceProvider serviceProvider,
        ICollection<IWithLifecycleActions> entities,
        CancellationToken cancellationToken)
        => ExecuteActionAsync(serviceProvider, entities, cancellationToken);

    public void CallDisableEntitiesActions(
        LifecycleHookType hookType,
        ICollection<IWithLifecycleActions> entities)
        => DisableEntitiesActions(hookType, entities);
}
