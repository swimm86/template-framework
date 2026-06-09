// ----------------------------------------------------------------------------------------------
// <copyright file="ILifecycleActionGate.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.LifecycleAction.Interfaces;

/// <summary>
/// Хранит и разрешает настройки активности действий перехвата
/// жизненного цикла: глобальные, по ключу, по фазе и по сущности.
/// </summary>
/// <remarks>
/// <para>
/// <b>Не потокобезопасен.</b> Все операции должны выполняться
/// в рамках одного логического контекста (scoped-lifetime в DI).
/// Совместный доступ из нескольких потоков на одной и той же инстанции
/// не поддерживается и приведёт к непредсказуемому поведению
/// (потере настроек, двойным вызовам handler-ов, нарушению порядка).
/// </para>
/// <para>
/// Если в будущем потребуется параллельная обработка в одном scope,
/// необходимо либо сериализовать доступ (lock/SemaphoreSlim),
/// либо спроектировать отдельную потокобезопасную реализацию.
/// </para>
/// </remarks>
public interface ILifecycleActionGate
{
    /// <summary>
    /// Определяет, разрешено ли выполнение действия перехвата
    /// для указанной сущности в указанной фазе.
    /// </summary>
    /// <param name="entity">Сущность для проверки.</param>
    /// <param name="key">Ключ действия перехвата.</param>
    /// <param name="phase">Фаза жизненного цикла.</param>
    /// <returns><c>true</c>, если действие разрешено; иначе <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="entity"/> равен <c>null</c>.
    /// </exception>
    bool IsEnabled(IEntity entity, string key, LifecyclePhase phase);

    /// <summary>
    /// Включает все действия перехвата для всех отслеживаемых сущностей.
    /// </summary>
    void Enable();

    /// <summary>
    /// Отключает все действия перехвата для всех отслеживаемых сущностей.
    /// </summary>
    void Disable();

    /// <summary>
    /// Включает указанные действия перехвата глобально.
    /// </summary>
    /// <param name="keys">Ключи действий для включения. Не может быть <c>null</c>.</param>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="keys"/> равен <c>null</c>.
    /// </exception>
    void Enable(IReadOnlyList<string> keys);

    /// <summary>
    /// Отключает указанные действия перехвата глобально.
    /// </summary>
    /// <param name="keys">Ключи действий для отключения. Не может быть <c>null</c>.</param>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="keys"/> равен <c>null</c>.
    /// </exception>
    void Disable(IReadOnlyList<string> keys);

    /// <summary>
    /// Включает указанные действия перехвата для заданной сущности.
    /// </summary>
    /// <param name="keys">Ключи действий для включения. Не может быть <c>null</c>.</param>
    /// <param name="entity">Сущность, для которой включаются действия. Не может быть <c>null</c>.</param>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="keys"/> или <paramref name="entity"/> равны <c>null</c>.
    /// </exception>
    void EnableForEntity(IReadOnlyList<string> keys, IEntity entity);

    /// <summary>
    /// Отключает указанные действия перехвата для заданной сущности.
    /// </summary>
    /// <param name="keys">Ключи действий для отключения. Не может быть <c>null</c>.</param>
    /// <param name="entity">Сущность, для которой отключаются действия. Не может быть <c>null</c>.</param>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="keys"/> или <paramref name="entity"/> равны <c>null</c>.
    /// </exception>
    void DisableForEntity(IReadOnlyList<string> keys, IEntity entity);

    /// <summary>
    /// Включает все действия перехвата заданной фазы глобально.
    /// </summary>
    /// <param name="phase">Фаза жизненного цикла.</param>
    void EnablePhase(LifecyclePhase phase);

    /// <summary>
    /// Отключает все действия перехвата заданной фазы глобально.
    /// </summary>
    /// <param name="phase">Фаза жизненного цикла.</param>
    void DisablePhase(LifecyclePhase phase);

    /// <summary>
    /// Включает все действия перехвата заданной фазы для указанной сущности.
    /// </summary>
    /// <param name="phase">Фаза жизненного цикла.</param>
    /// <param name="entity">Сущность, для которой включаются действия. Не может быть <c>null</c>.</param>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="entity"/> равен <c>null</c>.
    /// </exception>
    void EnablePhaseForEntity(LifecyclePhase phase, IEntity entity);

    /// <summary>
    /// Отключает все действия перехвата заданной фазы для указанной сущности.
    /// </summary>
    /// <param name="phase">Фаза жизненного цикла.</param>
    /// <param name="entity">Сущность, для которой отключаются действия. Не может быть <c>null</c>.</param>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="entity"/> равен <c>null</c>.
    /// </exception>
    void DisablePhaseForEntity(LifecyclePhase phase, IEntity entity);

    /// <summary>
    /// Удаляет все per-entity настройки для указанных сущностей.
    /// Вызывается из реестра при удалении сущности, чтобы конфигурация
    /// не удерживала мёртвые ссылки.
    /// </summary>
    /// <param name="entities">Сущности, для которых сбрасываются per-entity настройки. Не может быть <c>null</c>.</param>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="entities"/> равен <c>null</c>.
    /// </exception>
    void Forget(IEnumerable<IEntity> entities);

    /// <summary>
    /// Сбрасывает все настройки активности в состояние по умолчанию
    /// (все действия включены).
    /// </summary>
    void Reset();
}
