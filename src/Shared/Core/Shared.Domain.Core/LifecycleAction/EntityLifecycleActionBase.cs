// ----------------------------------------------------------------------------------------------
// <copyright file="EntityLifecycleActionBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Common.Extensions;
using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.LifecycleAction;

/// <summary>
/// Базовый класс действий перехвата при сохранении сущности, реализующей интерфейс <see cref="IEntity"/>.
/// </summary>
/// <param name="key">Ключ действия.</param>
public abstract class EntityLifecycleActionBase(Enum key)
    : IEntityLifecycleAction
{
    /// <summary>
    /// Признак того, что действие включено.
    /// Не является thread-safe — действия обрабатываются последовательно в рамках одного SaveChanges.
    /// </summary>
    private bool _enabled = true;

    /// <inheritdoc />
    public Enum Key { get; } = key;

    /// <inheritdoc />
    public async Task ExecuteAsync(
        LifecycleHookType hookType,
        IServiceProvider serviceProvider,
        ICollection<IWithLifecycleActions> entities,
        CancellationToken cancellationToken)
    {
        if (_enabled)
        {
            await ExecuteActionAsync(serviceProvider, entities, cancellationToken);
        }

        Disable();
        DisableEntitiesActions(hookType, entities);
    }

    /// <inheritdoc />
    public void Enable() => _enabled = true;

    /// <inheritdoc />
    public void Disable() => _enabled = false;

    /// <summary>
    /// Выполняет действия перехвата.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/>.</param>
    /// <param name="entities">Сущности для обработки.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Результат выполнения асинхронной операции.</returns>
    protected abstract Task ExecuteActionAsync(
        IServiceProvider serviceProvider,
        ICollection<IWithLifecycleActions> entities,
        CancellationToken cancellationToken);

    /// <summary>
    /// Отключает действия, которые обработались вместе с текущим.
    /// </summary>
    /// <param name="hookType">Тип перехвата.</param>
    /// <param name="entities">Сущности для обработки.</param>
    protected virtual void DisableEntitiesActions(
        LifecycleHookType hookType,
        ICollection<IWithLifecycleActions> entities) =>
        entities.ForEach(x =>
        {
            if (x.TryGetAction(hookType, Key, out var lifecycleAction))
            {
                lifecycleAction.Disable();
            }
        });
}
