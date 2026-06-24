// ----------------------------------------------------------------------------------------------
// <copyright file="ISetterRepository.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Dal.Repository.Interfaces;

/// <summary>
/// Интерфейс репозитория модификации данных для сущности <typeparamref name="TEntity"/>.
/// </summary>
/// <typeparam name="TEntity">Тип сущности.</typeparam>
public partial interface ISetterRepository<TEntity>
    where TEntity : class, IEntity
{
    /// <summary>
    /// Выполняет синхронную операцию в контексте репозитория с возвращением результата.
    /// </summary>
    /// <typeparam name="TResult">Тип возвращаемого значения.</typeparam>
    /// <param name="process">Делегат операции для выполнения.</param>
    /// <param name="useTransaction">Выполнять операцию в рамках транзакции.</param>
    /// <returns>Результат выполнения операции.</returns>
    TResult Execute<TResult>(
        Func<TResult> process,
        bool useTransaction = false);

    /// <summary>
    /// Выполняет синхронную операцию в контексте репозитория.
    /// </summary>
    /// <inheritdoc cref="Execute{TResult}"/>
    /// <param name="process"/><param name="useTransaction"/>
    void Execute(
        Action process,
        bool useTransaction = false) =>
        Execute<object?>(() =>
        {
            process();
            return null;
        });

    /// <summary>
    /// Выполняет асинхронную операцию в контексте репозитория с возвращением результата.
    /// </summary>
    /// <typeparam name="TResult">Тип возвращаемого значения.</typeparam>
    /// <param name="process">Асинхронный делегат операции для выполнения.</param>
    /// <param name="useTransaction">Выполнять операцию в рамках транзакции.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Задача выполнения операции.</returns>
    Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> process,
        bool useTransaction = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Выполняет асинхронную операцию в контексте репозитория.
    /// </summary>
    /// <inheritdoc cref="ExecuteAsync{TResult}"/>
    /// <param name="process"/><param name="useTransaction"/><param name="cancellationToken"/>
    Task ExecuteAsync(
        Func<Task> process,
        bool useTransaction = false,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync<object?>(
            async () =>
            {
                await process();
                return null;
            },
            useTransaction,
            cancellationToken);

    /// <summary>
    /// Синхронно сохраняет все изменения, внесённые в контекст репозитория.
    /// </summary>
    void SaveChanges();

    /// <summary>
    /// Асинхронно сохраняет все изменения, внесённые в контекст репозитория.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Задача выполнения операции сохранения.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
