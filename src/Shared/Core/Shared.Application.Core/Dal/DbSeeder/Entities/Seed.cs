// ----------------------------------------------------------------------------------------------
// <copyright file="Seed.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Dal.DbSeeder.Entities;

/// <summary>
/// Сущность "Seed".
/// </summary>
public class Seed
{
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
        return new Seed { Name = name };
    }
}
