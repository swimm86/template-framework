// ----------------------------------------------------------------------------------------------
// <copyright file="LifecycleActionSettings.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Enums;
using Shared.Domain.Core.LifecycleAction.Settings.Base;

namespace Shared.Domain.Core.LifecycleAction.Settings;

/// <summary>
/// Модель настройки действий перехвата жизненного цикла.
/// </summary>
/// <param name="Enabled">Признак того, что по умолчанию настройка включена.</param>
public record LifecycleActionSettings(bool Enabled = true)
    : ActionSettingsWithInternalSettingsBase<
        Type,
        EntityTypeActionSettings,
        LifecycleHookType,
        Dictionary<LifecycleHookType, ActionKeySettings>>(
        Enabled)
{
    /// <inheritdoc />
    protected override EntityTypeActionSettings CreateExceptItem(bool enabled) =>
        new(enabled);

    /// <summary>
    /// Проверяет, включено ли действие перехвата.
    /// </summary>
    /// <param name="typeKey">Тип сущности.</param>
    /// <param name="hookTypeKey">Тип перехвата.</param>
    /// <param name="actionKey">Ключ действия.</param>
    /// <returns><see langword="true"/>, если действие включено.</returns>
    public bool AnyElementEnabled(Type typeKey, LifecycleHookType hookTypeKey, Enum actionKey) =>
        AnyElementEnabled(typeKey, x => x?.AnyElementEnabled(hookTypeKey, actionKey));

    /// <summary>
    /// Меняет состояние настройки действия перехвата.
    /// </summary>
    /// <param name="typeKey">Тип сущности.</param>
    /// <param name="hookTypeKey">Тип перехвата.</param>
    /// <param name="actionKey">Ключ действия.</param>
    /// <param name="enabled">Состояние настройки.</param>
    public void Switch(Type typeKey, LifecycleHookType hookTypeKey, Enum actionKey, bool enabled) =>
        Switch(typeKey, enabled, (item, enableItem) => item?.Switch(hookTypeKey, actionKey, enableItem));
}
