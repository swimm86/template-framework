// ----------------------------------------------------------------------------------------------
// <copyright file="MemberConfigurationExpression.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Domain.Core.Mapping.Description;
using Shared.Domain.Core.Mapping.Expression.Interfaces;

namespace Shared.Domain.Core.Mapping.Expression;

/// <summary>
/// Реализация <see cref="IMemberConfigurationExpression{TSource, TDestination, TMember}"/>.
/// </summary>
/// <typeparam name="TSource"><inheritdoc cref="IMemberConfigurationExpression{TSource, TDestination, TMember}" path="/typeparam[@name='TSource']"/></typeparam>
/// <typeparam name="TDestination"><inheritdoc cref="IMemberConfigurationExpression{TSource, TDestination, TMember}" path="/typeparam[@name='TDestination']"/></typeparam>
/// <typeparam name="TMember"><inheritdoc cref="IMemberConfigurationExpression{TSource, TDestination, TMember}" path="/typeparam[@name='TMember']"/></typeparam>
/// <param name="description">Заполняемое описание настройки члена.</param>
internal sealed class MemberConfigurationExpression<TSource, TDestination, TMember>(
    MemberMapDescription description)
    : IMemberConfigurationExpression<TSource, TDestination, TMember>
{
    /// <inheritdoc />
    public void MapFrom<TSourceMember>(
        Expression<Func<TSource, TSourceMember>> sourceMember)
    {
        description.SourceExpression = sourceMember;
    }

    /// <inheritdoc />
    public void MapFrom<TSourceMember>(
        Expression<Func<TSource, IDictionary<string, object?>, TSourceMember>> sourceMember)
    {
        description.ParameterizedSourceExpression = sourceMember;
    }

    /// <inheritdoc />
    public void Ignore()
    {
        description.IsIgnored = true;
    }
}
