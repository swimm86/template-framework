// ----------------------------------------------------------------------------------------------
// <copyright file="IDbUpdater.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Dal.DbUpdater.Interfaces;

/// <summary>
/// Интерфейс для классов, которые выполняют инициализацию и миграцию базы данных.
/// </summary>
public interface IDbUpdater
{
    /// <summary>
    /// Создает базу данных, если она еще не существует.
    /// </summary>
    void CreateDbIfNotExists();

    /// <summary>
    /// Выполняет миграцию базы данных до последней версии.
    /// </summary>
    void Migrate();

    /// <summary>
    /// Выполняет инициализацию базы данных.
    /// </summary>
    void Initialize();
}
