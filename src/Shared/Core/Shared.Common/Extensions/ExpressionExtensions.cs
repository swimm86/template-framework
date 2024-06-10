// ----------------------------------------------------------------------------------------------
// <copyright file="ExpressionExtensions.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
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
    /// Получает имя выходного поля Func.
    /// </summary>
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
}
