// ----------------------------------------------------------------------------------------------
// <copyright file="LifecycleActionHandlerBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.LifecycleAction.Interfaces;
using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.LifecycleAction;

/// <summary>
/// Базовый класс обработчика действий перехвата для коллекции сущностей одного типа.
/// </summary>
/// <inheritdoc cref="ILifecycleActionHandler{TEntity}"/>
public abstract class LifecycleActionHandlerBase<TEntity>
    : ILifecycleActionHandler<TEntity>
    where TEntity : class, IEntity
{
    /// <inheritdoc />
    public abstract LifecyclePhase Phase { get; }

    /// <inheritdoc />
    public abstract string Key { get; }

    /// <inheritdoc />
    public abstract int Order { get; }

    /// <inheritdoc />
    public virtual string[] RequiredNavigationProperties => [];

    /// <inheritdoc />
    public Task ExecuteAsync(
        IEnumerable<IEntity> entities,
        CancellationToken cancellationToken)
    {
        return ((ILifecycleActionHandler<TEntity>)this).ExecuteAsync(
            entities.OfType<TEntity>(),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken)
    {
        if (entities.Any())
        {
            await ExecuteActionAsync(entities, cancellationToken);
        }
    }

    /// <summary>
    /// Выполняет действия перехвата.
    /// </summary>
    /// <param name="entities">Сущности для обработки.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Результат выполнения асинхронной операции.</returns>
    protected abstract Task ExecuteActionAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken);
}
