// ----------------------------------------------------------------------------------------------
// <copyright file="IBeforeSaveChangesService.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Infrastructure.Dal.EFCore.Interfaces;

/// <summary>
/// Сервис для выполнения действий перед сохранением изменений в БД.
/// </summary>
public interface IBeforeSaveChangesService
{
    /// <summary>
    /// Асинхронно выполняет действия перед сохранением изменений в БД.
    /// </summary>
    /// <param name="dbContext"><see cref="DbContext"/>.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат асинхронного выполнения.</returns>
    Task ProcessAsync(
        DbContext dbContext,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Выполняет действия перед сохранением изменений в БД.
    /// </summary>
    /// <param name="dbContext"><see cref="DbContext"/>.</param>
    void Process(DbContext dbContext);
}
