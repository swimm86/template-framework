using Shared.Domain.Core.Enums;
using Shared.Domain.Core.LifecycleAction;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

public class EntityLifecycleActionStub(
    Enum key,
    Func<IServiceProvider,
        CancellationToken, Task> action)
    : EntityLifecycleAction(key, action)
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
