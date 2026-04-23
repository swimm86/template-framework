// ----------------------------------------------------------------------------------------------
// <copyright file="Seed.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.Dal.DbSeeder.Entities;

/// <summary>
/// Сущность "Seed".
/// </summary>
public class Seed
    : IEntity<Guid>
{
    /// <inheritdoc />
    public Guid Id { get; private init; }

    /// <summary>
    /// Конструктор класса <see cref="Seed"/>.
    /// </summary>
    private Seed()
    {
    }

    /// <summary>
    /// Наименование.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Создает экземпляр класса <see cref="Seed"/>.
    /// </summary>
    /// <param name="name">Наименование.</param>
    /// <returns>Экземпляр класса <see cref="Seed"/>.</returns>
    public static Seed Create(string name)
    {
        return new Seed
        {
            Id = Guid.NewGuid(),
            Name = name,
        };
    }
}