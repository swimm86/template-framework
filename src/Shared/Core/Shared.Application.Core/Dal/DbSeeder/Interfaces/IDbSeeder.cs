// ----------------------------------------------------------------------------------------------
// <copyright file="IDbSeeder.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dal.DbSeeder.Entities;

namespace Shared.Application.Core.Dal.DbSeeder.Interfaces;

/// <summary>
/// Интерфейс для управления <see cref="Seed"/>-ами.
/// </summary>
public interface IDbSeeder
{
    /// <summary>
    /// Применяет все <see cref="Seed"/>-ы.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Результат выполнения асинхронной операции.</returns>
    Task ApplySeedsAsync(CancellationToken cancellationToken = default);
}