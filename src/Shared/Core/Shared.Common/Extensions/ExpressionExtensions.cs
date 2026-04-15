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
    /// Получает выражения доступа к свойству и конечный тип свойства.
    /// </summary>
    /// <remarks>Поддерживает глубокий доступ.</remarks>
    /// <typeparam name="T">Тип входного параметра выражений.</typeparam>
    /// <param name="parameterExpr">Параметр выражения.</param>
    /// <param name="path">Путь к свойству через точку.</param>
    /// <returns>Пара (выражение доступа к свойству, тип свойства) или <c>(null, null)</c>, если параметр или свойство не найдены.</returns>
    public static (Expression? AccessExpr, Type? PropertyType) GetPropertyAccessAndType<T>(
        this ParameterExpression? parameterExpr,
        string path)
    {
        if (parameterExpr == null)
        {
            return (null, null);
        }

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

    /// <summary>
    /// Объединяет выражения через "И".
    /// </summary>
    /// <typeparam name="T">Тип входного параметра выражений.</typeparam>
    /// <param name="expr1">Выражение 1.</param>
    /// <param name="expr2">Выражение 2.</param>
    /// <returns>Объединенное выражение.</returns>
    /// <exception cref="ArgumentNullException">Выбрасывается если один из параметров равен <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Выбрасывается если выражения не содержат параметров.</exception>
    public static Expression<Func<T, bool>> And<T>(
        this Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        ArgumentNullException.ThrowIfNull(expr1);
        ArgumentNullException.ThrowIfNull(expr2);

        if (expr1.Parameters.Count == 0 || expr2.Parameters.Count == 0)
        {
            throw new ArgumentException("Выражения должны содержать хотя бы один параметр.");
        }

        var parameter = expr1.Parameters[0];
        var visitor = new ParameterReplacer(expr2.Parameters[0], parameter);
        var body2 = visitor.Visit(expr2.Body);
        return Expression.Lambda<Func<T, bool>>(
            Expression.AndAlso(expr1.Body, body2), parameter);
    }

    /// <summary>
    /// Объединяет выражения через "ИЛИ".
    /// </summary>
    /// <typeparam name="T">Тип входного параметра выражений.</typeparam>
    /// <param name="expr1">Выражение 1.</param>
    /// <param name="expr2">Выражение 2.</param>
    /// <returns>Объединенное выражение.</returns>
    /// <exception cref="ArgumentNullException">Выбрасывается если один из параметров равен <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Выбрасывается если выражения не содержат параметров.</exception>
    public static Expression<Func<T, bool>> Or<T>(
        this Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        ArgumentNullException.ThrowIfNull(expr1);
        ArgumentNullException.ThrowIfNull(expr2);

        if (expr1.Parameters.Count == 0 || expr2.Parameters.Count == 0)
        {
            throw new ArgumentException("Выражения должны содержать хотя бы один параметр.");
        }

        var parameter = expr1.Parameters[0];
        var visitor = new ParameterReplacer(expr2.Parameters[0], parameter);
        var body2 = visitor.Visit(expr2.Body);
        return Expression.Lambda<Func<T, bool>>(
            Expression.OrElse(expr1.Body, body2), parameter);
    }

    private sealed class ParameterReplacer(
        ParameterExpression source,
        ParameterExpression target)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == source ? target : base.VisitParameter(node);
    }
}
