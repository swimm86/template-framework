// ----------------------------------------------------------------------------------------------
// <copyright file="PersonWork.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Interfaces;

namespace Gpn.Template.Domain.Entities;

/// <summary>
/// Сущность "PersonWork".
/// </summary>
public class PersonWork : IEntity<Guid>
{
    /// <inheritdoc cref="IEntity.Id"/>
    public Guid Id { get; private set; }

    /// <summary>
    /// Id человека
    /// </summary>
    public Guid PersonId { get; private set; }

    /// <summary>
    /// Id рабыоты
    /// </summary>
    public Guid WorkId { get; private set; }

    /// <summary>
    /// Ссылка на человека
    /// </summary>
    public Person Person { get; private set; }

    /// <summary>
    /// Ссылка на работу
    /// </summary>
    public Work Work { get; private set; }

    /// <summary>
    /// Создание сущности "PersonWork".
    /// </summary>
    /// <param name="personId">Id человека</param>
    /// <param name="workId">Id работы</param>
    /// <param name="person">Человек</param>
    /// <param name="work">Работы</param>
    /// <returns>Экземпляр сущности "PersonWork".</returns>
    public static PersonWork Create(Guid personId, Guid workId, Person person, Work work)
    {
        return new PersonWork
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            WorkId = workId,
            Person = person,
            Work = work
        };
    }
}
