// ----------------------------------------------------------------------------------------------
// <copyright file="CtorParamConfigurationExpression.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Domain.Core.Mapping.Description;
using Shared.Domain.Core.Mapping.Expression.Interfaces;

namespace Shared.Domain.Core.Mapping.Expression;

/// <summary>
/// Реализация <see cref="ICtorParamConfigurationExpression{TSource}"/>.
/// </summary>
/// <typeparam name="TSource">Исходный тип.</typeparam>
/// <param name="description">Заполняемое описание настройки параметра конструктора.</param>
internal sealed class CtorParamConfigurationExpression<TSource>(
    CtorParamMapDescription description)
    : ICtorParamConfigurationExpression<TSource>
{
    /// <inheritdoc />
    public void MapFrom<TSourceMember>(
        Expression<Func<TSource, TSourceMember>> sourceMember)
    {
        description.SourceExpression = sourceMember;
    }
}