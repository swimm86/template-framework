// ----------------------------------------------------------------------------------------------
// <copyright file="IIncludable.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;

namespace Shared.Domain.Core.Dal.Repository.Interfaces;

/// <summary>
/// Кастомное представление Include.
/// </summary>
/// <typeparam name="TSrcEntity">Сущность, для которой осуществляется Include.</typeparam>
public interface IIncludable<TSrcEntity>
{
    /// <summary>
    /// Выражение Include.
    /// </summary>
    LambdaExpression Expression { get; }

    /// <summary>
    /// Последующий Include.
    /// </summary>
    IIncludable<TSrcEntity>? Child { get; }

    /// <summary>
    /// Установление последующего Include.
    /// </summary>
    /// <param name="includable">Include для вложения.</param>
    void SetChild(IIncludable<TSrcEntity> includable);
}
