// ----------------------------------------------------------------------------------------------
// <copyright file="SeedAttribute.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Dal.DbSeeder.Attributes;

/// <summary>
/// Атрибут seed-а.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class SeedAttribute(string name, int order) : Attribute
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
