// ----------------------------------------------------------------------------------------------
// <copyright file="IDbSeeder.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Dal.DbSeeder.Interfaces;

/// <summary>
/// Интерфейс для классов, которые выполняют инициализацию и миграцию базы данных.
/// </summary>
public interface IDbSeeder
{
    /// <summary>
    /// Выполняет миграцию базы данных до последней версии.
    /// </summary>
    void Migrate();

    /// <summary>
    /// Выполняет инициализацию базы данных.
    /// </summary>
    void Initialize();
}
