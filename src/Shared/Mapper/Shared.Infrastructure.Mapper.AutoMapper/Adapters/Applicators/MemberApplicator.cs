// ----------------------------------------------------------------------------------------------
// <copyright file="MemberApplicator.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using System.Reflection;
using Shared.Domain.Core.Mapping.Description.Interfaces;
using Shared.Infrastructure.Mapper.AutoMapper.Adapters.ExpressionTransformers;
using Shared.Infrastructure.Mapper.AutoMapper.Adapters.Helpers;
using Shared.Infrastructure.Mapper.Core.ProfileBuilder.Interfaces;

namespace Shared.Infrastructure.Mapper.AutoMapper.Adapters.Applicators;

/// <summary>
/// Применяет настройки членов (<see cref="IMemberMapDescription"/>) к AutoMapper-выражению.
/// </summary>
internal sealed class MemberApplicator
    : IDescriptionApplicator
{
    /// <inheritdoc />
    public void Apply(object mapExpr, IMapDescription description)
    {
        if (description.Members.Count == 0)
        {
            return;
        }

        var amMapType = typeof(global::AutoMapper.IMappingExpression<,>).MakeGenericType(
            description.SourceType, description.DestinationType);
        var forMemberMethod = LambdaTypeConverter.FindMethodsOnHierarchy(
            amMapType,
            nameof(global::AutoMapper.IMappingExpression<object, object>.ForMember))
            .First(m =>
                m.GetParameters().Length == 2
                && m.GetParameters()[1].ParameterType.IsGenericType
                && m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Action<>));

        foreach (var member in description.Members)
        {
            var memberType = member.DestinationMemberType;
            var destLambdaType = typeof(Expression<>).MakeGenericType(
                typeof(Func<,>).MakeGenericType(description.DestinationType, memberType));

            var destLambda = BuildDestinationAccess(description.DestinationType, member.DestinationMemberName);
            var typedDestLambda = LambdaTypeConverter.ConvertLambdaType(destLambda, destLambdaType);

            var memberActionType = typeof(Action<>).MakeGenericType(
                typeof(global::AutoMapper.IMemberConfigurationExpression<,,>).MakeGenericType(
                    description.SourceType, description.DestinationType, memberType));

            var memberAction = BuildMemberAction(member, description, memberType, memberActionType);

            var forMemberGeneric = forMemberMethod.MakeGenericMethod(memberType);
            forMemberGeneric.Invoke(mapExpr, [typedDestLambda, memberAction]);
        }
    }

    private static LambdaExpression BuildDestinationAccess(Type destinationType, string memberName)
    {
        var param = Expression.Parameter(destinationType, "dest");
        var property = destinationType.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException(
                $"Property '{memberName}' not found on type '{destinationType.FullName}'.");
        return Expression.Lambda(Expression.Property(param, property), param);
    }

    private static Delegate BuildMemberAction(
        IMemberMapDescription member,
        IMapDescription description,
        Type memberType,
        Type memberActionType)
    {
        var interfaceType = typeof(global::AutoMapper.IMemberConfigurationExpression<,,>).MakeGenericType(
            description.SourceType, description.DestinationType, memberType);

        var interfaceMapFrom = FindExpressionMapFrom(interfaceType);
        var interfaceIgnore = FindIgnore(interfaceType);

        var param = Expression.Parameter(interfaceType, "adapter");
        var block = BuildMemberActionBody(member, description, param, interfaceMapFrom, interfaceIgnore);
        var lambda = Expression.Lambda(memberActionType, block, param);
        return lambda.Compile();
    }

    private static MethodInfo FindExpressionMapFrom(Type interfaceType)
    {
        const string mapFromMethodName =
            nameof(global::AutoMapper.IMemberConfigurationExpression<object, object, object>.MapFrom);

        return LambdaTypeConverter.FindSingleMethodOnHierarchy(
                   interfaceType,
                   mapFromMethodName,
                   method =>
                       method.GetGenericArguments().Length == 1
                       && method.GetParameters().Length == 1
                       && method.GetParameters()[0].ParameterType.IsGenericType
                       && method.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(Expression<>))
               ?? throw new InvalidOperationException(
                   $"Cannot find {mapFromMethodName}<TSourceMember>(" +
                   $"{nameof(Expression)}<{nameof(Func<object, object>)}<TSource, TSourceMember>>) " +
                   $"on '{interfaceType.FullName}'.");
    }

    private static MethodInfo FindIgnore(Type interfaceType)
    {
        const string ignoreMethodName =
            nameof(global::AutoMapper.IMemberConfigurationExpression<object, object, object>.Ignore);

        return LambdaTypeConverter.FindSingleMethodOnHierarchy(
                   interfaceType,
                   ignoreMethodName,
                   m => m.GetParameters().Length == 0)
               ?? throw new InvalidOperationException(
                   $"Cannot find {ignoreMethodName}() on '{interfaceType.FullName}'.");
    }

    private static Expression BuildMemberActionBody(
        IMemberMapDescription member,
        IMapDescription description,
        ParameterExpression param,
        MethodInfo interfaceMapFrom,
        MethodInfo interfaceIgnore)
    {
        // Свойство явно помечено как Ignore — вызвать Ignore() на AutoMapper-адаптере.
        if (member.IsIgnored)
        {
            return Expression.Call(param, interfaceIgnore);
        }

        // Ни обычное, ни параметризованное выражение не задано — тело пустое.
        if (member.SourceExpression is null && member.ParameterizedSourceExpression is null)
        {
            return Expression.Empty();
        }

        // Параметризованное выражение имеет приоритет над обычным.
        var isParameterized = member.ParameterizedSourceExpression is not null;
        var sourceExpr = isParameterized ? member.ParameterizedSourceExpression! : member.SourceExpression!;
        var sourceMemberType = sourceExpr.ReturnType;

        if (isParameterized)
        {
            // Параметризованный путь: выражение имеет вид (src, params) => value.
            // AutoMapper не понимает такой формы — подменяем params["key"] на holder.key.
            var paramsParam = sourceExpr.Parameters[1];
            var collector = new ParamKeyCollector(paramsParam);
            collector.Visit(sourceExpr.Body);
            var keys = collector.Keys;

            LambdaExpression lambdaToApply;
            if (keys.Count == 0)
            {
                // params не используется в теле — выражение совместимо с AutoMapper как есть.
                lambdaToApply = sourceExpr;
            }
            else
            {
                // Transform parameterized expression: replace params["key"] with holder.key
                // Holder — динамически собранный тип со свойствами для всех ключей params.
                var holderType = ParamHolderTypeCache.GetOrCreate(keys);
                var holderInstance = Activator.CreateInstance(holderType)!;
                var holderConst = Expression.Constant(holderInstance, holderType);
                var replacer = new ParamAccessReplacer(paramsParam, holderConst);
                var newBody = replacer.Visit(sourceExpr.Body);
                lambdaToApply = Expression.Lambda(
                    typeof(Func<,>).MakeGenericType(description.SourceType, sourceMemberType),
                    newBody,
                    sourceExpr.Parameters[0]);
            }

            var funcTypeParam = typeof(Func<,>).MakeGenericType(description.SourceType, sourceMemberType);
            var sourceLambdaTypeParam = typeof(Expression<>).MakeGenericType(funcTypeParam);
            var typedLambda = LambdaTypeConverter.ConvertLambdaType(lambdaToApply, sourceLambdaTypeParam);

            var paramMapFromGeneric = interfaceMapFrom.MakeGenericMethod(sourceMemberType);

            return Expression.Call(param, paramMapFromGeneric, Expression.Constant(typedLambda, sourceLambdaTypeParam));
        }

        // Обычный путь: выражение (src) => value.
        // Конвертируем в Expression<Func<TSource, TSourceMember>> и вызываем MapFrom.
        var funcTypeNormal = typeof(Func<,>).MakeGenericType(description.SourceType, sourceMemberType);
        var sourceLambdaTypeNormal = typeof(Expression<>).MakeGenericType(funcTypeNormal);
        var typedLambdaNormal = LambdaTypeConverter.ConvertLambdaType(sourceExpr, sourceLambdaTypeNormal);

        var mapFromGeneric = interfaceMapFrom.MakeGenericMethod(sourceMemberType);
        return Expression.Call(param, mapFromGeneric, Expression.Constant(typedLambdaNormal, sourceLambdaTypeNormal));
    }
}
