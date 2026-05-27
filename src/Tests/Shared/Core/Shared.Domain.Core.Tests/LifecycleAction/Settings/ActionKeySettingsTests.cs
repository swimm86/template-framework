using Shared.Domain.Core.LifecycleAction.Settings;
using Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Domain.Core.Tests.LifecycleAction.Settings;

/// <summary>
/// Тесты для <see cref="ActionKeySettings"/> — настройки включения/отключения доменных событий по ключу-перечислению.
/// </summary>
public sealed class ActionKeySettingsTests
{
    /// <summary>
    /// Проверяет, что вызов <see cref="ActionKeySettings.Switch(System.Enum,bool)"/> с <c>true</c> включает указанный элемент.
    /// </summary>
    [Fact]
    public void Switch_WithTrue_EnablesElement()
    {
        // Arrange
        var settings = new ActionKeySettings(Enabled: true);

        // Act
        settings.Switch(TestEnum.BeforeCreate, true);

        // Assert
        settings.AnyElementEnabled(TestEnum.BeforeCreate).Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что вызов <see cref="ActionKeySettings.Switch(System.Enum,bool)"/> с <c>false</c> отключает указанный элемент.
    /// </summary>
    [Fact]
    public void Switch_WithFalse_DisablesElement()
    {
        // Arrange
        var settings = new ActionKeySettings(Enabled: true);

        // Act
        settings.Switch(TestEnum.BeforeCreate, false);

        // Assert
        settings.AnyElementEnabled(TestEnum.BeforeCreate).Should().BeFalse();
    }

    /// <summary>
    /// Проверяет, что <see cref="ActionKeySettings.AnyElementEnabled(System.Enum)"/> возвращает <c>false</c>, когда ни один элемент не включён.
    /// </summary>
    [Fact]
    public void AnyElementEnabled_WhenNoneEnabled_ReturnsFalse()
    {
        // Arrange
        var settings = new ActionKeySettings(Enabled: false);

        // Act
        var result = settings.AnyElementEnabled(TestEnum.BeforeCreate);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Проверяет, что <see cref="ActionKeySettings.AnyElementEnabled(System.Enum)"/> возвращает <c>true</c>, когда хотя бы один элемент включён.
    /// </summary>
    [Fact]
    public void AnyElementEnabled_WhenAnyEnabled_ReturnsTrue()
    {
        // Arrange
        var settings = new ActionKeySettings(Enabled: true);

        // Act
        settings.Switch(TestEnum.AfterCreate, false);
        var result = settings.AnyElementEnabled(TestEnum.BeforeCreate);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что свойство <see cref="ActionKeySettings.AnyEnabled"/> по умолчанию равно <c>true</c>.
    /// </summary>
    [Fact]
    public void AnyEnabled_DefaultIsTrue()
    {
        // Arrange
        var settings = new ActionKeySettings();

        // Act
        var result = settings.AnyEnabled;

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что HasFlag корректно работает с enum без [Flags] атрибута.
    /// При отключении одного ключа другие ключи остаются включёнными.
    /// </summary>
    [Fact]
    public void Switch_DisableOneKey_OtherKeysRemainEnabled()
    {
        // Arrange
        var settings = new ActionKeySettings(Enabled: true);

        // Act
        settings.Switch(TestEnum.BeforeCreate, false);

        // Assert
        settings.AnyElementEnabled(TestEnum.BeforeCreate).Should().BeFalse();
        settings.AnyElementEnabled(TestEnum.AfterCreate).Should().BeTrue();
        settings.AnyElementEnabled(TestEnum.BeforeUpdate).Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что при последовательном отключении нескольких ключей
    /// каждый ключ отключается независимо (HasFlag работает для простых enum).
    /// </summary>
    [Fact]
    public void Switch_DisableMultipleKeys_EachKeyDisabledIndependently()
    {
        // Arrange
        var settings = new ActionKeySettings(Enabled: true);

        // Act
        settings.Switch(TestEnum.BeforeCreate, false);
        settings.Switch(TestEnum.AfterCreate, false);

        // Assert
        settings.AnyElementEnabled(TestEnum.BeforeCreate).Should().BeFalse();
        settings.AnyElementEnabled(TestEnum.AfterCreate).Should().BeFalse();
        settings.AnyElementEnabled(TestEnum.BeforeUpdate).Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что повторное включение ранее отключённого ключа
    /// восстанавливает его состояние.
    /// </summary>
    [Fact]
    public void Switch_DisableThenEnable_KeyIsEnabledAgain()
    {
        // Arrange
        var settings = new ActionKeySettings(Enabled: true);
        settings.Switch(TestEnum.BeforeCreate, false);
        settings.AnyElementEnabled(TestEnum.BeforeCreate).Should().BeFalse();

        // Act
        settings.Switch(TestEnum.BeforeCreate, true);

        // Assert
        settings.AnyElementEnabled(TestEnum.BeforeCreate).Should().BeTrue();
    }
}
