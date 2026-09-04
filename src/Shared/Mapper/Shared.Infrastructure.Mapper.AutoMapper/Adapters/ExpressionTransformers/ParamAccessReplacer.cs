// ----------------------------------------------------------------------------------------------
// <copyright file="ParamAccessReplacer.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;

namespace Shared.Infrastructure.Mapper.AutoMapper.Adapters.ExpressionTransformers;

/// <summary>
/// Заменяет обращения к <c>params["key"]</c> на обращения к полям <c>holder.key</c>,
/// а параметр <c>params</c> — на <c>ConstantExpression(holder)</c>.
/// </summary>
internal sealed class ParamAccessReplacer(
    ParameterExpression paramsParam,
    Expression holder)
    : ExpressionVisitor
{
    /// <inheritdoc />
    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node == paramsParam ? holder : base.VisitParameter(node);
    }

    /// <inheritdoc />
    protected override Expression VisitIndex(IndexExpression node)
    {
        if (node.Object == paramsParam && node.Arguments[0] is ConstantExpression keyConst)
        {
            return Expression.Field(holder, (string)keyConst.Value!);
        }

        return base.VisitIndex(node);
    }

    /// <inheritdoc />
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Object == paramsParam
            && node.Method.Name == "get_Item"
            && node.Arguments[0] is ConstantExpression keyConst)
        {
            return Expression.Field(holder, (string)keyConst.Value!);
        }

        return base.VisitMethodCall(node);
    }
}
