// ----------------------------------------------------------------------------------------------
// <copyright file="LambdaTypeConverter.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;

namespace Shared.Infrastructure.Mapper.Mapster.Adapters.Helpers;

/// <summary>
/// Утилиты преобразования <see cref="LambdaExpression"/> к нужному generic-типу <c>Func&lt;X,Y&gt;</c>.
/// </summary>
internal static class LambdaTypeConverter
{
    /// <summary>
    /// Преобразует <see cref="LambdaExpression"/> к нужному generic-типу <c>Func&lt;X,Y&gt;</c>.
    /// </summary>
    /// <param name="source">Исходное лямбда-выражение.</param>
    /// <param name="targetFuncType">Целевой тип <c>Func&lt;X,Y&gt;</c>.</param>
    /// <returns>Преобразованное лямбда-выражение требуемого типа.</returns>
    /// <exception cref="InvalidOperationException">Выбрасывается, если возвращаемый тип исходного лямбда-выражения несовместим с возвращаемым типом целевого <c>Func</c>.</exception>
    public static LambdaExpression ConvertLambdaType(LambdaExpression source, Type targetFuncType)
    {
        var targetReturnType = targetFuncType.GetGenericArguments()[1];
        var targetParamType = targetFuncType.GetGenericArguments()[0];

        if (source.ReturnType == targetReturnType && source.Parameters[0].Type == targetParamType)
        {
            return source;
        }

        if (targetReturnType.IsAssignableFrom(source.ReturnType)
            || source.ReturnType.IsAssignableFrom(targetReturnType))
        {
            var convertedBody = Expression.Convert(source.Body, targetReturnType);
            return Expression.Lambda(targetFuncType, convertedBody, source.Parameters);
        }

        throw new InvalidOperationException(
            $"Cannot convert lambda with return type '{source.ReturnType}' to target return type '{targetReturnType}'.");
    }
}
