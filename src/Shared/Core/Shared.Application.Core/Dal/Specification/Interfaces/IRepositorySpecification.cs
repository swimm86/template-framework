// ----------------------------------------------------------------------------------------------
// <copyright file="IRepositorySpecification.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dal.Repository.Models;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.Dal.Specification.Interfaces;

/// <summary>
/// Интерфейс для спецификации.
/// </summary>
/// <typeparam name="TEntity"> Тип сущности.</typeparam>
public interface IRepositorySpecification<TEntity> where TEntity : IEntity
{
    /// <summary>
    /// Собирает настройки для спецификации.
    /// </summary>
    /// <returns>Настройки спецификации.</returns>
    public QueryOptions<TEntity> BuildOptions();
}
