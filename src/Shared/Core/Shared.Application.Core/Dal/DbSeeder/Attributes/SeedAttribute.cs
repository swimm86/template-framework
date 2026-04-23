// ----------------------------------------------------------------------------------------------
// <copyright file="SeedAttribute.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Dal.DbSeeder.Attributes;

/// <summary>
/// Атрибут seed-а.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class SeedAttribute(string name, int order)
    : Attribute
{
    /// <summary>
    /// Наименование.
    /// </summary>
    public string Name { get; private set; } = name;

    /// <summary>
    /// Порядок.
    /// </summary>
    public int Order { get; private set; } = order;
}