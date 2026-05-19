using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Event.Settings;
using Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Domain.Core.Tests.Event.Settings;

public sealed class DomainEventSettingsTests
{
    [Fact]
    public void Switch_TripleKey_EnablesDisablesEvent()
    {
        var settings = new DomainEventSettings(true);

        settings.Switch(typeof(string), DomainEventType.BeforeSave, TestEnum.BeforeCreate, false);

        settings.AnyElementEnabled(typeof(string), DomainEventType.BeforeSave, TestEnum.BeforeCreate).Should().BeFalse();
    }

    [Fact]
    public void AnyElementEnabled_ByTripleKey_ReturnsCorrectValue()
    {
        var settings = new DomainEventSettings(true);

        var result = settings.AnyElementEnabled(typeof(int), DomainEventType.AfterSave, TestEnum.AfterCreate);

        result.Should().BeTrue();
    }

    [Fact]
    public void Switch_RootTrue_EnablesAll()
    {
        var settings = new DomainEventSettings(true);
        settings.Switch(typeof(string), DomainEventType.BeforeSave, TestEnum.AfterUpdate, false);

        settings.Switch(true);

        settings.AnyElementEnabled(typeof(string), DomainEventType.BeforeSave, TestEnum.AfterUpdate).Should().BeTrue();
    }
}
