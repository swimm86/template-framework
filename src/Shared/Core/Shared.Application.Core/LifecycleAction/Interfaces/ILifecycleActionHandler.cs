// ----------------------------------------------------------------------------------------------
// <copyright file="ILifecycleActionHandler.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.LifecycleAction.Interfaces;

/// <summary>
/// Определяет контракт для обработчика действий перехвата жизненного цикла сущности.
/// </summary>
public interface ILifecycleActionHandler
{
    /// <summary>
    /// Тип сущности, к которой применяется обработчик.
    /// </summary>
    Type EntityType { get; }

    /// <summary>
    /// Фаза жизненного цикла сущности.
    /// </summary>
    LifecyclePhase Phase { get; }

    /// <summary>
    /// Ключ действия.
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Порядок выполнения.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Имена навигационных свойств, которые необходимо загрузить перед выполнением действия.
    /// </summary>
    string[] RequiredNavigationProperties => [];

    /// <summary>
    /// Выполняет действие перехвата.
    /// </summary>
    /// <param name="entities">Сущности для обработки.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Результат выполнения асинхронной операции.</returns>
    Task ExecuteAsync(
        IEnumerable<IEntity> entities,
        CancellationToken cancellationToken);
}

/// <inheritdoc cref="ILifecycleActionHandler"/>
/// <typeparam name="TEntity">Тип сущности.</typeparam>
public interface ILifecycleActionHandler<TEntity>
    : ILifecycleActionHandler
    where TEntity : class, IEntity
{
    /// <inheritdoc />
    Type ILifecycleActionHandler.EntityType => typeof(TEntity);

    /// <summary>
    /// Выполняет действие перехвата.
    /// </summary>
    /// <param name="entities">Сущности для обработки.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Результат выполнения асинхронной операции.</returns>
    Task ExecuteAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken);
}
