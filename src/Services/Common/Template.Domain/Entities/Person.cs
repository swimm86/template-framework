// ----------------------------------------------------------------------------------------------
// <copyright file="Person.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Interfaces;

namespace Template.Domain.Entities;

/// <summary>
/// Сущность "Персона".
/// </summary>
public class Person
    : IEntity<Guid>
{
    /// <inheritdoc cref="IEntity.Id"/>
    public Guid Id { get; init; }

    /// <summary>
    /// Имя.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Адрес электронной почты.
    /// </summary>
    public string Email { get; private set; }

    /// <summary>
    /// Создает сущность "Персона".
    /// </summary>
    /// <param name="name"><inheritdoc cref="Domain.Entities.Person.Name" path="/summary"/></param>
    /// <param name="email"><inheritdoc cref="Domain.Entities.Person.Email" path="/summary"/></param>
    /// <returns>Экземпляр сущности "Персона".</returns>
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
