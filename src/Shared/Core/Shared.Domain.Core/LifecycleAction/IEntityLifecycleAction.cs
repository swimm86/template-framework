// ----------------------------------------------------------------------------------------------
// <copyright file="IEntityLifecycleAction.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.LifecycleAction;

/// <summary>
/// Определяет контракт для действия перехвата жизненного цикла сущности.
/// </summary>
public interface IEntityLifecycleAction
{
    /// <summary>
    /// Ключ действия.
    /// </summary>
    Enum Key { get; }

    /// <summary>
    /// Выполняет действие перехвата.
    /// </summary>
    /// <param name="hookType">Тип перехвата жизненного цикла.</param>
    /// <param name="serviceProvider">Провайдер сервисов для получения зависимостей.</param>
    /// <param name="entities">Сущности для обработки.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Результат выполнения асинхронной операции.</returns>
    Task ExecuteAsync(
        LifecycleHookType hookType,
        IServiceProvider serviceProvider,
        ICollection<IWithLifecycleActions> entities,
        CancellationToken cancellationToken);

    /// <summary>
    /// Включает действие.
    /// </summary>
    void Enable();

    /// <summary>
    /// Отключает действие.
    /// </summary>
    void Disable();
}
