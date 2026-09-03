// ----------------------------------------------------------------------------------------------
// <copyright file="ConstructUsingApplicator.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Mapping.Description.Interfaces;
using Shared.Infrastructure.Mapper.AutoMapper.Adapters.Helpers;
using Shared.Infrastructure.Mapper.AutoMapper.Adapters.Wrappers;
using Shared.Infrastructure.Mapper.Core.ProfileBuilder.Interfaces;

namespace Shared.Infrastructure.Mapper.AutoMapper.Adapters.Applicators;

/// <summary>
/// Применяет настройки <see cref="IMapDescription.ConstructUsing"/> к AutoMapper-выражению.
/// </summary>
internal sealed class ConstructUsingApplicator
    : IDescriptionApplicator
{
    /// <inheritdoc />
    public void Apply(object mapExpr, IMapDescription description)
    {
        if (description.ConstructUsing is null)
        {
            return;
        }

        var amMapType = typeof(global::AutoMapper.IMappingExpression<,>).MakeGenericType(
            description.SourceType, description.DestinationType);

        var sharedFuncType = typeof(Func<,,>).MakeGenericType(
            description.SourceType,
            typeof(global::AutoMapper.ResolutionContext),
            description.DestinationType);
        var constructUsingMethod = LambdaTypeConverter.FindSingleMethodOnHierarchy(
            amMapType,
            nameof(global::AutoMapper.IMappingExpression<object, object>.ConstructUsing),
            m => m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == sharedFuncType)
            ?? throw new InvalidOperationException(
                $"{nameof(global::AutoMapper.IMappingExpression<object, object>.ConstructUsing)}" +
                $"(Func<{description.SourceType.Name}, {nameof(global::AutoMapper.ResolutionContext)}, " +
                $"{description.DestinationType.Name}>) not found.");

        var sharedFunc = description.ConstructUsing;
        var wrapperType = typeof(ConstructUsingWrapper<,>).MakeGenericType(
            description.SourceType, description.DestinationType);
        var wrapper = Activator.CreateInstance(wrapperType, sharedFunc)!;

        var invokeMethod = wrapperType.GetMethod(
            nameof(ConstructUsingWrapper<object, object>.Invoke))!;
        var del = invokeMethod.CreateDelegate(sharedFuncType, wrapper);

        constructUsingMethod.Invoke(mapExpr, [del]);
    }
}
