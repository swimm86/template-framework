// ----------------------------------------------------------------------------------------------
// <copyright file="SeedAttribute.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Dal.DbSeeder.Attributes;

/// <summary>
/// Атрибут для пометки классов первичного заполнения данных (seed).
/// </summary>
/// <param name="name">Наименование seed-процесса.</param>
/// <param name="order">Порядок выполнения.</param>
[AttributeUsage(AttributeTargets.Class)]
public class SeedAttribute(string name, int order)
    : Attribute
{
    /// <inheritdoc cref="SeedAttribute" select="param[@name='name']"/>
    public string Name { get; private set; } = name;

    /// <inheritdoc cref="SeedAttribute" select="param[@name='order']"/>
    public int Order { get; private set; } = order;
}