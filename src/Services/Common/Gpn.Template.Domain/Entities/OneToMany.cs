// ----------------------------------------------------------------------------------------------
// <copyright file="OneToMany.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Interfaces;

namespace Gpn.Template.Domain.Entities;

/// <summary>
/// Сущность "OneToMany".
/// </summary>
public class OneToMany : IEntity<Guid>
{
    /// <inheritdoc cref="IEntity.Id"/>
    public Guid Id { get; private set; }

    /// <summary>
    /// Имя.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// OneToOne Id
    /// </summary>
    public Guid OneToOneId { get; set; }

    /// <summary>
    /// Ссылка на OneToOne
    /// </summary>
    public OneToOne OneToOne { get; private set; }

    /// <summary>
    /// Создание сущности OneToOne
    /// </summary>
    /// <param name="Name">Имя</param>
    /// <returns>Экземпляр сущности OneToOne</returns>
    public static OneToMany Create(string Name)
    {
        return new OneToMany
        {
            Id = Guid.NewGuid(),
            Name = Name,
        };
    }
}
