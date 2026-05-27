// ----------------------------------------------------------------------------------------------
// <copyright file="BaseEntity.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Shared.Common.Extensions;
using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;
using Shared.Domain.Core.LifecycleAction;

namespace Shared.Domain.Core.Base;

/// <summary>
/// Абстрактный базовый класс сущности.
/// </summary>
/// <typeparam name="TKey"> Тип ключа сущности. </typeparam>
public abstract class BaseEntity<TKey>
    : IEntity<TKey>, IWithLifecycleActions
{
    /// <summary>
    /// Словарь действий перехвата до сохранения.
    /// </summary>
    [NotMapped]
    private ReadOnlyDictionary<Enum, IEntityLifecycleAction> _lifecycleActionsBeforeSave = null!;

    /// <summary>
    /// Словарь действий перехвата после сохранения.
    /// </summary>
    [NotMapped]
    private ReadOnlyDictionary<Enum, IEntityLifecycleAction> _lifecycleActionsAfterSave = null!;

    /// <inheritdoc />
    public virtual TKey Id { get; set; } = default!;

    /// <inheritdoc />
    public virtual string[] RequiredToSaveNavigationPropertiesNames => [];

    /// <summary>
    /// Действия, выполняемые перед сохранением.
    /// </summary>
    protected virtual IEntityLifecycleAction[] BeforeSaveActions => [];

    /// <summary>
    /// Действия, выполняемые после сохранения.
    /// </summary>
    protected virtual IEntityLifecycleAction[] AfterSaveActions => [];

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="BaseEntity{TKey}"/>.
    /// </summary>
    protected BaseEntity()
    {
        CreateActions();
    }

    /// <inheritdoc />
    public bool TryGetAction(
        LifecycleHookType hookType,
        Enum key,
        [MaybeNullWhen(false)] out IEntityLifecycleAction lifecycleAction)
        => GetCurrentLifecycleActions(hookType).TryGetValue(key, out lifecycleAction);

    /// <inheritdoc />
    public void ResetActions() =>
        EnableLifecycleActions();

    /// <inheritdoc />
    public ICollection<Enum> GetAllKeys(LifecycleHookType hookType)
        => GetCurrentLifecycleActions(hookType).Keys;

    /// <summary>
    /// Отключает действия перехвата жизненного цикла.
    /// </summary>
    public void DisableLifecycleActions()
    {
        DisableLifecycleActions(LifecycleHookType.BeforeSave);
        DisableLifecycleActions(LifecycleHookType.AfterSave);
    }

    /// <summary>
    /// Отключает действия перехвата жизненного цикла.
    /// </summary>
    /// <param name="hookType">Тип перехвата.</param>
    /// <param name="flags">Флаги действия (если <see langword="null"/>, то берутся все действия типа).</param>
    public void DisableLifecycleActions(
        LifecycleHookType hookType,
        Enum? flags = null)
        => UpdateLifecycleActions(hookType, flags, x => x.Disable());

    /// <summary>
    /// Включает действия перехвата жизненного цикла.
    /// </summary>
    public void EnableLifecycleActions()
    {
        EnableLifecycleActions(LifecycleHookType.BeforeSave);
        EnableLifecycleActions(LifecycleHookType.AfterSave);
    }

    /// <param name="hookType">Тип перехвата.</param>
    /// <param name="flags">Флаги действия (если <see langword="null"/>, то берутся все действия типа).</param>
    /// <inheritdoc cref="EnableLifecycleActions()"/>
    public void EnableLifecycleActions(
        LifecycleHookType hookType,
        Enum? flags = null)
        => UpdateLifecycleActions(hookType, flags, x => x.Enable());

    /// <summary>
    /// Создает действия перехвата.
    /// </summary>
    private void CreateActions()
    {
        _lifecycleActionsBeforeSave = new ReadOnlyDictionary<Enum, IEntityLifecycleAction>(BeforeSaveActions.ToDictionary(x => x.Key));
        _lifecycleActionsAfterSave = new ReadOnlyDictionary<Enum, IEntityLifecycleAction>(AfterSaveActions.ToDictionary(x => x.Key));
    }

    /// <summary>
    /// Обновляет действия перехвата.
    /// </summary>
    /// <param name="hookType">Тип перехвата.</param>
    /// <param name="flags">Флаги действия (если <see langword="null"/>, то берутся все действия типа).</param>
    /// <param name="action">Действие над перехватчиком.</param>
    private void UpdateLifecycleActions(
        LifecycleHookType hookType,
        Enum? flags,
        Action<IEntityLifecycleAction> action)
    {
        var actions = GetCurrentLifecycleActions(hookType);

        actions.Where(x => flags?.HasFlag(x.Key) ?? true)
            .ForEach(x => action(x.Value));
    }

    /// <summary>
    /// Чтение действий перехвата по типу.
    /// </summary>
    /// <param name="hookType"> Тип перехвата. </param>
    /// <returns> Коллекцию действий перехвата выбранного типа. </returns>
    private ReadOnlyDictionary<Enum, IEntityLifecycleAction> GetCurrentLifecycleActions(
        LifecycleHookType hookType)
        => hookType == LifecycleHookType.AfterSave
            ? _lifecycleActionsAfterSave
            : _lifecycleActionsBeforeSave;
}
