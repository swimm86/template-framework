// ----------------------------------------------------------------------------------------------
// <copyright file="ISeed.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
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