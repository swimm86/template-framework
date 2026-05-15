// ----------------------------------------------------------------------------------------------
// <copyright file="ExpressionHelper.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;

namespace Shared.Common.Helpers;

/// <summary>
/// Полезные методы для работы с лямбда-выражениями.
/// </summary>
public static class ExpressionHelper
{
    /// <summary>
    /// Возвращает лямбда-выражение, которое возвращает значение указанного свойства.
    /// </summary>
    /// <typeparam name="TObj">Тип объекта.</typeparam>
    /// <param name="propertyName">Наименование свойства.</param>
    /// <param name="delimiter">Разделитель.</param>
    /// <returns>Лямбда-выражение, которое возвращает значение указанного свойства.</returns>
    public static Expression<Func<TObj, object>> GetPropExpression<TObj>(string propertyName, char delimiter = '.')
    {
        var parameter = Expression.Parameter(typeof(TObj), "p");

        var propertyNames = propertyName.Split(delimiter);
        var currentType = typeof(TObj);
        Expression expression = parameter;

        foreach (var name in propertyNames)
        {
            var propertyInfo = currentType.GetProperty(name);
            if (propertyInfo == null)
            {
                throw new ArgumentException(
                    $"Property '{name}' not found on type '{currentType.Name}'. " +
                    $"Full path: '{propertyName}'.");
            }

            expression = Expression.Property(expression, propertyInfo);
            currentType = propertyInfo.PropertyType;
        }

        var conversion = Expression.Convert(expression, typeof(object));
        return Expression.Lambda<Func<TObj, object>>(conversion, parameter);
    }
}
