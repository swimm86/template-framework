// ----------------------------------------------------------------------------------------------
// <copyright file="ParameterizedExpressionTransformer.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using System.Reflection;
using Mapster;

namespace Shared.Infrastructure.Mapper.Mapster.Adapters.ExpressionTransformers;

/// <summary>
/// Трансформер лямбда-выражения с параметрами <c>(TSource src, IDictionary&lt;string, object?&gt; params)</c>
/// в лямбда-выражение без второго параметра, где доступ к params["key"] заменён на
/// <c>MapContext.Current.Parameters["key"]</c>.
/// </summary>
internal sealed class ParameterizedExpressionTransformer(
    ParameterExpression paramsParam)
    : ExpressionVisitor
{
    private readonly HashSet<string> _keys = [];

    /// <summary>
    /// Преобразует параметризованное лямбда-выражение в обычное, заменив доступ к
    /// <c>IDictionary&lt;string, object?&gt;</c> на <c>MapContext.Current.Parameters</c>.
    /// </summary>
    /// <param name="expression">Исходное выражение вида <c>(src, params) => ... params["key"] ...</c>.</param>
    /// <param name="sourceType">Исходный тип (TSource).</param>
    /// <returns>Кортеж: преобразованное выражение и набор ключей параметров.</returns>
    public static (LambdaExpression Expression, HashSet<string> Keys) Transform(
        LambdaExpression expression, Type sourceType)
    {
        if (expression.Parameters.Count != 2)
        {
            throw new ArgumentException(
                $"Expected parameterized expression with 2 parameters " +
                $"(TSource, {nameof(IDictionary<object, object>)}<{nameof(String)}, {nameof(Object)}?>)",
                nameof(expression));
        }

        var paramsParam = expression.Parameters[1];
        var transformer = new ParameterizedExpressionTransformer(paramsParam);

        var newBody = transformer.Visit(expression.Body);
        return (Expression.Lambda(newBody, expression.Parameters[0]), transformer._keys);
    }

    /// <inheritdoc />
    protected override Expression VisitIndex(IndexExpression node)
    {
        if (node.Object == paramsParam)
        {
            CollectKeys(node.Arguments);
            return BuildMapContextParametersAccess(node.Arguments);
        }

        return base.VisitIndex(node);
    }

    /// <inheritdoc />
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Object == paramsParam && node.Method.Name == "get_Item")
        {
            CollectKeys(node.Arguments);
            return BuildMapContextParametersCall(node.Arguments);
        }

        return base.VisitMethodCall(node);
    }

    private static Expression BuildMapContextParametersAccess(IList<Expression> arguments)
    {
        var currentProp = typeof(MapContext).GetProperty(
            nameof(MapContext.Current),
            BindingFlags.Public | BindingFlags.Static)!;
        var parametersProp = typeof(MapContext).GetProperty(
            nameof(MapContext.Parameters))!;
        var itemProp = parametersProp.PropertyType.GetProperty("Item")!;

        return Expression.MakeIndex(
            Expression.Property(Expression.Property(null, currentProp), parametersProp),
            itemProp,
            arguments);
    }

    private static Expression BuildMapContextParametersCall(IList<Expression> arguments)
    {
        var currentProp = typeof(MapContext).GetProperty(
            nameof(MapContext.Current),
            BindingFlags.Public | BindingFlags.Static)!;
        var parametersProp = typeof(MapContext).GetProperty(
            nameof(MapContext.Parameters))!;
        var itemMethod = parametersProp.PropertyType.GetMethod(
            "get_Item", BindingFlags.Public | BindingFlags.Instance)!;

        var parameters = Expression.Property(Expression.Property(null, currentProp), parametersProp);
        return Expression.Call(parameters, itemMethod, arguments);
    }

    private void CollectKeys(IList<Expression> arguments)
    {
        foreach (var arg in arguments)
        {
            if (arg is ConstantExpression { Value: string key })
            {
                _keys.Add(key);
            }
        }
    }
}
