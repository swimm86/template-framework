// ----------------------------------------------------------------------------------------------
// <copyright file="CtorParamApplicator.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using AutoMapper;
using Shared.Domain.Core.Mapping.Description.Interfaces;
using Shared.Infrastructure.Mapper.AutoMapper.Adapters.Helpers;
using Shared.Infrastructure.Mapper.Core.ProfileBuilder.Interfaces;

namespace Shared.Infrastructure.Mapper.AutoMapper.Adapters.Applicators;

/// <summary>
/// Применяет настройки ctor-параметров (<see cref="ICtorParamMapDescription"/>) к AutoMapper-выражению.
/// </summary>
internal sealed class CtorParamApplicator
    : IDescriptionApplicator
{
    /// <inheritdoc />
    public void Apply(object mapExpr, IMapDescription description)
    {
        if (description.CtorParams.Count == 0)
        {
            return;
        }

        var amCtorExprInterfaceType = typeof(global::AutoMapper.Configuration.ICtorParamConfigurationExpression<>)
            .MakeGenericType(description.SourceType);
        var actionType = typeof(Action<>).MakeGenericType(amCtorExprInterfaceType);

        var mapFromMethod = amCtorExprInterfaceType.GetMethods()
            .First(m =>
                m.Name == nameof(global::AutoMapper.Configuration.ICtorParamConfigurationExpression<object>.MapFrom)
                && m.GetParameters().Length == 1
                && m.GetParameters()[0].ParameterType.IsGenericType
                && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(Expression<>));

        var forCtorParamMethod = mapExpr.GetType().GetMethod(
            nameof(IProjectionExpression<object, object>.ForCtorParam),
            [typeof(string), actionType]);

        foreach (var ctorParam in description.CtorParams)
        {
            if (ctorParam.SourceExpression is null)
            {
                continue;
            }

            var sourceMemberType = ctorParam.SourceExpression!.ReturnType;
            var sourceLambdaType = typeof(Expression<>).MakeGenericType(
                typeof(Func<,>).MakeGenericType(description.SourceType, sourceMemberType));
            var typedLambda = LambdaTypeConverter.ConvertLambdaType(
                ctorParam.SourceExpression!, sourceLambdaType);

            var mapFromGeneric = mapFromMethod.MakeGenericMethod(sourceMemberType);
            var typedLambdaConst = Expression.Constant(typedLambda, sourceLambdaType);

            var adapterParam = Expression.Parameter(amCtorExprInterfaceType, "adapter");
            var body = Expression.Call(adapterParam, mapFromGeneric, typedLambdaConst);
            var lambda = Expression.Lambda(actionType, body, adapterParam);
            var del = lambda.Compile();

            forCtorParamMethod?.Invoke(mapExpr, [ctorParam.CtorParamName, del]);
        }
    }
}
