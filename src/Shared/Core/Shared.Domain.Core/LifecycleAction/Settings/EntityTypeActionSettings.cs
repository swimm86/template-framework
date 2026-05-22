// ----------------------------------------------------------------------------------------------
// <copyright file="EntityTypeActionSettings.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Enums;
using Shared.Domain.Core.LifecycleAction.Settings.Base;

namespace Shared.Domain.Core.LifecycleAction.Settings;

/// <summary>
/// Модель настройки действий перехвата по типу сущности и типу перехвата.
/// </summary>
/// <param name="Enabled">Признак того, что по умолчанию настройка включена.</param>
public record EntityTypeActionSettings(bool Enabled = true)
    : ActionSettingsWithInternalSettingsBase<LifecycleHookType, ActionKeySettings, Enum, Enum?>(Enabled)
{
    /// <inheritdoc />
    protected override ActionKeySettings CreateExceptItem(bool enabled) =>
        new(enabled);
}
