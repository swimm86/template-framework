// ----------------------------------------------------------------------------------------------
// <copyright file="IncludeNode.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;

namespace Shared.Domain.Core.Dal.Repository.Models;

/// <summary>
/// Include узел
/// </summary>
/// <param name="expression">Навигационные свойства</param>
/// <param name="sourceType">Тип источника</param>
/// <param name="destType">Тип параметра</param>
/// <param name="previousType">Тип предыдущей сущности</param>
public class IncludeNode(LambdaExpression expression, Type sourceType, Type destType, Type? previousType = null)
{
    /// <summary>
    /// Навигационные свойства
    /// </summary>
    public LambdaExpression Expression { get; set; } = expression;

    /// <summary>
    /// Тип предыдущей сущности
    /// </summary>
    public Type? PreviousType { get; set; } = previousType;

    /// <summary>
    /// Тип источника
    /// </summary>
    public Type SourceType { get; set; } = sourceType;

    /// <summary>
    /// Тип параметра
    /// </summary>
    public Type DestinationType { get; set; } = destType;
}
