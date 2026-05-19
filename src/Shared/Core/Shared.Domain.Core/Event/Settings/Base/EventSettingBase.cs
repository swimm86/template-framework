// ----------------------------------------------------------------------------------------------
// <copyright file="EventSettingBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Event.Settings.Base;

/// <summary>
/// Базовая модель настройки событий.
/// </summary>
/// <typeparam name="TItems">Тип исключенных элементов.</typeparam>
/// <typeparam name="TKey">Тип ключа исключенного элемента.</typeparam>
public abstract record EventSettingBase<TItems, TKey>
{
    /// <summary>
    /// Признак того, что по умолчанию настройка включена.
    /// </summary>
    protected bool Enabled { get; private set; }

    /// <summary>
    /// Признак того, что у настройки нет исключений.
    /// </summary>
    protected abstract bool HasExceptItems { get; }

    /// <summary>
    /// Элементы-исключения.
    /// </summary>
    protected TItems ExceptItems { get; set; } = default!;

    /// <summary>
    /// Получает элементы по умолчанию.
    /// </summary>
    protected abstract TItems GetDefaultItems { get; }

    /// <summary>
    /// Признак того, что в настройке есть включенные элементы.
    /// </summary>
    public bool AnyEnabled => Enabled || HasExceptItems;

    /// <summary>
    /// Признак того, что в настройке есть отключенные элементы.
    /// </summary>
    public bool AnyDisabled => !Enabled || HasExceptItems;

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="enabled">Признак того, что элемент включен.</param>
    protected EventSettingBase(bool enabled)
    {
        Enabled = enabled;
        ClearExceptItems();
    }

    /// <summary>
    /// Проверяет, включен ли элемент-исключение.
    /// </summary>
    /// <param name="itemKey">Ключ элемента-исключения.</param>
    /// <returns><see langword="true"/>, если элемент включен.</returns>
    public abstract bool AnyElementEnabled(TKey itemKey);

    /// <summary>
    /// Меняет состояние настройки.
    /// </summary>
    /// <param name="enabled">Состояние настройки.</param>
    public void Switch(bool enabled)
    {
        Enabled = enabled;
        ClearExceptItems();
    }

    /// <summary>
    /// Меняет состояние настройки элемента-исключения.
    /// </summary>
    /// <param name="itemKey">Ключ элемента-исключения.</param>
    /// <param name="enabled">Состояние настройки.</param>
    public abstract void Switch(TKey itemKey, bool enabled);

    private void ClearExceptItems() => ExceptItems = GetDefaultItems;
}
