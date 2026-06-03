// ----------------------------------------------------------------------------------------------
// <copyright file="IWithLifecycleActions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Shared.Common.Extensions;
using Shared.Domain.Core.Enums;
using Shared.Domain.Core.LifecycleAction;

namespace Shared.Domain.Core.Interfaces;

/// <summary>
/// Предоставляет интерфейс для чтения действий перехвата жизненного цикла сущности.
/// </summary>
public interface IWithLifecycleActions
{
    /// <summary>
    /// Имена свойств со связанными сущностями, которые необходимы для сохранения.
    /// </summary>
    string[] RequiredToSaveNavigationPropertiesNames { get; }

    /// <summary>
    /// Попытка извлечения действия перехвата.
    /// </summary>
    /// <param name="hookType">Тип перехвата жизненного цикла.</param>
    /// <param name="key">Ключ действия.</param>
    /// <param name="lifecycleAction">Действие перехвата.</param>
    /// <returns>Признак успешного выполнения операции.</returns>
    public bool TryGetAction(
        LifecycleHookType hookType,
        Enum key,
        [MaybeNullWhen(false)] out IEntityLifecycleAction lifecycleAction);

    /// <summary>
    /// Сбрасывает действия перехвата (повторно включает все отключённые действия).
    /// </summary>
    public void ResetActions();

    /// <summary>
    /// Получает все ключи действий перехвата заданного типа.
    /// </summary>
    /// <param name="hookType">Тип перехвата жизненного цикла.</param>
    /// <returns>Все ключи действий перехвата заданного типа.</returns>
    public ICollection<Enum> GetAllKeys(LifecycleHookType hookType);

    /// <summary>
    /// Выполняет обработку действия перехвата.
    /// </summary>
    /// <param name="hookType">Тип перехвата.</param>
    /// <param name="key">Ключ действия.</param>
    /// <param name="serviceProvider">Провайдер сервисов для получения зависимостей.</param>
    /// <param name="entities">Коллекция сущностей для обработки.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Результат выполнения асинхронной операции.</returns>
    public async Task ProcessLifecycleActionAsync(
        LifecycleHookType hookType,
        Enum key,
        IServiceProvider serviceProvider,
        ICollection<IWithLifecycleActions>? entities = null,
        CancellationToken cancellationToken = default)
    {
        entities ??= [];

        if (TryGetAction(hookType, key, out var lifecycleAction))
        {
            await lifecycleAction.ExecuteAsync(hookType, serviceProvider, entities, cancellationToken);
        }
    }

    /// <summary>
    /// Выполняет обработку всех действий перехвата заданного типа.
    /// </summary>
    /// <param name="hookType">Тип перехвата.</param>
    /// <param name="serviceProvider">Провайдер сервисов для получения зависимостей.</param>
    /// <param name="entities">Коллекция сущностей для обработки.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Результат выполнения асинхронной операции.</returns>
    public Task ProcessLifecycleActionsAsync(
        LifecycleHookType hookType,
        IServiceProvider serviceProvider,
        ICollection<IWithLifecycleActions>? entities = null,
        CancellationToken cancellationToken = default) =>
        GetAllKeys(hookType).ForeachAsync(
            key => ProcessLifecycleActionAsync(hookType, key, serviceProvider, entities, cancellationToken),
            cancellationToken);
}
