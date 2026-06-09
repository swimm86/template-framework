// ----------------------------------------------------------------------------------------------
// <copyright file="ILifecycleActionOrchestrator.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.LifecycleAction.Interfaces;

/// <summary>
/// Управляет действиями перехвата жизненного цикла сущностей.
/// Хранит карту отслеживаемых сущностей и настройки активности действий.
/// </summary>
/// <remarks>
/// <para>
/// <b>Не потокобезопасен.</b> Координирует состояние реестра сущностей
/// и gate-а настроек активности в рамках scoped-lifetime.
/// Совместный доступ из нескольких потоков на одной инстанции
/// не поддерживается.
/// </para>
/// <para>
/// Корректный сценарий: один scope (HTTP-запрос, джоба, фоновый процесс)
/// владеет ровно одной инстанцией оркестратора; handler-ы вызываются
/// последовательно в <see cref="DispatchAsync"/>.
/// </para>
/// </remarks>
public interface ILifecycleActionOrchestrator
{
    /// <summary>
    /// Добавляет сущности в карту отслеживаемых.
    /// </summary>
    /// <param name="entities">Сущности для добавления. Не может быть <c>null</c>.</param>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="entities"/> равен <c>null</c>.
    /// </exception>
    void AddEntities(IEnumerable<IEntity> entities);

    /// <summary>
    /// Удаляет сущности из карты отслеживаемых.
    /// </summary>
    /// <param name="entities">Сущности для удаления. Не может быть <c>null</c>.</param>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="entities"/> равен <c>null</c>.
    /// </exception>
    void RemoveEntities(IEnumerable<IEntity> entities);

    /// <summary>
    /// Получает имена свойств, которые необходимо загрузить для сущностей указанного типа перед выполнением действий перехвата.
    /// </summary>
    /// <param name="entityType">Тип сущности. Не может быть <c>null</c>.</param>
    /// <returns>Массив имён свойств, требующих загрузки.</returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="entityType"/> равен <c>null</c>.
    /// </exception>
    string[] GetRequiredProperties(Type entityType);

    /// <summary>
    /// Определяет, разрешено ли выполнение действия перехвата для указанной сущности.
    /// </summary>
    /// <param name="entity">Сущность для проверки. Не может быть <c>null</c>.</param>
    /// <param name="key">Ключ действия перехвата. Не может быть <c>null</c>.</param>
    /// <param name="phase">Фаза жизненного цикла.</param>
    /// <returns><c>true</c>, если действие разрешено; иначе <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="entity"/> или <paramref name="key"/> равны <c>null</c>.
    /// </exception>
    bool IsActionEnabled(IEntity entity, string key, LifecyclePhase phase);

    /// <summary>
    /// Включает все действия перехвата для всех отслеживаемых сущностей.
    /// </summary>
    void EnableActions();

    /// <summary>
    /// Отключает все действия перехвата для всех отслеживаемых сущностей.
    /// </summary>
    void DisableActions();

    /// <summary>
    /// Включает указанные действия перехвата глобально.
    /// </summary>
    /// <param name="keys">Ключи действий для включения. Не может быть <c>null</c>.</param>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="keys"/> равен <c>null</c>.
    /// </exception>
    void EnableActions(IReadOnlyList<string> keys);

    /// <summary>
    /// Отключает указанные действия перехвата глобально.
    /// </summary>
    /// <param name="keys">Ключи действий для отключения. Не может быть <c>null</c>.</param>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="keys"/> равен <c>null</c>.
    /// </exception>
    void DisableActions(IReadOnlyList<string> keys);

    /// <summary>
    /// Включает указанное действие перехвата для заданной сущности.
    /// Короткая форма для самого частого сценария — один ключ + одна сущность.
    /// </summary>
    /// <param name="key">Ключ действия для включения. Не может быть <c>null</c>.</param>
    /// <param name="entity">Сущность, для которой включается действие. Не может быть <c>null</c>.</param>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="key"/> или <paramref name="entity"/> равны <c>null</c>.
    /// </exception>
    void EnableActionForEntity(string key, IEntity entity);

    /// <summary>
    /// Отключает указанное действие перехвата для заданной сущности.
    /// Короткая форма для самого частого сценария — один ключ + одна сущность.
    /// </summary>
    /// <param name="key">Ключ действия для отключения. Не может быть <c>null</c>.</param>
    /// <param name="entity">Сущность, для которой отключается действие. Не может быть <c>null</c>.</param>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="key"/> или <paramref name="entity"/> равны <c>null</c>.
    /// </exception>
    void DisableActionForEntity(string key, IEntity entity);

    /// <summary>
    /// Включает указанные действия перехвата для заданных сущностей.
    /// </summary>
    /// <param name="keys">Ключи действий для включения. Не может быть <c>null</c>.</param>
    /// <param name="entities">Сущности, для которых включаются действия. Не может быть <c>null</c>.</param>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="keys"/> или <paramref name="entities"/> равны <c>null</c>.
    /// </exception>
    void EnableActionsForEntities(IReadOnlyList<string> keys, IReadOnlyList<IEntity> entities);

    /// <summary>
    /// Отключает указанные действия перехвата для заданных сущностей.
    /// </summary>
    /// <param name="keys">Ключи действий для отключения. Не может быть <c>null</c>.</param>
    /// <param name="entities">Сущности, для которых отключаются действия. Не может быть <c>null</c>.</param>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="keys"/> или <paramref name="entities"/> равны <c>null</c>.
    /// </exception>
    void DisableActionsForEntities(IReadOnlyList<string> keys, IReadOnlyList<IEntity> entities);

    /// <summary>
    /// Включает все действия перехвата заданной фазы для всех отслеживаемых сущностей.
    /// </summary>
    /// <param name="phase">Фаза жизненного цикла.</param>
    void EnablePhase(LifecyclePhase phase);

    /// <summary>
    /// Отключает все действия перехвата заданной фазы для всех отслеживаемых сущностей.
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
    /// Выполняет диспетчеризацию действий перехвата для указанной фазы жизненного цикла.
    /// </summary>
    /// <param name="phase">Фаза жизненного цикла.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Результат выполнения асинхронной операции.</returns>
    Task DispatchAsync(LifecyclePhase phase, CancellationToken cancellationToken);

    /// <summary>
    /// Сбрасывает все настройки активности действий перехвата.
    /// </summary>
    void ResetAllActions();
}
