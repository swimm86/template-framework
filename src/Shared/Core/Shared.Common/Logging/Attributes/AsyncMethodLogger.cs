// ----------------------------------------------------------------------------------------------
// <copyright file="AsyncMethodLogger.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Shared.Common.Logging.Attributes;

/// <summary>
/// Вспомогательный класс для оборачивания async-задач (<see cref="Task"/> / <see cref="Task{T}"/>)
/// в цепочку с колбэками логирования.
/// Кеширует скомпилированные делегаты через Expression Trees для каждого уникального типа <c>T</c>,
/// избегая overhead рефлексии при повторных вызовах.
/// </summary>
internal static class AsyncMethodLogger
{
    private static readonly ConcurrentDictionary<
        Type,
        Func<object, Action, Action<Exception>, object>> WrapperCache = new();

    /// <summary>
    /// Оборачивает <paramref name="returnValue"/> (Task / Task&lt;T&gt;) в обёртку с логированием.
    /// Для значений, не являющихся Task, немедленно вызывает <paramref name="onCompleted"/>.
    /// </summary>
    /// <param name="returnValue">Возвращаемое значение метода.</param>
    /// <param name="onCompleted">Колбэк, вызываемый при успешном завершении.</param>
    /// <param name="onFailed">Колбэк, вызываемый при ошибке.</param>
    /// <returns>Обёрнутый Task или исходное значение.</returns>
    internal static object Wrap(
        object returnValue,
        Action onCompleted,
        Action<Exception> onFailed)
    {
        var returnType = returnValue.GetType();

        if (returnType == typeof(Task))
            return WrapAsync(returnValue, onCompleted, onFailed);

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = returnType.GetGenericArguments()[0];
            var wrapper = WrapperCache.GetOrAdd(resultType, BuildWrapper);
            return wrapper(returnValue, onCompleted, onFailed);
        }

        onCompleted();
        return returnValue;
    }

    private static async Task WrapAsync(
        object returnValue,
        Action onCompleted,
        Action<Exception> onFailed)
    {
        try
        {
            await (Task)returnValue;
            onCompleted();
        }
        catch (Exception ex)
        {
            onFailed(ex);
            throw;
        }
    }

    private static async Task<T> WrapAsync<T>(
        object returnValue,
        Action onCompleted,
        Action<Exception> onFailed)
    {
        try
        {
            var result = await (Task<T>)returnValue;
            onCompleted();
            return result;
        }
        catch (Exception ex)
        {
            onFailed(ex);
            throw;
        }
    }

    private static Func<object, Action, Action<Exception>, object> BuildWrapper(Type resultType)
    {
        var method = typeof(AsyncMethodLogger)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Single(m => m is { Name: nameof(WrapAsync), IsGenericMethodDefinition: true })
            .MakeGenericMethod(resultType);

        var pValue = Expression.Parameter(typeof(object), "returnValue");
        var pCompleted = Expression.Parameter(typeof(Action), "onCompleted");
        var pFailed = Expression.Parameter(typeof(Action<Exception>), "onFailed");

        return Expression.Lambda<Func<object, Action, Action<Exception>, object>>(
            Expression.Convert(
                Expression.Call(method, pValue, pCompleted, pFailed),
                typeof(object)),
            pValue,
            pCompleted,
            pFailed).Compile();
    }
}
