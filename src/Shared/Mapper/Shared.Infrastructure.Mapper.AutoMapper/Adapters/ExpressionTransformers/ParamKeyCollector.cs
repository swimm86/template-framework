// ----------------------------------------------------------------------------------------------
// <copyright file="ParamKeyCollector.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;

namespace Shared.Infrastructure.Mapper.AutoMapper.Adapters.ExpressionTransformers;

/// <summary>
/// Собирает все ключи, используемые в обращении к <c>params["key"]</c> или <c>params.get_Item("key")</c>.
/// </summary>
internal sealed class ParamKeyCollector(
    ParameterExpression paramsParam)
    : ExpressionVisitor
{
    /// <summary>
    /// Набор уникальных ключей.
    /// </summary>
    public HashSet<string> Keys { get; } = new();

    /// <inheritdoc />
    protected override Expression VisitIndex(IndexExpression node)
    {
        if (node.Object == paramsParam && node.Arguments is [ConstantExpression keyConst])
        {
            Keys.Add((string)keyConst.Value!);
        }

        return base.VisitIndex(node);
    }

    /// <inheritdoc />
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Object == paramsParam
            && node.Method.Name == "get_Item"
            && node.Arguments is [ConstantExpression keyConst])
        {
            Keys.Add((string)keyConst.Value!);
        }

        return base.VisitMethodCall(node);
    }
}
