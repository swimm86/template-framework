// ----------------------------------------------------------------------------------------------
// <copyright file="RepositoryOrderByOption.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.Dal.Repository.Models;

/// <summary>
/// Опции сортировки для запроса.
/// </summary>
/// <typeparam name="TEntity">Тип сущности <see cref="IEntity"/>.</typeparam>
/// <param name="Expression">Выражение с условием сортировки.</param>
/// <param name="Direction">Направление сортировки.</param>
public record QueryOrderByOption<TEntity>(
    Expression<Func<TEntity, object>> Expression,
    OrderDirectionType Direction);
