// ----------------------------------------------------------------------------------------------
// <copyright file="IDbSeeder.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Dal.DbSeeder.Interfaces;

/// <summary>
/// Интерфейс для классов, которые выполняют инициализацию и миграцию базы данных.
/// </summary>
public interface IDbSeeder
{
    /// <summary>
    /// Создает базу данных, если она еще не существует.
    /// </summary>
    void CreateDbIfNotExists();

    /// <summary>
    /// Выполняет миграцию базы данных до последней версии.
    /// </summary>
    void Migrate();
}
