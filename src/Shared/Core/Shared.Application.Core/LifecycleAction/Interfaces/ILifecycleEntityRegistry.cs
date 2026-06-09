// ----------------------------------------------------------------------------------------------
// <copyright file="ILifecycleEntityRegistry.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.LifecycleAction.Interfaces;

/// <summary>
/// Хранит набор отслеживаемых сущностей, для которых orchestrator
/// выполняет диспетчеризацию действий перехвата жизненного цикла.
/// </summary>
/// <remarks>
/// Реестр использует <see cref="EntityKey"/> в качестве ключа коллекции,
/// чтобы одна и та же доменная сущность оставалась идентифицируемой
/// при подмене экземпляра (Attach/Detach, разные сессии, прокси EF).
/// </remarks>
public interface ILifecycleEntityRegistry
{
    /// <summary>
    /// Добавляет сущности в реестр. Идемпотентно: повторное добавление
    /// той же сущности не приводит к дубликатам.
    /// </summary>
    /// <param name="entities">Сущности для добавления.</param>
    void Track(IEnumerable<IEntity> entities);

    /// <summary>
    /// Удаляет сущности из реестра. Удаляет также все per-entity
    /// настройки активности, связанные с этими сущностями.
    /// </summary>
    /// <param name="entities">Сущности для удаления.</param>
    void Untrack(IEnumerable<IEntity> entities);

    /// <summary>
    /// Возвращает перечисление текущих отслеживаемых сущностей.
    /// </summary>
    /// <returns>Снимок текущего состояния реестра.</returns>
    IReadOnlyCollection<IEntity> Snapshot();

    /// <summary>
    /// Очищает реестр.
    /// </summary>
    void Clear();
}
