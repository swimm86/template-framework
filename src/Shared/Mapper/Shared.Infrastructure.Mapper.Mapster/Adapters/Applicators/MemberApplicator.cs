// ----------------------------------------------------------------------------------------------
// <copyright file="MemberApplicator.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using System.Reflection;
using Mapster;
using Shared.Domain.Core.Mapping.Description.Interfaces;
using Shared.Infrastructure.Mapper.Core.ProfileBuilder.Interfaces;
using Shared.Infrastructure.Mapper.Mapster.Adapters.ExpressionTransformers;
using Shared.Infrastructure.Mapper.Mapster.Adapters.Helpers;
using Shared.Infrastructure.Mapper.Mapster.Utils;

namespace Shared.Infrastructure.Mapper.Mapster.Adapters.Applicators;

/// <summary>
/// Применяет настройки членов (<see cref="IMemberMapDescription"/>) к Mapster-сеттеру.
/// </summary>
internal sealed class MemberApplicator
    : IDescriptionApplicator
{
    private static readonly MethodInfo MapByNameMethod = typeof(TypeAdapterSetterExtensions)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .First(m =>
            m is { Name: nameof(TypeAdapterSetterExtensions.Map), IsGenericMethodDefinition: true }
            && m.GetParameters().Length == 3
            && m.GetParameters()[0].ParameterType.IsGenericParameter
            && m.GetParameters()[1].ParameterType == typeof(string)
            && m.GetParameters()[2].ParameterType.GetGenericTypeDefinition() == typeof(Expression<>)
            && m.GetGenericArguments().Length == 3);

    private static readonly MethodInfo IgnoreMethod = typeof(TypeAdapterSetterExtensions)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .First(m =>
            m is { Name: nameof(TypeAdapterSetterExtensions.Ignore), IsGenericMethodDefinition: true }
            && m.GetParameters().Length == 2
            && m.GetParameters()[0].ParameterType.IsGenericParameter
            && m.GetParameters()[1].ParameterType == typeof(string[]));

    /// <inheritdoc />
    public void Apply(object setter, IMapDescription description)
    {
        if (description.Members.Count == 0)
        {
            return;
        }

        foreach (var member in description.Members)
        {
            if (member.IsIgnored)
            {
                InvokeIgnore(setter, member.DestinationMemberName);
                continue;
            }

            if (member.SourceExpression is null && member.ParameterizedSourceExpression is null)
            {
                continue;
            }

            if (member.ParameterizedSourceExpression is not null)
            {
                var (transformed, keys) = ParameterizedExpressionTransformer.Transform(
                    member.ParameterizedSourceExpression, description.SourceType);
                ParameterKeyRegistry.Register(description.SourceType, description.DestinationType, keys);
                InvokeMapByName(setter, description.SourceType, member.DestinationMemberName, transformed);
            }
            else
            {
                InvokeMapByName(setter, description.SourceType, member.DestinationMemberName, member.SourceExpression!);
            }
        }
    }

    private static void InvokeMapByName(
        object setter,
        Type sourceType,
        string memberName,
        LambdaExpression sourceLambda)
    {
        var sourceMemberType = sourceLambda.ReturnType;
        var funcType = typeof(Func<,>).MakeGenericType(sourceType, sourceMemberType);
        var typedLambda = LambdaTypeConverter.ConvertLambdaType(sourceLambda, funcType);

        var closed = MapByNameMethod.MakeGenericMethod(setter.GetType(), sourceType, sourceMemberType);
        closed.Invoke(null, [setter, memberName, typedLambda]);
    }

    private static void InvokeIgnore(object setter, string memberName)
    {
        var closed = IgnoreMethod.MakeGenericMethod(setter.GetType());
        closed.Invoke(null, [setter, new[] { memberName }]);
    }
}
