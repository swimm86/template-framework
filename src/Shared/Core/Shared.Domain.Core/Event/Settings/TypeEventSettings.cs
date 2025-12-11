// ----------------------------------------------------------------------------------------------
// <copyright file="TypeEventSettings.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Event.Settings.Base;

namespace Shared.Domain.Core.Event.Settings;

/// <summary>
/// Модель настройки ивентов по типу сущностей.
/// </summary>
/// <param name="Enabled">Признак того, что по умолчанию настройка включена.</param>
public record TypeEventSettings(bool Enabled = true)
    : EventSettingsWithInternalSettingsBase<DomainEventType, EventTypeEventSettings, Enum, Enum?>(Enabled)
{
    /// <inheritdoc />
    protected override EventTypeEventSettings CreateExceptItem(bool enabled) =>
        new(enabled);
}
