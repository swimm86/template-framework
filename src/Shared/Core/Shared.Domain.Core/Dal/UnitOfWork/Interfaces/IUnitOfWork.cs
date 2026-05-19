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
    /// <param name="resetEventSettingsAfterSave">Сбросить настройки доменных событий после сохранения.</param>
    /// <returns>Код результата.</returns>
    int SaveChanges(
        bool commitTransaction = true,
        bool resetEventSettingsAfterSave = true);

    /// <summary>
    /// Асинхронно сохраняет изменения.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <param name="commitTransaction">Признак того, что необходимо закоммитить транзакцию.</param>
    /// <param name="resetEventSettingsAfterSave">Сбросить настройки доменных событий после сохранения.</param>
    /// <returns>Код результата.</returns>
    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default,
        bool commitTransaction = true,
        bool resetEventSettingsAfterSave = true);

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
    /// Отключает доменные события.
    /// </summary>
    /// <returns><see cref="IUnitOfWork"/>.</returns>
    IUnitOfWork DisableEvents();

    /// <summary>
    /// Включает доменные события.
    /// </summary>
    /// <returns><see cref="IUnitOfWork"/>.</returns>
    IUnitOfWork EnableEvents();

    /// <summary>
    /// Отключает доменные события.
    /// </summary>
    /// <typeparam name="TEntity">Тип сущности.</typeparam>
    /// <param name="eventType">Тип события (если <see langword="null"/>, то отключаются события для всего типа).</param>
    /// <returns><see cref="IUnitOfWork"/>.</returns>
    IUnitOfWork DisableEvents<TEntity>(DomainEventType? eventType = default)
        where TEntity : IEntity, IWithDomainEvents;

    /// <summary>
    /// Включает доменные события.
    /// </summary>
    /// <typeparam name="TEntity">Тип сущности.</typeparam>
    /// <param name="eventType">Тип события (если <see langword="null"/>, то отключаются события для всего типа).</param>
    /// <returns><see cref="IUnitOfWork"/>.</returns>
    IUnitOfWork EnableEvents<TEntity>(DomainEventType? eventType = default)
        where TEntity : IEntity, IWithDomainEvents;

    /// <summary>
    /// Отключает доменные события.
    /// </summary>
    /// <typeparam name="TEntity">Тип сущности.</typeparam>
    /// <param name="eventType">Тип события.</param>
    /// <param name="eventKeyFlags">Флаги событий.</param>
    /// <returns><see cref="IUnitOfWork"/>.</returns>
    IUnitOfWork DisableEvents<TEntity>(DomainEventType eventType, Enum eventKeyFlags)
        where TEntity : IEntity, IWithDomainEvents;

    /// <summary>
    /// Включает доменные события.
    /// </summary>
    /// <typeparam name="TEntity">Тип сущности.</typeparam>
    /// <param name="eventType">Тип события.</param>
    /// <param name="eventKeyFlags">Флаги событий.</param>
    /// <returns><see cref="IUnitOfWork"/>.</returns>
    IUnitOfWork EnableEvents<TEntity>(DomainEventType eventType, Enum eventKeyFlags)
        where TEntity : IEntity, IWithDomainEvents;

    /// <summary>
    /// Сбрасывает настройки доменных событий.
    /// </summary>
    /// <returns><see cref="IUnitOfWork"/>.</returns>
    IUnitOfWork ResetEventSettings();

    /// <summary>
    /// Очищает отслеживание всех сущностей.
    /// </summary>
    void ClearTracking();
}
