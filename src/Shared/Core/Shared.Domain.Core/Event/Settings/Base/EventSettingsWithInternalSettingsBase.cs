// ----------------------------------------------------------------------------------------------
// <copyright file="EventSettingsWithInternalSettingsBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Event.Settings.Base;

/// <summary>
/// Базовая модель настройки событий с вложенными настройками.
/// </summary>
/// <typeparam name="TKey">Тип ключа элемента-исключения.</typeparam>
/// <typeparam name="TItem">Тип элемента-исключения.</typeparam>
/// <typeparam name="TExceptKey">Тип ключа вложенного элемента-исключения.</typeparam>
/// <typeparam name="TExceptItems">Тип вложенных элементов-исключений.</typeparam>
/// <param name="Enabled">Признак того, что по умолчанию настройка включена.</param>
public abstract record EventSettingsWithInternalSettingsBase<TKey, TItem, TExceptKey, TExceptItems>(
    bool Enabled)
    : EventSettingBase<Dictionary<TKey, TItem>, TKey>(Enabled)
    where TKey : notnull
    where TItem : EventSettingBase<TExceptItems, TExceptKey>
{
    /// <inheritdoc />
    protected sealed override Dictionary<TKey, TItem> GetDefaultItems => [];

    /// <inheritdoc />
    protected sealed override bool HasExceptItems => ExceptItems.Any();

    /// <summary>
    /// Проверяет включение вложенного элемента.
    /// </summary>
    /// <param name="itemKey">Ключ элемента-исключения.</param>
    /// <param name="isEnabledFunc">Функция, определяющая включенность нужного элемента.</param>
    /// <returns><see langword="true"/>, если элемент включен.</returns>
    protected bool AnyElementEnabled(TKey itemKey, Func<TItem?, bool?> isEnabledFunc) =>
        isEnabledFunc(ExceptItems.GetValueOrDefault(itemKey)) ?? Enabled;

    /// <summary>
    /// Меняет состояние настройки внутреннего элемента-исключения.
    /// </summary>
    /// <param name="itemKey">Ключ элемента-исключения.</param>
    /// <param name="enabled">Состояние настройки.</param>
    /// <param name="switchAction">Действие переключения состояния.</param>
    protected void Switch(TKey itemKey, bool enabled, Action<TItem?, bool> switchAction)
    {
        if (!ExceptItems.TryGetValue(itemKey, out var eventTypeSettings) && Enabled != enabled)
        {
            eventTypeSettings = ExceptItems[itemKey] = CreateExceptItem(Enabled);
        }

        switchAction(eventTypeSettings, enabled);

        if (eventTypeSettings != null && Enabled ? !AnyDisabled : !AnyEnabled)
        {
            ExceptItems.Remove(itemKey);
        }
    }

    /// <summary>
    /// Создает элемент-исключение.
    /// </summary>
    /// <param name="enabled">Признак того, что элемент-исключение включен.</param>
    /// <returns>Элемент-исключение.</returns>
    protected abstract TItem CreateExceptItem(bool enabled);

    /// <inheritdoc />
    public sealed override bool AnyElementEnabled(TKey itemKey) =>
        AnyElementEnabled(itemKey, x => x?.AnyEnabled);

    /// <summary>
    /// Проверяет, включен ли внутренний элемент-исключение.
    /// </summary>
    /// <param name="itemKey">Ключ элемента-исключения.</param>
    /// <param name="exceptItemKey">Ключ внутреннего элемента-исключения.</param>
    /// <returns><see langword="true"/>, если элемент включен.</returns>
    public bool AnyElementEnabled(TKey itemKey, TExceptKey exceptItemKey) =>
        AnyElementEnabled(itemKey, x => x?.AnyElementEnabled(exceptItemKey));

    /// <inheritdoc />
    public sealed override void Switch(TKey itemKey, bool enabled) =>
        Switch(itemKey, enabled, (item, enableItem) => item?.Switch(enableItem));

    /// <summary>
    /// Меняет состояние настройки внутреннего элемента-исключения.
    /// </summary>
    /// <param name="itemKey">Ключ элемента-исключения.</param>
    /// <param name="exceptItemKey">Ключ внутреннего элемента-исключения.</param>
    /// <param name="enabled">Состояние настройки.</param>
    public void Switch(TKey itemKey, TExceptKey exceptItemKey, bool enabled) =>
        Switch(itemKey, enabled, (item, enableItem) => item?.Switch(exceptItemKey, enableItem));
}