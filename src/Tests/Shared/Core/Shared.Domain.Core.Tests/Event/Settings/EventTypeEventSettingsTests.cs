using Shared.Domain.Core.Event.Settings;
using Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Domain.Core.Tests.Event.Settings;

/// <summary>
/// Тесты для <see cref="EventTypeEventSettings"/> — настройки включения/отключения доменных событий по ключу-перечислению.
/// </summary>
public sealed class EventTypeEventSettingsTests
{
    /// <summary>
    /// Проверяет, что вызов <see cref="EventTypeEventSettings.Switch(System.Enum,bool)"/> с <c>true</c> включает указанный элемент.
    /// </summary>
    [Fact]
    public void Switch_WithTrue_EnablesElement()
    {
        // Arrange
        var settings = new EventTypeEventSettings(Enabled: true);

        // Act
        settings.Switch(TestEnum.BeforeCreate, true);

        // Assert
        settings.AnyElementEnabled(TestEnum.BeforeCreate).Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что вызов <see cref="EventTypeEventSettings.Switch(System.Enum,bool)"/> с <c>false</c> отключает указанный элемент.
    /// </summary>
    [Fact]
    public void Switch_WithFalse_DisablesElement()
    {
        // Arrange
        var settings = new EventTypeEventSettings(Enabled: true);

        // Act
        settings.Switch(TestEnum.BeforeCreate, false);

        // Assert
        settings.AnyElementEnabled(TestEnum.BeforeCreate).Should().BeFalse();
    }

    /// <summary>
    /// Проверяет, что <see cref="EventTypeEventSettings.AnyElementEnabled(System.Enum)"/> возвращает <c>false</c>, когда ни один элемент не включён.
    /// </summary>
    [Fact]
    public void AnyElementEnabled_WhenNoneEnabled_ReturnsFalse()
    {
        // Arrange
        var settings = new EventTypeEventSettings(Enabled: false);

        // Act
        var result = settings.AnyElementEnabled(TestEnum.BeforeCreate);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Проверяет, что <see cref="EventTypeEventSettings.AnyElementEnabled(System.Enum)"/> возвращает <c>true</c>, когда хотя бы один элемент включён.
    /// </summary>
    [Fact]
    public void AnyElementEnabled_WhenAnyEnabled_ReturnsTrue()
    {
        // Arrange
        var settings = new EventTypeEventSettings(Enabled: true);

        // Act
        settings.Switch(TestEnum.AfterCreate, false);
        var result = settings.AnyElementEnabled(TestEnum.BeforeCreate);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что свойство <see cref="EventTypeEventSettings.AnyEnabled"/> по умолчанию равно <c>true</c>.
    /// </summary>
    [Fact]
    public void AnyEnabled_DefaultIsTrue()
    {
        // Arrange
        var settings = new EventTypeEventSettings();

        // Act
        var result = settings.AnyEnabled;

        // Assert
        result.Should().BeTrue();
    }
}
