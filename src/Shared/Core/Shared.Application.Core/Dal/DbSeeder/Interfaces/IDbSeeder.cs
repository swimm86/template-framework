// ----------------------------------------------------------------------------------------------
// <copyright file="IDbSeeder.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Dal.DbSeeder.Interfaces;

/// <summary>
/// Интерфейс сервиса управления seed-процессами.
/// </summary>
public interface IDbSeeder
{
    /// <summary>
    /// Применяет все зарегистрированные seed-процессы.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    Task ApplySeedsAsync(CancellationToken cancellationToken = default);
}