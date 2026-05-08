// ----------------------------------------------------------------------------------------------
// <copyright file="Person.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Interfaces;

namespace Template.Domain.Entities;

/// <summary>
/// Сущность "Person".
/// </summary>
public class Person : IEntity<Guid>
{
    /// <inheritdoc cref="IEntity.Id"/>
    public Guid Id { get; private set; }

    /// <summary>
    /// Имя.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Адрес электронной почты.
    /// </summary>
    public string Email { get; private set; }

    /// <summary>
    /// Создание сущности "Person".
    /// </summary>
    /// <param name="name">Имя.</param>
    /// <param name="email">Адрес электронной почты.</param>
    /// <returns>Экземпляр сущности "Person".</returns>
    public static Person Create(
        string name,
        string email)
    {
        return new Person
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
        };
    }
}
