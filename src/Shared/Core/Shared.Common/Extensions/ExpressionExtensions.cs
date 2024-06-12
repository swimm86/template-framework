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
}
