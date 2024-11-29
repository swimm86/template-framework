// ----------------------------------------------------------------------------------------------
// <copyright file="ControllerTypeAttribute.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Presentation.Core.Attributes;

/// <summary>
/// Указывает тип контроллера для использования в инфраструктуре приложения.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ControllerTypeAttribute(string name) : Attribute
{
    /// <summary>
    /// Получает имя, связанное с типом контроллера.
    /// </summary>
    public readonly string Name = name;
}
