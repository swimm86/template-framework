// ----------------------------------------------------------------------------------------------
// <copyright file="EntityComparableNameAttribute.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Attributes;

/// <summary>
/// Атрибут названия параметра сущности для сравнения.
/// </summary>
/// <param name="name">Название сущности.</param>
[AttributeUsage(AttributeTargets.Class)]
public class EntityComparableNameAttribute(string name) : Attribute
{
    /// <summary>
    /// Название параметра для сравнения по умолчанию.
    /// </summary>
    public const string DefaultComparableParameterName = "наименованием";

    /// <summary>
    /// Название параметра для сравнения.
    /// </summary>
    public string Name => name;
}
