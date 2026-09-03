// ----------------------------------------------------------------------------------------------
// <copyright file="BeforeAfterMapApplicator.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Mapping.Description.Interfaces;
using Shared.Infrastructure.Mapper.AutoMapper.Adapters.Helpers;
using Shared.Infrastructure.Mapper.Core.ProfileBuilder.Interfaces;

namespace Shared.Infrastructure.Mapper.AutoMapper.Adapters.Applicators;

/// <summary>
/// Применяет <see cref="IMapDescription.BeforeMap"/> и <see cref="IMapDescription.AfterMap"/> к AutoMapper-выражению.
/// </summary>
internal sealed class BeforeAfterMapApplicator
    : IDescriptionApplicator
{
    /// <inheritdoc />
    public void Apply(object mapExpr, IMapDescription description)
    {
        var amMapType = typeof(global::AutoMapper.IMappingExpression<,>).MakeGenericType(
            description.SourceType, description.DestinationType);
        var actionType = typeof(Action<,>).MakeGenericType(description.SourceType, description.DestinationType);

        if (description.BeforeMap is not null)
        {
            Invoke(
                mapExpr,
                amMapType,
                actionType,
                description.BeforeMap,
                nameof(global::AutoMapper.IMappingExpression<object, object>.BeforeMap));
        }

        if (description.AfterMap is not null)
        {
            Invoke(
                mapExpr,
                amMapType,
                actionType,
                description.AfterMap,
                nameof(global::AutoMapper.IMappingExpression<object, object>.AfterMap));
        }
    }

    private static void Invoke(
        object mapExpr,
        Type amMapType,
        Type actionType,
        Action<object, object> action,
        string methodName)
    {
        var method = LambdaTypeConverter.FindSingleMethodOnHierarchy(amMapType, methodName, _ => true)
            ?? throw new InvalidOperationException($"{methodName} not found on '{amMapType.FullName}'.");

        var invokeMethod = action.GetType().GetMethod(nameof(Action.Invoke))!;
        var del = invokeMethod.CreateDelegate(actionType, action);

        method.Invoke(mapExpr, [del]);
    }
}
