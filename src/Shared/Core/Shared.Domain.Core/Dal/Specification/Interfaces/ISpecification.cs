// ----------------------------------------------------------------------------------------------
// <copyright file="ISpecification.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Dal.Specification.Interfaces;

/// <summary>
/// Интерфейс для спецификации.
/// </summary>
/// <typeparam name="TEntity"> Тип сущности.</typeparam>
public interface ISpecification<TEntity>
    where TEntity : IEntity
{
    /// <summary>
    /// Собирает настройки для спецификации.
    /// </summary>
    /// <returns>Настройки спецификации.</returns>
    public QueryOptions<TEntity> BuildOptions();
}
