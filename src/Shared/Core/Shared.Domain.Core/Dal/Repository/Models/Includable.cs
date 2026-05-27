// ----------------------------------------------------------------------------------------------
// <copyright file="Includable.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Domain.Core.Dal.Repository.Interfaces;

namespace Shared.Domain.Core.Dal.Repository.Models;

/// <summary>
/// Реализация кастомного Include.
/// </summary>
/// <typeparam name="TSrcEntity">Сущность, для которой осуществляется Include.</typeparam>
/// <typeparam name="TDstEntity">Сущность, в которую проецирует Include.</typeparam>
/// <param name="expression">Выражение.</param>
public class Includable<TSrcEntity, TDstEntity>(
    LambdaExpression expression)
    : IIncludable<TSrcEntity>
{
    /// <inheritdoc />
    public LambdaExpression Expression { get; } = expression;

    /// <inheritdoc />
    public IIncludable<TSrcEntity>? Child { get; private set; }

    /// <inheritdoc />
    public void SetChild(IIncludable<TSrcEntity> includable)
    {
        Child = includable;
    }
}
