using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Event.Settings;
using Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Domain.Core.Tests.Event.Settings;

/// <summary>
/// Тесты для <see cref="DomainEventSettings"/> — настройки включения/отключения доменных событий по тройному ключу (тип сущности, тип события, тип перечисления).
/// </summary>
public sealed class DomainEventSettingsTests
{
    /// <summary>
    /// Проверяет, что вызов <see cref="DomainEventSettings.Switch(System.Type,DomainEventType,System.Enum,bool)"/> с тройным ключом и значением <c>false</c> отключает событие.
    /// </summary>
    [Fact]
    public void Switch_TripleKey_EnablesDisablesEvent()
    {
        // Arrange
        var settings = new DomainEventSettings(Enabled: true);

        // Act
        settings.Switch(typeof(string), DomainEventType.BeforeSave, TestEnum.BeforeCreate, false);

        // Assert
        settings.AnyElementEnabled(typeof(string), DomainEventType.BeforeSave, TestEnum.BeforeCreate).Should().BeFalse();
    }

    /// <summary>
    /// Проверяет, что <see cref="DomainEventSettings.AnyElementEnabled(System.Type,DomainEventType,System.Enum)"/> возвращает <c>true</c> для тройного ключа, когда настройки разрешают все события.
    /// </summary>
    [Fact]
    public void AnyElementEnabled_ByTripleKey_ReturnsCorrectValue()
    {
        // Arrange
        var settings = new DomainEventSettings(Enabled: true);

        // Act
        var result = settings.AnyElementEnabled(typeof(int), DomainEventType.AfterSave, TestEnum.AfterCreate);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что вызов <see cref="DomainEventSettings.Switch(bool)"/> с <c>true</c> включает все события, даже если конкретный тройной ключ был ранее отключен.
    /// </summary>
    [Fact]
    public void Switch_RootTrue_EnablesAll()
    {
        // Arrange
        var settings = new DomainEventSettings(Enabled: true);
        settings.Switch(typeof(string), DomainEventType.BeforeSave, TestEnum.AfterUpdate, false);

        // Act
        settings.Switch(true);

        // Assert
        settings.AnyElementEnabled(typeof(string), DomainEventType.BeforeSave, TestEnum.AfterUpdate).Should().BeTrue();
    }
}
