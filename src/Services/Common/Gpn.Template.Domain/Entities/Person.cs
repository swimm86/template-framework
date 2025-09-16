// ----------------------------------------------------------------------------------------------
// <copyright file="Person.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Domain.Core.Interfaces;

namespace Gpn.Template.Domain.Entities;

/// <summary>
/// Сущность "Person".
/// </summary>
public class Person : IEntity<Guid>, IWithSequenceNumber<Person>
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
    /// Для теста.
    /// </summary>
    public int SomeKey { get; private set; }

    /// <inheritdoc/>
    public int? SequenceNumber { get; set; }

    /// <inheritdoc/>
    public Expression<Func<Person, bool>> FilterExpression => param => param.SomeKey == SomeKey;

    /// <summary>
    /// Создание сущности "Person".
    /// </summary>
    /// <param name="name">Имя.</param>
    /// <param name="email">Адрес электронной почты.</param>
    /// <param name="someKey">.</param>
    /// <returns>Экземпляр сущности "Person".</returns>
    public static Person Create(
        string name,
        string email,
        int someKey)
    {
        return new Person
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            SomeKey = someKey
        };
    }
}
