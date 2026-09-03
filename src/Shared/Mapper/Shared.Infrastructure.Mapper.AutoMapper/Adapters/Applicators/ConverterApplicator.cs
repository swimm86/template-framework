// ----------------------------------------------------------------------------------------------
// <copyright file="ConverterApplicator.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;
using Shared.Domain.Core.Mapping.Description.Interfaces;
using Shared.Domain.Core.Mapping.Interfaces;
using Shared.Infrastructure.Mapper.AutoMapper.Adapters.Helpers;
using Shared.Infrastructure.Mapper.AutoMapper.Adapters.Wrappers;
using Shared.Infrastructure.Mapper.Core.ProfileBuilder.Interfaces;

namespace Shared.Infrastructure.Mapper.AutoMapper.Adapters.Applicators;

/// <summary>
/// Применяет настройки конвертеров типов (<see cref="IMapDescription.Converter"/>,
/// <see cref="IMapDescription.ConvertUsingFunction"/>, <see cref="IMapDescription.ConvertUsingFunctionWithDest"/>)
/// к AutoMapper-выражению.
/// </summary>
internal sealed class ConverterApplicator
    : IDescriptionApplicator
{
    /// <inheritdoc />
    public void Apply(object mapExpr, IMapDescription description)
    {
        if (description.Converter is null
            && description.ConvertUsingFunction is null
            && description.ConvertUsingFunctionWithDest is null)
        {
            return;
        }

        var amMapType = typeof(global::AutoMapper.IMappingExpression<,>).MakeGenericType(
            description.SourceType, description.DestinationType);
        var convertUsingMethods = LambdaTypeConverter.FindMethodsOnHierarchy(
            amMapType,
            nameof(global::AutoMapper.IMappingExpression<object, object>.ConvertUsing));

        if (description.Converter is not null)
        {
            var ourConverterInterfaceType = typeof(ITypeConverter<,>).MakeGenericType(
                description.SourceType, description.DestinationType);
            var autoMapperConverterInterfaceType = typeof(global::AutoMapper.ITypeConverter<,>).MakeGenericType(
                description.SourceType, description.DestinationType);

            if (!description.Converter.GetType().IsAssignableTo(ourConverterInterfaceType))
            {
                return;
            }

            var converterForAutoMapper = description.Converter.GetType().IsAssignableTo(autoMapperConverterInterfaceType)
                ? description.Converter
                : AdaptOurConverter(description.Converter, description.SourceType, description.DestinationType);

            var method = convertUsingMethods.FirstOrDefault(m =>
                m.GetParameters().Length == 1
                && m.GetParameters()[0].ParameterType == autoMapperConverterInterfaceType);

            method?.Invoke(mapExpr, [converterForAutoMapper]);
            return;
        }

        if (description.ConvertUsingFunctionWithDest is not null)
        {
            InvokeConvertUsingFuncWithDest(
                mapExpr,
                convertUsingMethods,
                description.SourceType,
                description.DestinationType,
                description.ConvertUsingFunctionWithDest);
        }
        else if (description.ConvertUsingFunction is not null)
        {
            InvokeConvertUsingFunc(
                mapExpr,
                convertUsingMethods,
                description.SourceType,
                description.DestinationType,
                description.ConvertUsingFunction);
        }
    }

    private static void InvokeConvertUsingFunc(
        object mapExpr,
        MethodInfo[] convertUsingMethods,
        Type sourceType,
        Type destinationType,
        Delegate func)
    {
        var sharedFuncType = typeof(Func<,,>).MakeGenericType(sourceType, destinationType, destinationType);

        var wrapperType = typeof(ConvertUsingFuncWrapper<,>).MakeGenericType(sourceType, destinationType);
        var wrapper = Activator.CreateInstance(wrapperType, func)!;

        var invokeMethod = wrapperType.GetMethod(
            nameof(ConvertUsingFuncWrapper<object, object>.Invoke))!;
        var del = invokeMethod.CreateDelegate(sharedFuncType, wrapper);

        var method = convertUsingMethods.FirstOrDefault(m =>
            m.GetParameters().Length == 1
            && m.GetParameters()[0].ParameterType == sharedFuncType);

        method?.Invoke(mapExpr, [del]);
    }

    private static void InvokeConvertUsingFuncWithDest(
        object mapExpr,
        MethodInfo[] convertUsingMethods,
        Type sourceType,
        Type destinationType,
        Delegate func)
    {
        var sharedFuncType = typeof(Func<,,,>).MakeGenericType(
            sourceType,
            destinationType,
            typeof(global::AutoMapper.ResolutionContext),
            destinationType);

        var wrapperType = typeof(ConvertUsingFuncWithDestWrapper<,>).MakeGenericType(sourceType, destinationType);
        var wrapper = Activator.CreateInstance(wrapperType, func)!;

        var invokeMethod = wrapperType.GetMethod(
            nameof(ConvertUsingFuncWithDestWrapper<object, object>.Invoke))!;
        var del = invokeMethod.CreateDelegate(sharedFuncType, wrapper);

        var method = convertUsingMethods.FirstOrDefault(m =>
            m.GetParameters().Length == 1
            && m.GetParameters()[0].ParameterType == sharedFuncType);

        method?.Invoke(mapExpr, [del]);
    }

    private static object AdaptOurConverter(object ourConverter, Type sourceType, Type destinationType)
    {
        var adapterType = typeof(TypeConverterAdapter<,>).MakeGenericType(sourceType, destinationType);
        return Activator.CreateInstance(adapterType, ourConverter)!;
    }
}
