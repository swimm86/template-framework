using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Event.Settings;
using Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Domain.Core.Tests.Event.Settings;

public sealed class TypeEventSettingsTests
{
    [Fact]
    public void Switch_CreatesEventTypeEventSettings_IfNotExists()
    {
        var settings = new TypeEventSettings(true);

        settings.Switch(DomainEventType.BeforeSave, TestEnum.BeforeCreate, false);

        settings.AnyElementEnabled(DomainEventType.BeforeSave, TestEnum.BeforeCreate).Should().BeFalse();
    }

    [Fact]
    public void Switch_UpdatesExistingEventTypeEventSettings()
    {
        var settings = new TypeEventSettings(true);
        settings.Switch(DomainEventType.BeforeSave, TestEnum.BeforeCreate, false);

        settings.Switch(DomainEventType.BeforeSave, TestEnum.BeforeCreate, true);

        settings.AnyElementEnabled(DomainEventType.BeforeSave, TestEnum.BeforeCreate).Should().BeTrue();
    }

    [Fact]
    public void AnyElementEnabled_DelegatesToEventTypeSettings()
    {
        var settings = new TypeEventSettings(true);

        settings.AnyElementEnabled(DomainEventType.BeforeSave).Should().BeTrue();
    }

    [Fact]
    public void SwitchOnRoot_EnablesAll()
    {
        var settings = new TypeEventSettings(true);
        settings.Switch(DomainEventType.BeforeSave, TestEnum.AfterUpdate, false);

        settings.Switch(true);

        settings.AnyElementEnabled(DomainEventType.BeforeSave, TestEnum.AfterUpdate).Should().BeTrue();
    }
}
