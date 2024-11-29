// ----------------------------------------------------------------------------------------------
// <copyright file="EntityEventBase.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Base;

/// <summary>
/// Базовый класс событий при создании сущности, реализующей интерфейс <see cref="IEntity"/>.
/// </summary>
/// <typeparam name="TEntity"> Сущность, реализующая интерфейс <see cref="IEntity"/>. </typeparam>
public abstract class EntityEventBase<TEntity>(TEntity entity) : IDomainEvent
    where TEntity : IEntity
{
    /// <summary>
    /// Экземпляр сущности, реализующей интерфейс <see cref="IEntity"/>.
    /// </summary>
    public TEntity Entity => entity;
}
