using System.Reflection;
using Shared.Domain.Core.Base;
using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Event;
using Shared.Domain.Core.Event.Interfaces;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

public class TestBaseEntity : BaseEntity<Guid>
{
    public string Name { get; set; } = string.Empty;

    public bool BeforeActionCalled { get; private set; }

    public bool AfterActionCalled { get; private set; }

    public static readonly TestEventKey BeforeKey = TestEventKey.Before;

    public static readonly TestEventKey AfterKey = TestEventKey.After;

    private readonly List<IDomainEvent> _beforeEvents = [];

    private readonly List<IDomainEvent> _afterEvents = [];

    public TestBaseEntity()
    {
        _beforeEvents.Add(new EntityDomainEvent(
            TestEventKey.Before,
            (_, _) => { BeforeActionCalled = true; return Task.CompletedTask; }));
        _afterEvents.Add(new EntityDomainEvent(
            TestEventKey.After,
            (_, _) => { AfterActionCalled = true; return Task.CompletedTask; }));

        RecreateEvents();
    }

    protected override IDomainEvent[] BeforeSaveEvents => _beforeEvents.ToArray();

    protected override IDomainEvent[] AfterSaveEvents => _afterEvents.ToArray();

    private void RecreateEvents()
    {
        var method = typeof(BaseEntity<Guid>).GetMethod(
            "CreateEvents",
            BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(this, null);
    }
}
