using Shared.Domain.Core.Enums;
using Shared.Domain.Core.LifecycleAction;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

public class EntityLifecycleActionBaseStub : EntityLifecycleActionBase
{
    public bool ProcessActionAsyncCalled { get; set; }
    public bool DisableEntitiesActionsCalled { get; set; }
    public LifecycleHookType? LastHookType { get; private set; }
    public ICollection<IWithLifecycleActions>? LastEntities { get; private set; }

    public EntityLifecycleActionBaseStub(Enum key)
        : base(key)
    {
    }

    protected override Task ExecuteActionAsync(
        IServiceProvider serviceProvider,
        ICollection<IWithLifecycleActions> entities,
        CancellationToken cancellationToken)
    {
        ProcessActionAsyncCalled = true;
        return Task.CompletedTask;
    }

    protected override void DisableEntitiesActions(
        LifecycleHookType hookType,
        ICollection<IWithLifecycleActions> entities)
    {
        DisableEntitiesActionsCalled = true;
        LastHookType = hookType;
        LastEntities = entities;
        base.DisableEntitiesActions(hookType, entities);
    }
}
