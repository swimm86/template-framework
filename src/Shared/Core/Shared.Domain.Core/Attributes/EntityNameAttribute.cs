// ----------------------------------------------------------------------------------------------
// <copyright file="EntityNameAttribute.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Attributes;

/// <summary>
/// Атрибут названия сущности.
/// </summary>
/// <param name="name">Название сущности.</param>
[AttributeUsage(AttributeTargets.Class)]
public class EntityNameAttribute(string name) : Attribute
{
    /// <summary>
    /// Название сущности.
    /// </summary>
    public string Name => name;
}
