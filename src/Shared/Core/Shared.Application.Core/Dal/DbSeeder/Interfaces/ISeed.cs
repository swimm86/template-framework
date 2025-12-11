// ----------------------------------------------------------------------------------------------
// <copyright file="ISeed.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Dal.DbSeeder.Interfaces;

/// <summary>
/// Интерфейс для seed-ов.
/// </summary>
public interface ISeed
{
    /// <summary>
    /// Реализуеты seed-процесс.
    /// </summary>
    /// <returns>Результат выполнения асинхронной операции.</returns>
    Task SeedAsync();
}