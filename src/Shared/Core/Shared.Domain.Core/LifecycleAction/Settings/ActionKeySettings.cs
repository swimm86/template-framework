// ----------------------------------------------------------------------------------------------
// <copyright file="ActionKeySettings.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Common.Extensions;
using Shared.Domain.Core.LifecycleAction.Settings.Base;

namespace Shared.Domain.Core.LifecycleAction.Settings;

/// <summary>
/// Модель настройки действий перехвата по ключу (flags).
/// </summary>
/// <param name="Enabled">Признак того, что по умолчанию настройка включена.</param>
public record ActionKeySettings(bool Enabled = true)
    : ActionSettingBase<Enum?, Enum>(Enabled)
{
    /// <inheritdoc />
    protected override Enum? GetDefaultItems => null;

    /// <inheritdoc />
    protected sealed override bool HasExceptItems => ExceptItems != null;

    /// <inheritdoc />
    public override bool AnyElementEnabled(Enum itemKey)
    {
        var hasFlag = ExceptItems?.HasFlag(itemKey) ?? false;

        return Enabled ? !hasFlag : hasFlag;
    }

    /// <inheritdoc />
    public override void Switch(Enum itemKey, bool enabled)
    {
        ExceptItems = Enabled == enabled
            ? ExceptItems?.Without(itemKey)
            : ExceptItems?.With(itemKey) ?? itemKey;
    }
}
