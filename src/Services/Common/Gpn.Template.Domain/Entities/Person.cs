// ----------------------------------------------------------------------------------------------
// <copyright file="Person.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Interfaces;

namespace Gpn.Template.Domain.Entities;

/// <summary>
/// Класс сущности "Person".
/// </summary>
public class Person : IEntity<Guid>
{
    /// <inheritdoc cref="IEntity.Id"/>
    public Guid Id { get; set; }

    /// <summary>
    /// Имя.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Адрес электронной почты.
    /// </summary>
    public string Email { get; set; }
}
