// ----------------------------------------------------------------------------------------------
// <copyright file="ExpressionExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
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
    /// <exception cref="ArgumentException">Выбрасывается, если выражение не представляет доступ к свойству.</exception>
    public static string GetPropertyName<TPreviousProperty, TProperty>(
        this Expression<Func<TPreviousProperty, TProperty>> propertyExpression)
    {
        return propertyExpression.Body switch
        {
            MemberExpression memberExpression => memberExpression.Member.Name,
            UnaryExpression { Operand: MemberExpression expression } => expression.Member.Name,
            _ => throw new ArgumentException("Expression must represent a property access.")
        };
    }

    /// <summary>
    /// Получает выражение доступа к свойству и его конечный тип по точечному пути.
    /// </summary>
    /// <remarks>Поддерживает вложенные свойства (глубокий доступ через точку).</remarks>
    /// <typeparam name="T">Тип входного параметра выражения.</typeparam>
    /// <param name="parameterExpr">Параметр выражения.</param>
    /// <param name="path">Путь к свойству через точку (регистронезависимый).</param>
    /// <returns>Кортеж (выражение доступа, тип свойства) или <c>(null, null)</c>, если свойство не найдено.</returns>
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
        Expression propertyAccess = parameterExpr;

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
    /// Объединяет выражения через «И» (AndAlso).
    /// </summary>
    /// <typeparam name="T">Тип входного параметра выражений.</typeparam>
    /// <param name="expr1">Первое логическое выражение.</param>
    /// <param name="expr2">Второе логическое выражение.</param>
    /// <returns>Объединённое логическое выражение.</returns>
    /// <exception cref="ArgumentNullException">Выбрасывается, если один из параметров равен <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Выбрасывается, если выражения не содержат параметров.</exception>
    public static Expression<Func<T, bool>> And<T>(
        this Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        ArgumentNullException.ThrowIfNull(expr1);
        ArgumentNullException.ThrowIfNull(expr2);

        if (expr1.Parameters.Count == 0 || expr2.Parameters.Count == 0)
        {
            throw new ArgumentException("Expressions must contain at least one parameter.");
        }

        var parameter = expr1.Parameters[0];
        var visitor = new ParameterReplacer(expr2.Parameters[0], parameter);
        var body2 = visitor.Visit(expr2.Body);
        return Expression.Lambda<Func<T, bool>>(
            Expression.AndAlso(expr1.Body, body2), parameter);
    }

    /// <summary>
    /// Объединяет выражения через «ИЛИ» (OrElse).
    /// </summary>
    /// <typeparam name="T">Тип входного параметра выражений.</typeparam>
    /// <param name="expr1">Первое логическое выражение.</param>
    /// <param name="expr2">Второе логическое выражение.</param>
    /// <returns>Объединённое логическое выражение.</returns>
    /// <exception cref="ArgumentNullException">Выбрасывается, если один из параметров равен <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Выбрасывается, если выражения не содержат параметров.</exception>
    public static Expression<Func<T, bool>> Or<T>(
        this Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        ArgumentNullException.ThrowIfNull(expr1);
        ArgumentNullException.ThrowIfNull(expr2);

        if (expr1.Parameters.Count == 0 || expr2.Parameters.Count == 0)
        {
            throw new ArgumentException("Expressions must contain at least one parameter.");
        }

        var parameter = expr1.Parameters[0];
        var visitor = new ParameterReplacer(expr2.Parameters[0], parameter);
        var body2 = visitor.Visit(expr2.Body);
        return Expression.Lambda<Func<T, bool>>(
            Expression.OrElse(expr1.Body, body2), parameter);
    }

    /// <summary>
    /// Объединяет набор предикатов в одно выражение через "И" (AndAlso).
    /// </summary>
    /// <returns>
    /// Единое выражение <see cref="Expression{TDelegate}"/> типа <c>Func{T, bool}</c>,
    /// объединяющее все предикаты через AndAlso;
    /// <see langword="null"/>, если коллекция пуста.
    /// </returns>
    /// <inheritdoc cref="CombineAll"/>
    public static Expression<Func<T, bool>>? CombineAllAndAlso<T>(
        this IReadOnlyList<Expression<Func<T, bool>>>? predicates) =>
        CombineAll(predicates, And);

    /// <summary>
    /// Объединяет набор предикатов в одно выражение через "ИЛИ" (OrElse).
    /// </summary>
    /// <returns>
    /// Единое выражение <see cref="Expression{TDelegate}"/> типа <c>Func{T, bool}</c>,
    /// объединяющее все предикаты через OrElse;
    /// <see langword="null"/>, если коллекция пуста.
    /// </returns>
    /// <inheritdoc cref="CombineAll"/>
    public static Expression<Func<T, bool>>? CombineAllOrElse<T>(
        this IReadOnlyList<Expression<Func<T, bool>>>? predicates) =>
        CombineAll(predicates, Or);

    /// <summary>
    /// Объединяет набор предикатов в одно выражение через <paramref name="combineFunc"/>.
    /// </summary>
    /// <typeparam name="T">Тип входного параметра предикатов.</typeparam>
    /// <param name="predicates">Коллекция предикатов.</param>
    /// <param name="combineFunc">Функция логического объединения двух предикатов.</param>
    /// <returns>
    /// Единое выражение <see cref="Expression{TDelegate}"/> типа <c>Func{T, bool}</c>,
    /// объединяющее все предикаты через <paramref name="combineFunc"/>;
    /// <see langword="null"/>, если коллекция пуста.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Выбрасывается, если хотя бы один элемент <paramref name="predicates"/> равен <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// Предикаты объединяются попарно для ограничения глубины дерева выражений
    /// и предотвращения переполнения стека компилятора при большом количестве предикатов.
    /// </remarks>
    private static Expression<Func<T, bool>>? CombineAll<T>(
        this IReadOnlyList<Expression<Func<T, bool>>>? predicates,
        Func<Expression<Func<T, bool>>, Expression<Func<T, bool>>, Expression<Func<T, bool>>> combineFunc)
    {
        if (predicates?.Any() != true)
        {
            return null;
        }

        var nullIndexes = predicates
            .Select((item, i) => new
            {
                item,
                i,
            })
            .Where(x => x.item is null)
            .Select(x => x.i)
            .ToArray();

        if (nullIndexes.Length > 0)
        {
            throw new ArgumentException(
                $"Null predicates at indexes: {string.Join(", ", nullIndexes)}.",
                nameof(predicates));
        }

        if (predicates.Count == 1)
        {
            return predicates[0];
        }

        var current = predicates;
        while (current.Count > 1)
        {
            var next = new List<Expression<Func<T, bool>>>((current.Count + 1) / 2);
            for (var i = 0; i < current.Count; i += 2)
            {
                next.Add(i + 1 < current.Count
                    ? combineFunc(current[i], current[i + 1])
                    : current[i]);
            }

            current = next;
        }

        return current[0];
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
