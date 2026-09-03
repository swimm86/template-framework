// ----------------------------------------------------------------------------------------------
// <copyright file="ConverterApplicator.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using System.Reflection;
using Mapster;
using Shared.Domain.Core.Mapping.Description.Interfaces;
using Shared.Domain.Core.Mapping.Interfaces;
using Shared.Infrastructure.Mapper.Core.ProfileBuilder.Interfaces;
using Shared.Infrastructure.Mapper.Mapster.Adapters.Wrappers;

namespace Shared.Infrastructure.Mapper.Mapster.Adapters.Applicators;

/// <summary>
/// Применяет настройки конвертеров типов (type converter) к Mapster-сеттеру.
/// </summary>
internal sealed class ConverterApplicator
    : IDescriptionApplicator
{
    /// <inheritdoc />
    public void Apply(object setter, IMapDescription description)
    {
        if (description.Converter is not null)
        {
            ApplyConverterObject(setter, description);
        }
        else if (description.ConvertUsingFunctionWithDest is not null)
        {
            ApplyConvertUsingFunctionWithDest(setter, description);
        }
        else if (description.ConvertUsingFunction is not null)
        {
            ApplyConvertUsingFunction(setter, description);
        }
    }

    private static void ApplyConverterObject(object setter, IMapDescription description)
    {
        var converterInterfaceType = typeof(ITypeConverter<,>).MakeGenericType(
            description.SourceType, description.DestinationType);

        if (!description.Converter!.GetType().IsAssignableTo(converterInterfaceType))
        {
            return;
        }

        var adapterType = typeof(TypeConverterAdapter<,>).MakeGenericType(
            description.SourceType, description.DestinationType);
        var adapter = Activator.CreateInstance(adapterType, description.Converter)!;

        var funcType = typeof(Func<,>).MakeGenericType(description.SourceType, description.DestinationType);
        var setterType = setter.GetType();
        var mapWith = setterType.GetMethod(
            nameof(TypeAdapterSetter<object, object>.MapWith),
            [typeof(Expression<>).MakeGenericType(funcType), typeof(bool)]);
        if (mapWith is null)
        {
            return;
        }

        var srcParam = Expression.Parameter(description.SourceType, "src");
        var adapterConst = Expression.Constant(adapter);
        var invokeMethod = adapterType.GetMethod(
            nameof(TypeConverterAdapter<object, object>.Invoke),
            [description.SourceType])!;
        var body = Expression.Call(adapterConst, invokeMethod, srcParam);

        var lambdaObj = Expression.Lambda(funcType, body, srcParam);

        mapWith.Invoke(setter, [lambdaObj, true]);
    }

    private static void ApplyConvertUsingFunction(object setter, IMapDescription description)
    {
        var func = description.ConvertUsingFunction!;

        var funcType = typeof(Func<,>).MakeGenericType(description.SourceType, description.DestinationType);
        var setterType = setter.GetType();
        var mapWith = setterType.GetMethod(
            nameof(TypeAdapterSetter<object, object>.MapWith),
            [typeof(Expression<>).MakeGenericType(funcType), typeof(bool)]);
        if (mapWith is null)
        {
            return;
        }

        var srcParam = Expression.Parameter(description.SourceType, "src");
        var funcConst = Expression.Constant(func);
        var invokeMethod = func.GetType().GetMethod("Invoke")!;
        var funcCall = Expression.Call(funcConst, invokeMethod, srcParam);

        // Результат func — object; конвертируем в TDestination для lambda-сигнатуры.
        var convertedBody = Expression.Convert(funcCall, description.DestinationType);

        var lambdaObj = Expression.Lambda(funcType, convertedBody, srcParam);

        mapWith.Invoke(setter, [lambdaObj, true]);
    }

    private static void ApplyConvertUsingFunctionWithDest(object setter, IMapDescription description)
    {
        const string mapToTargetWithMethodName = nameof(TypeAdapterSetter<object, object>.MapToTargetWith);

        var sharedFunc = description.ConvertUsingFunctionWithDest!;
        var wrapperType = typeof(ConvertUsingFuncWithDestWrapper<,>).MakeGenericType(
            description.SourceType, description.DestinationType);
        var wrapper = Activator.CreateInstance(wrapperType, sharedFunc)!;

        var setterType = setter.GetType();
        var func2Type = typeof(Func<,,>).MakeGenericType(
            description.SourceType, description.DestinationType, description.DestinationType);

        var srcParam = Expression.Parameter(description.SourceType, "src");
        var destParam = Expression.Parameter(description.DestinationType, "dest");
        var wrapperConst = Expression.Constant(wrapper);
        var invokeMethod = wrapperType.GetMethod(
            nameof(ConvertUsingFuncWithDestWrapper<object, object>.Invoke),
            [description.SourceType, description.DestinationType])!;

        // MapToTargetWith(Func<TSource, TDestination, TDestination>) — обновляет существующий объект
        // или создаёт новый, если dest == null.
        var mapToTargetBody = Expression.Call(wrapperConst, invokeMethod, srcParam, destParam);
        var mapToTargetLambda = Expression.Lambda(func2Type, mapToTargetBody, srcParam, destParam);

        // Попытка 1: instance-метод на setter
        var setterMapToTarget = setterType.GetMethods()
            .FirstOrDefault(m =>
                m.Name == mapToTargetWithMethodName
                && m.GetParameters().Length == 2
                && m.GetParameters()[0].ParameterType == typeof(Expression<>).MakeGenericType(func2Type)
                && m.GetParameters()[1].ParameterType == typeof(bool));
        if (setterMapToTarget is not null)
        {
            setterMapToTarget.Invoke(setter, [mapToTargetLambda, true]);
            return;
        }

        // Попытка 2: extension-метод
        var extMapToTarget = typeof(TypeAdapterSetterExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m =>
                m is { Name: mapToTargetWithMethodName, IsGenericMethodDefinition: true }
                && m.GetParameters().Length == 3
                && m.GetParameters()[0].ParameterType.IsGenericParameter
                && m.GetParameters()[1].ParameterType == typeof(Expression<>).MakeGenericType(func2Type)
                && m.GetParameters()[2].ParameterType == typeof(bool));
        if (extMapToTarget is not null)
        {
            var closed = extMapToTarget.MakeGenericMethod(setterType);
            closed.Invoke(null, [setter, mapToTargetLambda, true]);
        }
    }
}
