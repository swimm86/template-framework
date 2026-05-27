// ----------------------------------------------------------------------------------------------
// <copyright file="EntityLifecycleAction.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.LifecycleAction;

/// <summary>
/// Действие перехвата отдельной сущности.
/// </summary>
public class EntityLifecycleAction
    : EntityLifecycleActionBase
{
    private readonly Func<IServiceProvider, CancellationToken, Task> _action;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="EntityLifecycleAction"/>.
    /// </summary>
    /// <param name="key">Ключ действия.</param>
    /// <param name="action">Действие, которое реализует перехват.</param>
    public EntityLifecycleAction(
        Enum key,
        Func<IServiceProvider, CancellationToken, Task> action)
        : base(key)
    {
        _action = action;
    }

    /// <inheritdoc />
    protected override Task ExecuteActionAsync(
        IServiceProvider serviceProvider,
        ICollection<IWithLifecycleActions> entities,
        CancellationToken cancellationToken)
        => _action(serviceProvider, cancellationToken);

    /// <summary>
    /// Пустая реализация — данное действие не отключает действия других сущностей,
    /// так как предназначено для обработки отдельной сущности без side-эффектов на коллекцию.
    /// </summary>
    /// <param name="hookType">Тип перехвата.</param>
    /// <param name="entities">Сущности для обработки.</param>
    /// TODO: разделить на действия для одной сущности и для всех.
    protected override void DisableEntitiesActions(
        LifecycleHookType hookType,
        ICollection<IWithLifecycleActions> entities)
    {
    }
}
