using Shared.Domain.Core.Enums;
using Shared.Domain.Core.LifecycleAction.Settings;
using Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Domain.Core.Tests.LifecycleAction.Settings;

/// <summary>
/// Тесты для <see cref="LifecycleActionSettings"/> — настройки включения/отключения доменных событий по тройному ключу (тип сущности, тип события, тип перечисления).
/// </summary>
public sealed class LifecycleActionSettingsTests
{
    /// <summary>
    /// Проверяет, что вызов <see cref="LifecycleActionSettings.Switch(System.Type,LifecycleHookType,System.Enum,bool)"/> с тройным ключом и значением <c>false</c> отключает событие.
    /// </summary>
    [Fact]
    public void Switch_TripleKey_EnablesDisablesAction()
    {
        // Arrange
        var settings = new LifecycleActionSettings(Enabled: true);

        // Act
        settings.Switch(typeof(string), LifecycleHookType.BeforeSave, TestEnum.BeforeCreate, false);

        // Assert
        settings.AnyElementEnabled(typeof(string), LifecycleHookType.BeforeSave, TestEnum.BeforeCreate).Should().BeFalse();
    }

    /// <summary>
    /// Проверяет, что <see cref="LifecycleActionSettings.AnyElementEnabled(System.Type,LifecycleHookType,System.Enum)"/> возвращает <c>true</c> для тройного ключа, когда настройки разрешают все события.
    /// </summary>
    [Fact]
    public void AnyElementEnabled_ByTripleKey_ReturnsCorrectValue()
    {
        // Arrange
        var settings = new LifecycleActionSettings(Enabled: true);

        // Act
        var result = settings.AnyElementEnabled(typeof(int), LifecycleHookType.AfterSave, TestEnum.AfterCreate);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что вызов <see cref="LifecycleActionSettings.Switch(bool)"/> с <c>true</c> включает все события, даже если конкретный тройной ключ был ранее отключен.
    /// </summary>
    [Fact]
    public void Switch_RootTrue_EnablesAll()
    {
        // Arrange
        var settings = new LifecycleActionSettings(Enabled: true);
        settings.Switch(typeof(string), LifecycleHookType.BeforeSave, TestEnum.AfterUpdate, false);

        // Act
        settings.Switch(true);

        // Assert
        settings.AnyElementEnabled(typeof(string), LifecycleHookType.BeforeSave, TestEnum.AfterUpdate).Should().BeTrue();
    }
}
