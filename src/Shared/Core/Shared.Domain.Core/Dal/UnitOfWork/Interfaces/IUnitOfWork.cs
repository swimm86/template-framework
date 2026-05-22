// ----------------------------------------------------------------------------------------------
// <copyright file="IUnitOfWork.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Dal.UnitOfWork.Interfaces;

/// <summary>
/// Определяет контракт для Unit of Work.
/// </summary>
public interface IUnitOfWork
    : IDisposable
{
    /// <summary>
    /// Сохраняет изменения.
    /// </summary>
    /// <param name="commitTransaction">Признак того, что необходимо закоммитить транзакцию.</param>
    /// <param name="resetLifecycleActionSettingsAfterSave">Сбросить настройки действий перехвата после сохранения.</param>
    /// <returns>Код результата.</returns>
    int SaveChanges(
        bool commitTransaction = true,
        bool resetLifecycleActionSettingsAfterSave = true);

    /// <summary>
    /// Асинхронно сохраняет изменения.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <param name="commitTransaction">Признак того, что необходимо закоммитить транзакцию.</param>
    /// <param name="resetLifecycleActionSettingsAfterSave">Сбросить настройки действий перехвата после сохранения.</param>
    /// <returns>Код результата.</returns>
    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default,
        bool commitTransaction = true,
        bool resetLifecycleActionSettingsAfterSave = true);

    /// <summary>
    /// Возвращает репозиторий с сущностями типа <typeparamref name="TEntity"/>.
    /// </summary>
    /// <typeparam name="TEntity">Тип сущности, для которого создается репозиторий.</typeparam>
    /// <returns>Репозиторий с сущностями типа <typeparamref name="TEntity"/>.</returns>
    IRepository<TEntity> GetRepository<TEntity>()
        where TEntity : class, IEntity;

    /// <summary>
    /// Осуществляет фиксацию транзакции.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Результат выполнения асинхронной операции.</returns>
    Task CommitTransactionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Осуществляет отмену изменений в рамках транзакции.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Результат выполнения асинхронной операции.</returns>
    Task RollbackTransactionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Осуществляет инициализацию новой транзакции.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Результат выполнения асинхронной операции.</returns>
    Task ResetTransactionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Включает использование транзакций (если еще не включено).
    /// </summary>
    /// <returns><see cref="IUnitOfWork"/>.</returns>
    IUnitOfWork EnableTransaction();

    /// <summary>
    /// Отменяет использование транзакций (если они используются).
    /// </summary>
    /// <returns><see cref="IUnitOfWork"/>.</returns>
    IUnitOfWork DisableTransaction();

    /// <summary>
    /// Отключает действия перехвата жизненного цикла.
    /// </summary>
    /// <returns><see cref="IUnitOfWork"/>.</returns>
    IUnitOfWork DisableLifecycleActions();

    /// <summary>
    /// Включает действия перехвата жизненного цикла.
    /// </summary>
    /// <returns><see cref="IUnitOfWork"/>.</returns>
    IUnitOfWork EnableLifecycleActions();

    /// <summary>
    /// Отключает действия перехвата жизненного цикла для типа сущности.
    /// </summary>
    /// <typeparam name="TEntity">Тип сущности.</typeparam>
    /// <param name="hookType">Тип перехвата (если <see langword="null"/>, то отключаются действия для всего типа).</param>
    /// <returns><see cref="IUnitOfWork"/>.</returns>
    IUnitOfWork DisableLifecycleActions<TEntity>(LifecycleHookType? hookType = default)
        where TEntity : IEntity, IWithLifecycleActions;

    /// <summary>
    /// Включает действия перехвата жизненного цикла для типа сущности.
    /// </summary>
    /// <typeparam name="TEntity">Тип сущности.</typeparam>
    /// <param name="hookType">Тип перехвата (если <see langword="null"/>, то включаются действия для всего типа).</param>
    /// <returns><see cref="IUnitOfWork"/>.</returns>
    IUnitOfWork EnableLifecycleActions<TEntity>(LifecycleHookType? hookType = default)
        where TEntity : IEntity, IWithLifecycleActions;

    /// <summary>
    /// Отключает действия перехвата жизненного цикла для типа сущности по флагам.
    /// </summary>
    /// <typeparam name="TEntity">Тип сущности.</typeparam>
    /// <param name="hookType">Тип перехвата.</param>
    /// <param name="actionKeyFlags">Флаги действий.</param>
    /// <returns><see cref="IUnitOfWork"/>.</returns>
    IUnitOfWork DisableLifecycleActions<TEntity>(LifecycleHookType hookType, Enum actionKeyFlags)
        where TEntity : IEntity, IWithLifecycleActions;

    /// <summary>
    /// Включает действия перехвата жизненного цикла для типа сущности по флагам.
    /// </summary>
    /// <typeparam name="TEntity">Тип сущности.</typeparam>
    /// <param name="hookType">Тип перехвата.</param>
    /// <param name="actionKeyFlags">Флаги действий.</param>
    /// <returns><see cref="IUnitOfWork"/>.</returns>
    IUnitOfWork EnableLifecycleActions<TEntity>(LifecycleHookType hookType, Enum actionKeyFlags)
        where TEntity : IEntity, IWithLifecycleActions;

    /// <summary>
    /// Сбрасывает настройки действий перехвата жизненного цикла.
    /// </summary>
    /// <returns><see cref="IUnitOfWork"/>.</returns>
    IUnitOfWork ResetLifecycleActionSettings();

    /// <summary>
    /// Очищает отслеживание всех сущностей.
    /// </summary>
    void ClearTracking();
}
