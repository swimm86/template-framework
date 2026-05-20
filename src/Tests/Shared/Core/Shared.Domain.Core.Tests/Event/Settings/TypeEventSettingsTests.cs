using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Event.Settings;
using Shared.Domain.Core.Event.Settings.Base;
using Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Domain.Core.Tests.Event.Settings;

/// <summary>
/// Тесты для <see cref="TypeEventSettings"/> — настройки включения/отключения доменных событий по двойному ключу (тип события, тип перечисления).
/// </summary>
public sealed class TypeEventSettingsTests
{
    /// <summary>
    /// Проверяет, что вызов <see cref="EventSettingsWithInternalSettingsBase{TKey, TItem, TExceptKey, TExceptItems}.Switch(TKey,TExceptKey, bool)"/> создаёт дочерние настройки <see cref="EventTypeEventSettings"/>, если их ещё нет.
    /// </summary>
    [Fact]
    public void Switch_CreatesEventTypeEventSettings_IfNotExists()
    {
        // Arrange
        var settings = new TypeEventSettings(Enabled: true);

        // Act
        settings.Switch(DomainEventType.BeforeSave, TestEnum.BeforeCreate, false);

        // Assert
        settings.AnyElementEnabled(DomainEventType.BeforeSave, TestEnum.BeforeCreate).Should().BeFalse();
    }

    /// <summary>
    /// Проверяет, что повторный вызов <see cref="EventSettingsWithInternalSettingsBase{TKey, TItem, TExceptKey, TExceptItems}.Switch(TKey,TExceptKey, bool)"/> обновляет существующие настройки, а не создаёт новые.
    /// </summary>
    [Fact]
    public void Switch_UpdatesExistingEventTypeEventSettings()
    {
        // Arrange
        var settings = new TypeEventSettings(Enabled: true);
        settings.Switch(DomainEventType.BeforeSave, TestEnum.BeforeCreate, false);

        // Act
        settings.Switch(DomainEventType.BeforeSave, TestEnum.BeforeCreate, true);

        // Assert
        settings.AnyElementEnabled(DomainEventType.BeforeSave, TestEnum.BeforeCreate).Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что <see cref="EventSettingsWithInternalSettingsBase{TKey, TItem, TExceptKey, TExceptItems}.AnyElementEnabled(TKey)"/> (без указания конкретного перечисления) делегирует вызов в <see cref="EventTypeEventSettings"/>.
    /// </summary>
    [Fact]
    public void AnyElementEnabled_DelegatesToEventTypeSettings()
    {
        // Arrange
        var settings = new TypeEventSettings(Enabled: true);

        // Act
        var result = settings.AnyElementEnabled(DomainEventType.BeforeSave);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что вызов <see cref="TypeEventSettings.Switch(bool)"/> на корневом уровне с <c>true</c> включает все события, даже ранее отключенные.
    /// </summary>
    [Fact]
    public void SwitchOnRoot_EnablesAll()
    {
        // Arrange
        var settings = new TypeEventSettings(Enabled: true);
        settings.Switch(DomainEventType.BeforeSave, TestEnum.AfterUpdate, false);

        // Act
        settings.Switch(true);

        // Assert
        settings.AnyElementEnabled(DomainEventType.BeforeSave, TestEnum.AfterUpdate).Should().BeTrue();
    }
}
