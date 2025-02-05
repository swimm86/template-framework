// ----------------------------------------------------------------------------------------------
// <copyright file="Work.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Interfaces;

namespace Gpn.Template.Domain.Entities;

/// <summary>
/// Сущность "Work".
/// </summary>
public class Work : IEntity<Guid>
{
    /// <inheritdoc cref="IEntity.Id"/>
    public Guid Id { get; private set; }

    /// <summary>
    /// Имя.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Ссылки на PersonWorks
    /// </summary>
    public List<PersonWork> PersonWorks { get; private set; } = [];

    /// <summary>
    /// Создание сущности "Work".
    /// </summary>
    /// <param name="name">Название работы</param>
    /// <returns>Экземпляр сущности "Work".</returns>
    public static Work Create(string name)
    {
        return new Work { Id = Guid.NewGuid(), Name = name };
    }
}
