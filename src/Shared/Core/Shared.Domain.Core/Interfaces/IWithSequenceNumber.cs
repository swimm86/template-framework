// ----------------------------------------------------------------------------------------------
// <copyright file="IWithSequenceNumber.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;

namespace Shared.Domain.Core.Interfaces;

/// <summary>
/// Интерфейс последовательности у сущности.
/// </summary>
/// <typeparam name="TEntity">.</typeparam>
public interface IWithSequenceNumber<TEntity>
{
    /// <summary>
    /// Порядковый номер.
    /// </summary>
    int? SequenceNumber { get; set; }

    /// <summary>
    /// Выражение фильтра.
    /// </summary>
    Expression<Func<TEntity, bool>> FilterExpression { get; }
}
