// ----------------------------------------------------------------------------------------------
// <copyright file="LambdaTypeConverter.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using System.Reflection;

namespace Shared.Infrastructure.Mapper.AutoMapper.Adapters.Helpers;

/// <summary>
/// Утилиты преобразования <see cref="LambdaExpression"/> к нужному generic-типу
/// и поиска методов по иерархии интерфейсов AutoMapper.
/// </summary>
internal static class LambdaTypeConverter
{
    /// <summary>
    /// Приводит <see cref="LambdaExpression"/> к нужному generic-типу <c>Expression&lt;Func&lt;X,Y&gt;&gt;</c>.
    /// </summary>
    /// <param name="source">Исходное лямбда-выражение.</param>
    /// <param name="targetExpressionType">Тип <c>Expression&lt;Func&lt;X,Y&gt;&gt;</c>, к которому нужно привести.</param>
    /// <returns>Преобразованное лямбда-выражение требуемого типа.</returns>
    public static LambdaExpression ConvertLambdaType(
        LambdaExpression source,
        Type targetExpressionType)
    {
        // Извлекаем целевые типы Func<,>: (TSource) => TTarget.
        var targetFuncType = targetExpressionType.GetGenericArguments()[0];
        var targetReturnType = targetFuncType.GetGenericArguments()[1];
        var targetParamType = targetFuncType.GetGenericArguments()[0];

        // Fast path: типы уже совпадают — вернуть source без изменений.
        // Условие: возвращаемый тип равен AND (тип самого Expression совпадает ИЛИ параметр совместим).
        if (source.ReturnType == targetReturnType &&
            (source.GetType() == targetExpressionType || source.Parameters[0].Type == targetParamType))
        {
            return source;
        }

        // Типы совместимы через присваивание (upcast или downcast) — оборачиваем body в Expression.Convert.
        if (targetReturnType.IsAssignableFrom(source.ReturnType)
            || source.ReturnType.IsAssignableFrom(targetReturnType))
        {
            var convertedBody = Expression.Convert(source.Body, targetReturnType);
            return Expression.Lambda(targetFuncType, convertedBody, source.Parameters[0]);
        }

        // Типы несовместимы — конвертация невозможна.
        throw new InvalidOperationException(
            $"Cannot convert lambda with return type '{source.ReturnType}' to target return type '{targetReturnType}'.");
    }

    /// <summary>
    /// Находит все методы с указанным именем среди самого интерфейса и всех его базовых интерфейсов.
    /// </summary>
    /// <param name="interfaceType">Тип интерфейса, в котором ищутся методы.</param>
    /// <param name="name">Имя метода.</param>
    /// <returns>Массив <see cref="MethodInfo"/> найденных методов.</returns>
    public static MethodInfo[] FindMethodsOnHierarchy(Type interfaceType, string name)
    {
        var result = new List<MethodInfo>();
        var interfaces = new List<Type>(interfaceType.GetInterfaces()) { interfaceType };
        foreach (var @interface in interfaces)
        {
            result.AddRange(@interface.GetMethods().Where(m => m.Name == name));
        }

        return [.. result];
    }

    /// <summary>
    /// Находит один метод с указанным именем среди самого интерфейса и всех его базовых интерфейсов.
    /// </summary>
    /// <param name="interfaceType">Тип интерфейса, в котором ищется метод.</param>
    /// <param name="name">Имя метода.</param>
    /// <param name="predicate">Дополнительный фильтр.</param>
    /// <returns>Найденный <see cref="MethodInfo"/> или <c>null</c>.</returns>
    public static MethodInfo? FindSingleMethodOnHierarchy(
        Type interfaceType,
        string name,
        Func<MethodInfo, bool> predicate)
    {
        var interfaces = new List<Type>(interfaceType.GetInterfaces()) { interfaceType };
        return interfaces
            .Select(@interface => @interface.GetMethods().FirstOrDefault(m => m.Name == name && predicate(m)))
            .OfType<MethodInfo>()
            .FirstOrDefault();
    }
}
