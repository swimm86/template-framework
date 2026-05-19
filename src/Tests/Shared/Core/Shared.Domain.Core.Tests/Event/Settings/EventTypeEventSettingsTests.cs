using Shared.Domain.Core.Event.Settings;
using Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Domain.Core.Tests.Event.Settings;

public sealed class EventTypeEventSettingsTests
{
    [Fact]
    public void Switch_WithTrue_EnablesElement()
    {
        var settings = new EventTypeEventSettings(true);

        settings.Switch(TestEnum.BeforeCreate, true);

        settings.AnyElementEnabled(TestEnum.BeforeCreate).Should().BeTrue();
    }

    [Fact]
    public void Switch_WithFalse_DisablesElement()
    {
        var settings = new EventTypeEventSettings(true);

        settings.Switch(TestEnum.BeforeCreate, false);

        settings.AnyElementEnabled(TestEnum.BeforeCreate).Should().BeFalse();
    }

    [Fact]
    public void AnyElementEnabled_WhenNoneEnabled_ReturnsFalse()
    {
        var settings = new EventTypeEventSettings(false);

        var result = settings.AnyElementEnabled(TestEnum.BeforeCreate);

        result.Should().BeFalse();
    }

    [Fact]
    public void AnyElementEnabled_WhenAnyEnabled_ReturnsTrue()
    {
        var settings = new EventTypeEventSettings(true);

        settings.Switch(TestEnum.AfterCreate, false);
        var result = settings.AnyElementEnabled(TestEnum.BeforeCreate);

        result.Should().BeTrue();
    }

    [Fact]
    public void AnyEnabled_DefaultIsTrue()
    {
        var settings = new EventTypeEventSettings();

        settings.AnyEnabled.Should().BeTrue();
    }
}
