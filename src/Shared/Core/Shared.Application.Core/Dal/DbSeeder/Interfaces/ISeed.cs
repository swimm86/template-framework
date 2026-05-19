// ----------------------------------------------------------------------------------------------
// <copyright file="ISeed.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Dal.DbSeeder.Interfaces;

/// <summary>
/// Интерфейс для классов выполняющих seed-процессы.
/// </summary>
public interface ISeed
{
    /// <summary>
    /// Выполняет seed-процесс.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    Task SeedAsync();
}