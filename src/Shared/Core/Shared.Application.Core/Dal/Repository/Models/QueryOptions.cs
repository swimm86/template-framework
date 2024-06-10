// ----------------------------------------------------------------------------------------------
// <copyright file="QueryOptions.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.Dal.Repository.Models;

/// <summary>
/// Настройки запроса для операций с сущностями.
/// </summary>
/// <typeparam name="TEntity">Тип сущности, для которой реализована спецификация.</typeparam>
public class QueryOptions<TEntity> where TEntity : IEntity
{
    /// <summary>
    /// Фильтры.
    /// </summary>
    public List<Expression<Func<TEntity, bool>>> Filters { get; private set; } = [];

    /// <summary>
    /// Настройки сортировки.
    /// </summary>
    public List<QueryOrderByOption<TEntity>> OrderBy { get; private set; } = [];

    /// <summary>
    /// Включаемые связанные сущности.
    /// </summary>
    public List<string> Includes { get; private set; } = [];

    /// <summary>
    /// Признак необходимости отслеживания изменений сущностей.
    /// </summary>
    public bool WithTracking { get; set; }
}
