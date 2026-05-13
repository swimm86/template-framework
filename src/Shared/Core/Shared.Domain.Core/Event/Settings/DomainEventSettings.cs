// ----------------------------------------------------------------------------------------------
// <copyright file="DomainEventSettings.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Event.Settings.Base;

namespace Shared.Domain.Core.Event.Settings;

/// <summary>
/// Модель настройки ивентов.
/// </summary>
/// <param name="Enabled">Признак того, что по умолчанию настройка включена.</param>
public record DomainEventSettings(bool Enabled = true)
    : EventSettingsWithInternalSettingsBase<
        Type,
        TypeEventSettings,
        DomainEventType,
        Dictionary<DomainEventType, EventTypeEventSettings>>(
        Enabled)
{
    /// <inheritdoc />
    protected override TypeEventSettings CreateExceptItem(bool enabled) =>
        new(enabled);

    /// <summary>
    /// Проверяет, включено ли событие.
    /// </summary>
    /// <param name="typeKey">Тип сущности.</param>
    /// <param name="eventTypeKey">Тип события.</param>
    /// <param name="eventKey">Ключ события.</param>
    /// <returns><see langword="true"/>, если событие включено.</returns>
    public bool AnyElementEnabled(Type typeKey, DomainEventType eventTypeKey, Enum eventKey) =>
        AnyElementEnabled(typeKey, x => x?.AnyElementEnabled(eventTypeKey, eventKey));

    /// <summary>
    /// Меняет состояние настройки события.
    /// </summary>
    /// <param name="typeKey">Тип сущности.</param>
    /// <param name="eventTypeKey">Тип события.</param>
    /// <param name="eventKey">Ключ события.</param>
    /// <param name="enabled">Состояние настройки.</param>
    public void Switch(Type typeKey, DomainEventType eventTypeKey, Enum eventKey, bool enabled) =>
        Switch(typeKey, enabled, (item, enableItem) => item?.Switch(eventTypeKey, eventKey, enableItem));
}