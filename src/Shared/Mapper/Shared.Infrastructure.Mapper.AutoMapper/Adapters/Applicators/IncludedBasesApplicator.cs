// ----------------------------------------------------------------------------------------------
// <copyright file="IncludedBasesApplicator.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Mapping.Description.Interfaces;
using Shared.Infrastructure.Mapper.AutoMapper.Adapters.Helpers;
using Shared.Infrastructure.Mapper.Core.ProfileBuilder.Interfaces;

namespace Shared.Infrastructure.Mapper.AutoMapper.Adapters.Applicators;

/// <summary>
/// Применяет <see cref="IMapDescription.IncludedBases"/> и <see cref="IMapDescription.HasReverseMap"/>
/// к AutoMapper-выражению.
/// </summary>
internal sealed class IncludedBasesApplicator
    : IDescriptionApplicator
{
    /// <inheritdoc />
    public void Apply(object mapExpr, IMapDescription description)
    {
        if (description.HasReverseMap)
        {
            ApplyReverseMap(mapExpr, description);
        }

        ApplyIncludedBases(mapExpr, description);
    }

    private static void ApplyReverseMap(object mapExpr, IMapDescription description)
    {
        var amMapType = typeof(global::AutoMapper.IMappingExpression<,>).MakeGenericType(
            description.SourceType, description.DestinationType);
        var reverseMapMethod = LambdaTypeConverter.FindSingleMethodOnHierarchy(
            amMapType,
            nameof(global::AutoMapper.IMappingExpression<object, object>.ReverseMap),
            m => m.GetParameters().Length == 0)
            ?? throw new InvalidOperationException(
                $"{nameof(global::AutoMapper.IMappingExpression<object, object>.ReverseMap)} not found.");
        reverseMapMethod.Invoke(mapExpr, null);
    }

    private static void ApplyIncludedBases(object mapExpr, IMapDescription description)
    {
        if (description.IncludedBases.Count == 0)
        {
            return;
        }

        var amMapType = typeof(global::AutoMapper.IMappingExpression<,>).MakeGenericType(
            description.SourceType, description.DestinationType);
        var includeBaseMethod = LambdaTypeConverter.FindSingleMethodOnHierarchy(
            amMapType,
            nameof(global::AutoMapper.IMappingExpression<object, object>.IncludeBase),
            m => m.IsGenericMethodDefinition)
            ?? throw new InvalidOperationException(
                $"{nameof(global::AutoMapper.IMappingExpression<object, object>.IncludeBase)} not found.");

        foreach (var (srcBase, destBase) in description.IncludedBases)
        {
            var generic = includeBaseMethod.MakeGenericMethod(srcBase, destBase);
            generic.Invoke(mapExpr, null);
        }
    }
}
