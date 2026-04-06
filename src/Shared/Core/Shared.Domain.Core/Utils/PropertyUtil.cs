// ----------------------------------------------------------------------------------------------
// <copyright file="PropertyUtil.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Linq.Expressions;
using Shared.Domain.Core.Converters;
using Shared.Domain.Core.Converters.Interfaces;

namespace Shared.Domain.Core.Utils;

/// <summary>
/// Утилита для взаимодействия со свойствами объектов.
/// </summary>
public class PropertyUtil
{
    private static readonly Action<object, object?> NoOpSetter = (_, _) => { };
    private static readonly Func<object, object?> NullGetter = _ => null;
    private static readonly IObjectToStringConverter DefaultConverter = new DefaultObjectToStringConverter();

    private readonly ConcurrentDictionary<(Type, string), Action<object, object?>> _propertySetters = new();
    private readonly ConcurrentDictionary<(Type, string), Func<object, object?>> _propertyGetters = new();

    /// <summary>
    /// Устанавливает значение свойства объекта.
    /// </summary>
    /// <param name="obj">Объект, которому необходимо установить значение свойства.</param>
    /// <param name="propertyName">Название свойства, в которое необходимо установить значение.</param>
    /// <param name="value">Значение.</param>
    /// <param name="throwIfNotFound">
    /// Если <c>true</c> (по умолчанию) — выбрасывает <see cref="InvalidOperationException"/>, когда свойство не найдено.
    /// Если <c>false</c> — операция игнорируется молча: исключение не выбрасывается, значение не устанавливается.
    /// </param>
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

    /// <summary>
    /// Получает значение свойства объекта.
    /// </summary>
    /// <param name="obj">Объект, из которого необходимо получить значение свойства.</param>
    /// <param name="propertyName">Название свойства, значение которого необходимо получить.</param>
    /// <param name="throwIfNotFound">
    /// Если <c>true</c> (по умолчанию) — выбрасывает <see cref="InvalidOperationException"/>, когда свойство не найдено.
    /// Если <c>false</c> — возвращает <c>null</c>.
    /// </param>
    /// <returns>Значение свойства, либо <c>null</c> если свойство не найдено и <paramref name="throwIfNotFound"/> равен <c>false</c>.</returns>
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

    /// <summary>
    /// Получает строковое представление значения свойства объекта.
    /// </summary>
    /// <param name="obj">Объект, из которого необходимо получить значение свойства.</param>
    /// <param name="propertyName">Название свойства, значение которого необходимо получить.</param>
    /// <param name="converter">Конвертер объекта в строку. По умолчанию используется <see cref="DefaultObjectToStringConverter"/>.</param>
    /// <returns>Строковое представление значения свойства.</returns>
    /// <exception cref="InvalidOperationException">Выбрасывается, если свойство с именем <paramref name="propertyName"/> не найдено.</exception>
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
