// ----------------------------------------------------------------------------------------------
// <copyright file="Seed.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.Dal.DbSeeder.Entities;

/// <summary>
/// Сущность для отслеживания выполненных seed-процессов.
/// </summary>
public class Seed
    : IEntity<string>
{
    /// <summary>
    /// Наименование seed-процесса.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="Seed"/>.
    /// </summary>
    private Seed()
    {
    }

    /// <summary>
    /// Создает новый экземпляр <see cref="Seed"/>.
    /// </summary>
    /// <param name="name">Наименование seed-процесса.</param>
    /// <returns>Экземпляр <see cref="Seed"/>.</returns>
    public static Seed Create(string name)
    {
        return new Seed
        {
            Id = name,
        };
    }
}