// ----------------------------------------------------------------------------------------------
// <copyright file="PropertyUtil.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Domain.Core.Converters;
using Shared.Domain.Core.Converters.Interfaces;

namespace Shared.Domain.Core.Utils;

/// <summary>
/// Утилита для взаимодействия со свойствами объектов.
/// </summary>
public class PropertyUtil
{
    private readonly Dictionary<(Type, string), Action<object, object>> _propertySetters = new();
    private readonly Dictionary<(Type, string), Func<object, object>> _propertyGetters = new();

    /// <summary>
    /// Устанавливает значения для свойств объекта.
    /// </summary>
    /// <param name="obj">Обект, которому необходимо установить значение свойства.</param>
    /// <param name="propertyName">Название свойства, в которое необходимо установить значение.</param>
    /// <param name="value">Знаечние.</param>
    public void SetProperty(object obj, string propertyName, object value)
    {
        var objectType = obj.GetType();
        var key = (objectType, propertyName);
        if (!_propertySetters.TryGetValue(key, out var setter))
        {
            var propertyInfo = obj.GetType().GetProperty(propertyName);
            if (propertyInfo != null)
            {
                var parameter = Expression.Parameter(typeof(object), "instance");
                var valueParameter = Expression.Parameter(typeof(object), nameof(value));
                var propertyExpression = Expression.Property(Expression.Convert(parameter, objectType), propertyInfo);
                var assignExpression = Expression.Assign(
                    propertyExpression,
                    Expression.Convert(valueParameter, propertyInfo.PropertyType));
                var lambdaExpression =
                    Expression.Lambda<Action<object, object>>(assignExpression, parameter, valueParameter);
                setter = lambdaExpression.Compile();
                _propertySetters.Add(key, setter);
            }
            else
            {
                throw new InvalidOperationException($"Property {propertyName} not found");
            }
        }

        setter(obj, value);
    }

    /// <summary>
    /// Получает значение свойства объекта.
    /// </summary>
    /// <param name="obj">Обект, из которого необходимо получить значение свойства.</param>
    /// <param name="propertyName">Название свойства, значение которого необходимо получить.</param>
    /// <returns>Значение свойства.</returns>
    public object? GetProperty(object obj, string propertyName)
    {
        var objectType = obj.GetType();
        var key = (objectType, propertyName);
        if (_propertyGetters.TryGetValue(key, out var getter))
        {
            return getter(obj);
        }

        var propertyInfo = objectType.GetProperty(propertyName);
        if (propertyInfo != null)
        {
            var parameter = Expression.Parameter(typeof(object), "instance");
            var propertyExpression = Expression.Property(Expression.Convert(parameter, objectType), propertyInfo);
            var convertExpression = Expression.Convert(propertyExpression, typeof(object));
            var lambdaExpression =
                Expression.Lambda<Func<object, object>>(convertExpression, parameter);
            getter = lambdaExpression.Compile();
            _propertyGetters.Add(key, getter);
        }
        else
        {
            throw new InvalidOperationException($"Property {propertyName} not found");
        }

        return getter(obj);
    }

    /// <summary>
    /// Получает значение свойства объекта.
    /// </summary>
    /// <param name="obj">Обект, из которого необходимо получить значение свойства.</param>
    /// <param name="propertyName">Название свойства, значение которого необходимо получить.</param>
    /// <param name="converter">Конвертер объекта в строку (по-умолчанию используется <see cref="DefaultObjectToStringConverter"/>).</param>
    /// <returns>Значение свойства.</returns>
    public string GetPropertyAsString(
        object obj,
        string propertyName,
        IObjectToStringConverter? converter = default)
    {
        var value = GetProperty(obj, propertyName);
        converter ??= new DefaultObjectToStringConverter();
        return converter.Convert(value);
    }
}
