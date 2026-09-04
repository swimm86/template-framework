// ----------------------------------------------------------------------------------------------
// <copyright file="MappingExpression.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Domain.Core.Mapping.Description;
using Shared.Domain.Core.Mapping.Expression.Interfaces;
using Shared.Domain.Core.Mapping.Interfaces;

namespace Shared.Domain.Core.Mapping.Expression;

/// <summary>
/// Внутренняя реализация <see cref="IMappingExpression{TSource, TDestination}"/>.
/// </summary>
/// <typeparam name="TSource">Исходный тип.</typeparam>
/// <typeparam name="TDestination">Целевой тип.</typeparam>
/// <param name="description">Описание преобразования (mapping), заполняемое fluent-методами.</param>
/// <param name="owner">Профиль, в котором зарегистрировано преобразование (mapping).</param>
internal sealed class MappingExpression<TSource, TDestination>(
    MapDescription<TSource, TDestination> description,
    MappingProfileBase owner)
    : IMappingExpression<TSource, TDestination>
{
    /// <inheritdoc />
    public IMappingExpression<TSource, TDestination> ForMember<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember,
        Action<IMemberConfigurationExpression<TSource, TDestination, TMember>> memberOptions)
    {
        var memberName = GetMemberName(destinationMember);
        var memberType = typeof(TMember);
        var memberDesc = new MemberMapDescription(memberName, memberType);
        var expr = new MemberConfigurationExpression<TSource, TDestination, TMember>(memberDesc);
        memberOptions(expr);
        description.AddMember(memberDesc);
        return this;
    }

    /// <inheritdoc />
    public IMappingExpression<TSource, TDestination> ForCtorParam(
        string ctorParamName,
        Action<ICtorParamConfigurationExpression<TSource>> paramOptions)
    {
        var paramDesc = new CtorParamMapDescription(ctorParamName);
        var expr = new CtorParamConfigurationExpression<TSource>(paramDesc);
        paramOptions(expr);
        description.AddCtorParam(paramDesc);
        return this;
    }

    /// <inheritdoc />
    public IMappingExpression<TSource, TDestination> AfterMap(
        Action<TSource, TDestination> afterMapAction)
    {
        description.AfterMap = (src, dest) => afterMapAction((TSource)src, (TDestination)dest);
        return this;
    }

    /// <inheritdoc />
    public IMappingExpression<TSource, TDestination> BeforeMap(
        Action<TSource, TDestination> beforeMapAction)
    {
        description.BeforeMap = (src, dest) => beforeMapAction((TSource)src, (TDestination)dest);
        return this;
    }

    /// <inheritdoc />
    public IMappingExpression<TSource, TDestination> ConstructUsing(
        Func<TSource, TDestination> ctor)
    {
        description.ConstructUsing = (src, _) => ctor((TSource)src)!;
        return this;
    }

    /// <inheritdoc />
    public IMappingExpression<TSource, TDestination> ConstructUsing(
        Func<TSource, ResolutionContext, TDestination> ctor)
    {
        description.ConstructUsing = (src, ctx) => ctor((TSource)src, ctx)!;
        return this;
    }

    /// <inheritdoc />
    public IMappingExpression<TSource, TDestination> ConvertUsing(
        ITypeConverter<TSource, TDestination> converter)
    {
        description.Converter = converter;
        return this;
    }

    /// <inheritdoc />
    public IMappingExpression<TSource, TDestination> ConvertUsing(
        Func<TSource, TDestination> mappingFunction)
    {
        description.ConvertUsingFunction = src => mappingFunction((TSource)src)!;
        return this;
    }

    /// <inheritdoc />
    public IMappingExpression<TSource, TDestination> ConvertUsing(
        Func<TSource, TDestination, ResolutionContext, TDestination> mappingFunction)
    {
        description.ConvertUsingFunctionWithDest = (src, dest, ctx) =>
            mappingFunction((TSource)src, (TDestination)dest, ctx)!;
        return this;
    }

    /// <inheritdoc />
    public IMappingExpression<TDestination, TSource> ReverseMap()
    {
        description.HasReverseMap = true;
        var reverseDesc = new MapDescription<TDestination, TSource>();
        owner.RegisterDescription(reverseDesc);
        return new MappingExpression<TDestination, TSource>(reverseDesc, owner);
    }

    /// <inheritdoc />
    public IMappingExpression<TSource, TDestination> IncludeBase<TSourceBase, TDestBase>()
    {
        description.IncludedBasesList.Add((typeof(TSourceBase), typeof(TDestBase)));
        return this;
    }

    private static string GetMemberName<TMember>(Expression<Func<TDestination, TMember>> expr)
    {
        if (expr.Body is MemberExpression member)
        {
            return member.Member.Name;
        }

        if (expr.Body is UnaryExpression { Operand: MemberExpression member2 })
        {
            return member2.Member.Name;
        }

        throw new ArgumentException(
            "Cannot extract destination member name from expression. Expected simple member access.",
            nameof(expr));
    }
}
