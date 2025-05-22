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
    /// Возвращает признак эквивалентности деревьев выражений.
    /// </summary>
    /// <param name="e1">Первое выражение для сравнения.</param>
    /// <param name="e2">Второе выражение для сравнения.</param>
    /// <returns>Результат проверки.</returns>
    public static bool AreEqual(
        this Expression? e1,
        Expression? e2)
    {
        if (e1 == null || e2 == null)
        {
            return e1 == e2;
        }

        if (e1.NodeType != e2.NodeType || e1.Type != e2.Type)
        {
            return false;
        }

        switch (e1.NodeType)
        {
            case ExpressionType.Parameter:
                return ((ParameterExpression)e1).Name == ((ParameterExpression)e2).Name;

            case ExpressionType.Lambda:
                var lambda1 = (LambdaExpression)e1;
                var lambda2 = (LambdaExpression)e2;

                if (lambda1.Parameters.Count != lambda2.Parameters.Count)
                    return false;

                for (int i = 0; i < lambda1.Parameters.Count; i++)
                {
                    if (!AreEqual(lambda1.Parameters[i], lambda2.Parameters[i]))
                        return false;
                }

                return AreEqual(lambda1.Body, lambda2.Body);

            case ExpressionType.GreaterThan:
            case ExpressionType.Equal:
            case ExpressionType.AndAlso:
                var b1 = (BinaryExpression)e1;
                var b2 = (BinaryExpression)e2;

                return AreEqual(b1.Left, b2.Left) && AreEqual(b1.Right, b2.Right);

            case ExpressionType.Constant:
                var c1 = (ConstantExpression)e1;
                var c2 = (ConstantExpression)e2;

                return Equals(c1.Value, c2.Value);

            default:
                throw new NotImplementedException($"Сравнение для типа {e1.NodeType} не реализовано.");
        }
    }
}
