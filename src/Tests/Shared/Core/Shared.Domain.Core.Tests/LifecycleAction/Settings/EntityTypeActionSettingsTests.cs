using Shared.Domain.Core.Enums;
using Shared.Domain.Core.LifecycleAction.Settings;
using Shared.Domain.Core.LifecycleAction.Settings.Base;
using Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Domain.Core.Tests.LifecycleAction.Settings;

/// <summary>
/// Тесты для <see cref="EntityTypeActionSettings"/> — настройки включения/отключения доменных событий по двойному ключу (тип события, тип перечисления).
/// </summary>
public sealed class EntityTypeActionSettingsTests
{
    /// <summary>
    /// Проверяет, что вызов <see cref="ActionSettingsWithInternalSettingsBase{TKey, TItem, TExceptKey, TExceptItems}.Switch(TKey,TExceptKey, bool)"/> создаёт дочерние настройки <see cref="ActionKeySettings"/>, если их ещё нет.
    /// </summary>
    [Fact]
    public void Switch_CreatesActionKeySettings_IfNotExists()
    {
        // Arrange
        var settings = new EntityTypeActionSettings(Enabled: true);

        // Act
        settings.Switch(LifecycleHookType.BeforeSave, TestEnum.BeforeCreate, false);

        // Assert
        settings.AnyElementEnabled(LifecycleHookType.BeforeSave, TestEnum.BeforeCreate).Should().BeFalse();
    }

    /// <summary>
    /// Проверяет, что повторный вызов <see cref="ActionSettingsWithInternalSettingsBase{TKey, TItem, TExceptKey, TExceptItems}.Switch(TKey,TExceptKey, bool)"/> обновляет существующие настройки, а не создаёт новые.
    /// </summary>
    [Fact]
    public void Switch_UpdatesExistingActionKeySettings()
    {
        // Arrange
        var settings = new EntityTypeActionSettings(Enabled: true);
        settings.Switch(LifecycleHookType.BeforeSave, TestEnum.BeforeCreate, false);

        // Act
        settings.Switch(LifecycleHookType.BeforeSave, TestEnum.BeforeCreate, true);

        // Assert
        settings.AnyElementEnabled(LifecycleHookType.BeforeSave, TestEnum.BeforeCreate).Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что <see cref="ActionSettingsWithInternalSettingsBase{TKey, TItem, TExceptKey, TExceptItems}.AnyElementEnabled(TKey)"/> (без указания конкретного перечисления) делегирует вызов в <see cref="ActionKeySettings"/>.
    /// </summary>
    [Fact]
    public void AnyElementEnabled_DelegatesToActionKeySettings()
    {
        // Arrange
        var settings = new EntityTypeActionSettings(Enabled: true);

        // Act
        var result = settings.AnyElementEnabled(LifecycleHookType.BeforeSave);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что вызов <see cref="EntityTypeActionSettings.Switch(bool)"/> на корневом уровне с <c>true</c> включает все события, даже ранее отключенные.
    /// </summary>
    [Fact]
    public void SwitchOnRoot_EnablesAll()
    {
        // Arrange
        var settings = new EntityTypeActionSettings(Enabled: true);
        settings.Switch(LifecycleHookType.BeforeSave, TestEnum.AfterUpdate, false);

        // Act
        settings.Switch(true);

        // Assert
        settings.AnyElementEnabled(LifecycleHookType.BeforeSave, TestEnum.AfterUpdate).Should().BeTrue();
    }
}
