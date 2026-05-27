// ----------------------------------------------------------------------------------------------
// <copyright file="ActionSettingsWithInternalSettingsBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.LifecycleAction.Settings.Base;

/// <summary>
/// Базовая модель настройки действий перехвата с вложенными настройками.
/// </summary>
/// <typeparam name="TKey">Тип ключа элемента-исключения.</typeparam>
/// <typeparam name="TItem">Тип элемента-исключения.</typeparam>
/// <typeparam name="TExceptKey">Тип ключа вложенного элемента-исключения.</typeparam>
/// <typeparam name="TExceptItems">Тип вложенных элементов-исключений.</typeparam>
/// <param name="Enabled">Признак того, что по умолчанию настройка включена.</param>
public abstract record ActionSettingsWithInternalSettingsBase<TKey, TItem, TExceptKey, TExceptItems>(
    bool Enabled)
    : ActionSettingBase<Dictionary<TKey, TItem>, TKey>(Enabled)
    where TKey : notnull
    where TItem : ActionSettingBase<TExceptItems, TExceptKey>
{
    /// <inheritdoc />
    protected sealed override Dictionary<TKey, TItem> GetDefaultItems => [];

    /// <inheritdoc />
    protected sealed override bool HasExceptItems => ExceptItems.Any();

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
        if (!ExceptItems.TryGetValue(itemKey, out var actionTypeSettings) && Enabled != enabled)
        {
            actionTypeSettings = ExceptItems[itemKey] = CreateExceptItem(Enabled);
        }

        switchAction(actionTypeSettings, enabled);

        if (ShouldRemoveItem(actionTypeSettings))
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

    /// <summary>
    /// Определяет, нужно ли удалить элемент-исключение из словаря.
    /// Элемент удаляется, если он пуст (не имеет исключений) и его состояние совпадает с родительским.
    /// </summary>
    private bool ShouldRemoveItem(TItem? actionTypeSettings)
    {
        return actionTypeSettings != null && (Enabled ? !AnyDisabled : !AnyEnabled);
    }
}
