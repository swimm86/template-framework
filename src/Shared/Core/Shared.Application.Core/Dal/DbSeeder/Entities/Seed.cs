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
    : IEntity<Guid>
{
    /// <inheritdoc />
    public Guid Id { get; private init; }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="Seed"/>.
    /// </summary>
    private Seed()
    {
    }

    /// <summary>
    /// Наименование seed-процесса.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Создает новый экземпляр <see cref="Seed"/>.
    /// </summary>
    /// <param name="name">Наименование seed-процесса.</param>
    /// <returns>Экземпляр <see cref="Seed"/>.</returns>
    public static Seed Create(string name)
    {
        return new Seed
        {
            Id = Guid.NewGuid(),
            Name = name,
        };
    }
}