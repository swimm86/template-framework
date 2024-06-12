// ----------------------------------------------------------------------------------------------
// <copyright file="LoggerExtensions.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Shared.Common.Extensions;

/// <summary>
/// Расширение для <see cref="ILogger"/>.
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Выполняет асинхронную задачу с логированием её начала, окончания и ошибок.
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения задачи.</typeparam>
    /// <param name="logger">Экземпляр логгера.</param>
    /// <param name="action">Асинхронная функция, которая будет выполнена.</param>
    /// <param name="token">Токен отмены задачи.</param>
    /// <param name="methodName">Имя метода, вызывающего логирование; автоматически определяется.</param>
    /// <returns>Результат выполнения асинхронной функции.</returns>
    public static async Task<T> LogTaskAsync<T>(
        this ILogger? logger,
        Func<Task<T>> action,
        CancellationToken token,
        [CallerMemberName] string? methodName = null)
    {
        try
        {
            logger?.LogInformation("'{methodName}' запущен.", methodName);
            var result = await action().WaitAsync(token).ConfigureAwait(false);
            logger?.LogInformation("'{methodName}' выполнен.", methodName);
            return result;
        }
        catch
        {
            logger?.LogError("'{methodName}' прошла ошибка.", methodName);
            throw;
        }
    }

    /// <summary>
    /// Выполняет асинхронную задачу без возвращаемого значения с логированием её начала, окончания и ошибок.
    /// </summary>
    /// <param name="logger">Экземпляр логгера.</param>
    /// <param name="action">Асинхронная действие, которое будет выполнено.</param>
    /// <param name="token">Токен отмены задачи.</param>
    /// <param name="methodName">Имя метода, вызывающего логирование; автоматически определяется.</param>
    /// <returns>Task представляющий асинхронную операцию.</returns>
    public static Task LogTaskAsync(
        this ILogger? logger,
        Func<Task> action,
        CancellationToken token,
        [CallerMemberName] string? methodName = null)
    {
        return LogTaskAsync(
            logger,
            async () =>
            {
                await action().WaitAsync(token).ConfigureAwait(false);
                return Task.CompletedTask;
            },
            token,
            methodName);
    }

    /// <summary>
    /// Выполняет синхронную задачу с логированием её начала, окончания и ошибок.
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения задачи.</typeparam>
    /// <param name="logger">Экземпляр логгера.</param>
    /// <param name="action">Синхронная функция, которая будет выполнена.</param>
    /// <param name="methodName">Имя метода, вызывающего логирование; автоматически определяется.</param>
    /// <returns>Результат выполнения синхронной функции.</returns>
    public static T LogTask<T>(
        this ILogger? logger,
        Func<T> action,
        [CallerMemberName] string? methodName = null)
    {
        return LogTaskAsync(logger, () => Task.FromResult(action()), default, methodName)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Выполняет синхронное действие с логированием его начала, окончания и ошибок.
    /// </summary>
    /// <param name="logger">Экземпляр логгера.</param>
    /// <param name="action">Синхронное действие, которое будет выполнено.</param>
    /// <param name="methodName">Имя метода, вызывающего логирование; автоматически определяется.</param>
    public static void LogTask(
        this ILogger? logger,
        Action action,
        [CallerMemberName] string? methodName = null)
    {
        LogTask<object?>(
            logger,
            () =>
            {
                action();
                return default;
            },
            methodName);
    }
}
