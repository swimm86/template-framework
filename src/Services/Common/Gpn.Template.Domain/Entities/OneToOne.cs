// ----------------------------------------------------------------------------------------------
// <copyright file="OneToOne.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Interfaces;

namespace Gpn.Template.Domain.Entities;

/// <summary>
/// Сущность "OneToOne".
/// </summary>
public class OneToOne : IEntity<Guid>
{
    /// <inheritdoc cref="IEntity.Id"/>
    public Guid Id { get; private set; }

    /// <summary>
    /// Имя.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Person Id
    /// </summary>
    public Guid PersonId { get; set; }

    /// <summary>
    /// Ссылка на Person
    /// </summary>
    public Person Person { get; private set; }

    /// <summary>
    /// Ссылки на OneToOne
    /// </summary>
    public List<OneToMany> OneToManies { get; private set; }

    /// <summary>
    /// Создание сущности OneToOne
    /// </summary>
    /// <param name="name">Имя</param>
    /// <param name="oneToManies">.</param>
    /// <returns>Экземпляр сущности OneToOne</returns>
    public static OneToOne Create(string name, List<OneToMany> oneToManies)
    {
        return new OneToOne
        {
            Id = Guid.NewGuid(),
            Name = name,
            OneToManies = oneToManies,
        };
    }
}
