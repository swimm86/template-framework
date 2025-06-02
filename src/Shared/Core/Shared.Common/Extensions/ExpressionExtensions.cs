// ----------------------------------------------------------------------------------------------
// <copyright file="ExpressionExtensions.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;

namespace Shared.Common.Extensions;

/// <summary>
/// Методы расширения для <see cref="Expression"/>.
/// </summary>
public static class ExpressionExtensions
{
    /// <summary>
    /// Получает имя свойства, указанного в лямбда-выражении.
    /// </summary>
    /// <typeparam name="TPreviousProperty">Тип объекта, из которого извлекается имя свойства.</typeparam>
    /// <typeparam name="TProperty">Тип свойства, имя которого необходимо получить.</typeparam>
    /// <param name="propertyExpression">Лямбда-выражение, указывающее на свойство, имя которого нужно извлечь.</param>
    /// <returns>Имя свойства, указанного в выражении.</returns>
    /// <exception cref="ArgumentException">Исключение, вызываемое если выражение не представляет свойство.</exception>
    public static string GetPropertyName<TPreviousProperty, TProperty>(
        this Expression<Func<TPreviousProperty, TProperty>> propertyExpression)
    {
        return propertyExpression.Body switch
        {
            MemberExpression memberExpression => memberExpression.Member.Name,
            UnaryExpression { Operand: MemberExpression expression } => expression.Member.Name,
            _ => throw new ArgumentException("Выражение не представляет свойство")
        };
    }

    /// <summary>
    /// Получает выражения доступа к свойству и конечный тип свойства сущности.
    /// </summary>
    /// <remarks>Поддерживает глубокий доступ.</remarks>
    /// <typeparam name="T">Тип сущности.</typeparam>
    /// <param name="parameterExpr">Параметр сущности.</param>
    /// <param name="path">Путь к свойству.</param>
    /// <returns>Пара ("Выражение доступа к свойству сущности", "Тип свойства сущности").</returns>
    public static (Expression? AccessExpr, Type? PropertyType) GetPropertyAccessAndType<T>(
        this ParameterExpression? parameterExpr,
        string path)
    {
        var currentType = typeof(T);
        var properties = path.Split('.');
        Expression? propertyAccess = parameterExpr;

        foreach (var property in properties)
        {
            var propInfo = currentType.GetPropertyIgnoreCase(property);

            if (propInfo == null)
            {
                return (null, null);
            }

            propertyAccess = Expression.MakeMemberAccess(propertyAccess, propInfo);
            currentType = propInfo.PropertyType;
        }

        return (propertyAccess, currentType);
    }
}
