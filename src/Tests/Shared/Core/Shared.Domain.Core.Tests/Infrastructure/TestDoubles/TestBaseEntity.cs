using System.Reflection;
using Shared.Domain.Core.Base;
using Shared.Domain.Core.Enums;
using Shared.Domain.Core.LifecycleAction;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

public class TestBaseEntity : BaseEntity<Guid>
{
    public string Name { get; set; } = string.Empty;

    public bool BeforeActionCalled { get; private set; }

    public bool AfterActionCalled { get; private set; }

    public static readonly TestEventKey BeforeKey = TestEventKey.Before;

    public static readonly TestEventKey AfterKey = TestEventKey.After;

    private readonly List<IEntityLifecycleAction> _beforeActions = [];

    private readonly List<IEntityLifecycleAction> _afterActions = [];

    public TestBaseEntity()
    {
        _beforeActions.Add(new EntityLifecycleAction(
            TestEventKey.Before,
            (_, _) => { BeforeActionCalled = true; return Task.CompletedTask; }));
        _afterActions.Add(new EntityLifecycleAction(
            TestEventKey.After,
            (_, _) => { AfterActionCalled = true; return Task.CompletedTask; }));

        RecreateActions();
    }

    protected override IEntityLifecycleAction[] BeforeSaveActions => _beforeActions.ToArray();

    protected override IEntityLifecycleAction[] AfterSaveActions => _afterActions.ToArray();

    private void RecreateActions()
    {
        var method = typeof(BaseEntity<Guid>).GetMethod(
            "CreateActions",
            BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(this, null);
    }
}
