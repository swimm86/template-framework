// ----------------------------------------------------------------------------------------------
// <copyright file="QueryOrderByOption.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Dal.Repository.Models;

/// <summary>
/// Опции сортировки для запроса.
/// </summary>
/// <typeparam name="TEntity">Тип сущности <see cref="IEntity"/>.</typeparam>
/// <param name="Expression">Выражение с условием сортировки.</param>
/// <param name="Direction">Направление сортировки.</param>
public record QueryOrderByOption<TEntity>(
    Expression<Func<TEntity, object>> Expression,
    OrderDirectionType Direction);
