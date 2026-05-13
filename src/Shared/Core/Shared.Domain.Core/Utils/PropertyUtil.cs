// ----------------------------------------------------------------------------------------------
// <copyright file="PropertyUtil.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Linq.Expressions;
using Shared.Domain.Core.Converters;
using Shared.Domain.Core.Converters.Interfaces;
using Shared.Domain.Core.Utils.Interfaces;

namespace Shared.Domain.Core.Utils;

/// <summary>
/// Утилита для взаимодействия со свойствами объектов.
/// </summary>
public class PropertyUtil
    : IPropertyGetter, IPropertySetter
{
    private static readonly Action<object, object?> NoOpSetter = (_, _) => { };
    private static readonly Func<object, object?> NullGetter = _ => null;
    private static readonly IObjectToStringConverter DefaultConverter = new DefaultObjectToStringConverter();

    private readonly ConcurrentDictionary<(Type, string), Action<object, object?>> _propertySetters = new();
    private readonly ConcurrentDictionary<(Type, string), Func<object, object?>> _propertyGetters = new();

    /// <inheritdoc />
    public void SetProperty(
        object obj,
        string propertyName,
        object? value,
        bool throwIfNotFound = true)
    {
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        var objectType = obj.GetType();
        var key = (objectType, propertyName);

        var setter = _propertySetters.GetOrAdd(key, x =>
        {
            var (type, name) = x;
            var propertyInfo = type.GetProperty(name);
            if (propertyInfo == null)
            {
                return NoOpSetter;
            }

            var parameter = Expression.Parameter(typeof(object), "instance");
            var valueParameter = Expression.Parameter(typeof(object), "value");
            var propertyExpression = Expression.Property(Expression.Convert(parameter, type), propertyInfo);
            var assignExpression = Expression.Assign(
                propertyExpression,
                Expression.Convert(valueParameter, propertyInfo.PropertyType));
            var lambdaExpression =
                Expression.Lambda<Action<object, object?>>(assignExpression, parameter, valueParameter);
            return lambdaExpression.Compile();
        });

        if (setter == NoOpSetter && throwIfNotFound)
        {
            throw new InvalidOperationException($"Property {propertyName} not found");
        }

        setter(obj, value);
    }

    /// <inheritdoc />
    public object? GetProperty(
        object? obj,
        string propertyName,
        bool throwIfNotFound = true)
    {
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        var objectType = obj.GetType();
        var key = (objectType, propertyName);

        var getter = _propertyGetters.GetOrAdd(key, x =>
        {
            var (type, name) = x;
            var propertyInfo = type.GetProperty(name);
            if (propertyInfo == null)
            {
                return NullGetter;
            }

            var parameter = Expression.Parameter(typeof(object), "instance");
            var propertyExpression = Expression.Property(Expression.Convert(parameter, type), propertyInfo);
            var convertExpression = Expression.Convert(propertyExpression, typeof(object));
            var lambdaExpression =
                Expression.Lambda<Func<object, object?>>(convertExpression, parameter);
            return lambdaExpression.Compile();
        });

        if (getter == NullGetter)
        {
            return throwIfNotFound
                ? throw new InvalidOperationException($"Property {propertyName} not found")
                : null;
        }

        return getter(obj);
    }

    /// <inheritdoc />
    public string GetPropertyAsString(
        object obj,
        string propertyName,
        IObjectToStringConverter? converter = null)
    {
        var value = GetProperty(obj, propertyName);
        converter ??= DefaultConverter;
        return converter.Convert(value);
    }
}
