// ----------------------------------------------------------------------------------------------
// <copyright file="ExpressionExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;

namespace Shared.Infrastructure.Dal.EFCore.Extensions;

/// <summary>
/// Статический класс, содержащий методы расширения для работы с выражениями (Expression).
/// </summary>
public static class ExpressionExtensions
{
    /// <summary>
    /// Оборачивает выражение в вызов метода EF.Functions.Collate для свойств типа string.
    /// Применяет колляцию только к свойствам типа string, игнорируя все остальные типы.
    /// </summary>
    /// <typeparam name="TEntity">
    /// Тип сущности, для которой применяется выражение.
    /// </typeparam>
    /// <param name="expression">
    /// Исходное выражение, которое нужно преобразовать.
    /// </param>
    /// <param name="collation">
    /// Название колляции, которая будет применена к свойствам типа string.
    /// </param>
    /// <returns>
    /// Преобразованное выражение, в котором свойства типа string обёрнуты в вызов EF.Functions.Collate.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если переданное выражение равно null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Выбрасывается, если параметр collation равен null или пустой строке.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если метод EF.Functions.Collate не найден через рефлексию.
    /// </exception>
    public static Expression<Func<TEntity, object>> WrapWithCollate<TEntity>(
        this Expression<Func<TEntity, object>> expression,
        string collation)
    {
        if (expression == null)
        {
            throw new ArgumentNullException(nameof(expression));
        }

        if (string.IsNullOrEmpty(collation))
        {
            throw new ArgumentException("Collation cannot be null or empty.", nameof(collation));
        }

        // Создаем посетителя для модификации выражения
        var visitor = new CollateExpressionVisitor(collation);

        // Применяем посетителя к телу выражения
        var newBody = visitor.Visit(expression.Body);

        // Возвращаем новое выражение с измененным телом
        return Expression.Lambda<Func<TEntity, object>>(newBody, expression.Parameters);
    }

    /// <summary>
    /// Внутренний класс-посетитель выражений, который используется для модификации дерева выражений.
    /// Применяет колляцию к свойствам типа string, заменяя доступ к свойству вызовом EF.Functions.Collate.
    /// </summary>
    /// <param name="collation">
    /// Название колляции, которая будет применена к свойствам типа string.
    /// </param>
    private class CollateExpressionVisitor(string collation)
        : ExpressionVisitor
    {
        /// <summary>
        /// Переопределяет метод VisitMember для обработки узлов MemberExpression.
        /// Проверяет, является ли свойство строковым, и заменяет его на вызов EF.Functions.Collate.
        /// </summary>
        /// <param name="node">
        /// Узел MemberExpression, представляющий доступ к свойству.
        /// </param>
        /// <returns>
        /// Преобразованный узел выражения, если свойство имеет тип string.
        /// В противном случае возвращает исходный узел без изменений.
        /// </returns>
        protected override Expression VisitMember(MemberExpression node)
        {
            // Получаем тип свойства (например, string для x.Name)
            var propertyType = node.Type;

            // Проверяем, что свойство имеет тип string, тк COLLATION применяется только к строкам
            if (propertyType != typeof(string))
            {
                return base.VisitMember(node);
            }

            // Получаем метод EF.Functions.Collate<TProperty> через рефлексию
            var collateMethod = typeof(RelationalDbFunctionsExtensions)
                .GetMethod(nameof(RelationalDbFunctionsExtensions.Collate));

            if (collateMethod == null)
            {
                throw new InvalidOperationException("EF.Functions.Collate method not found.");
            }

            // Создаем обобщенный метод Collate<TProperty>
            var genericCollateMethod = collateMethod.MakeGenericMethod(propertyType);

            // Вызываем метод EF.Functions.Collate<TProperty> с аргументами: EF.Functions, свойство и колляция
            return Expression.Call(
                null,
                genericCollateMethod,
                Expression.Constant(EF.Functions),
                node,
                Expression.Constant(collation));
        }
    }
}
