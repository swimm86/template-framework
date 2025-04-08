// ----------------------------------------------------------------------------------------------
// <copyright file="LoggerExtensions.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Diagnostics;
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
    /// <param name="processDescription">Описание процесса, который необходимо залогировать.</param>
    /// <param name="logProcessedTime">Признак того, что необходимо залогировать время обработки.</param>
    /// <returns>Результат выполнения асинхронной функции.</returns>
    public static async Task<T> LogTaskAsync<T>(
        this ILogger? logger,
        Func<Task<T>> action,
        CancellationToken token,
        [CallerMemberName] string? methodName = null,
        string? processDescription = null,
        bool logProcessedTime = true)
    {
        var process = string.IsNullOrWhiteSpace(processDescription) ? $"'{methodName}'" : processDescription;
        var stopwatch = logProcessedTime ? Stopwatch.StartNew() : null;
        try
        {
            logger?.LogInformation("{process} started.", process);
            var result = await action();
            logger?.LogInformation("{process} completed.", process);
            return result;
        }
        catch
        {
            logger?.LogError("{process} failed.", process);
            throw;
        }
        finally
        {
            if (stopwatch is not null)
            {
                logger?.LogInformation(
                    "{process} processed time: {time}ms.",
                    process,
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }

    /// <summary>
    /// Выполняет асинхронную задачу без возвращаемого значения с логированием её начала, окончания и ошибок.
    /// </summary>
    /// <param name="logger">Экземпляр логгера.</param>
    /// <param name="action">Асинхронная действие, которое будет выполнено.</param>
    /// <param name="token">Токен отмены задачи.</param>
    /// <param name="methodName">Имя метода, вызывающего логирование; автоматически определяется.</param>
    /// <param name="processDescription">Описание процесса, который необходимо залогировать.</param>
    /// <param name="logProcessedTime">Признак того, что необходимо залогировать время обработки.</param>
    /// <returns>Task представляющий асинхронную операцию.</returns>
    public static Task LogTaskAsync(
        this ILogger? logger,
        Func<Task> action,
        CancellationToken token,
        [CallerMemberName] string? methodName = null,
        string? processDescription = null,
        bool logProcessedTime = true)
    {
        return logger.LogTaskAsync(
            async () =>
            {
                await action().WaitAsync(token);
                return Task.CompletedTask;
            },
            token,
            methodName,
            processDescription,
            logProcessedTime);
    }

    /// <summary>
    /// Выполняет синхронную задачу с логированием её начала, окончания и ошибок.
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения задачи.</typeparam>
    /// <param name="logger">Экземпляр логгера.</param>
    /// <param name="action">Синхронная функция, которая будет выполнена.</param>
    /// <param name="methodName">Имя метода, вызывающего логирование; автоматически определяется.</param>
    /// <param name="processDescription">Описание процесса, который необходимо залогировать.</param>
    /// <param name="logProcessedTime">Признак того, что необходимо залогировать время обработки.</param>
    /// <returns>Результат выполнения синхронной функции.</returns>
    public static T LogTask<T>(
        this ILogger? logger,
        Func<T> action,
        [CallerMemberName] string? methodName = null,
        string? processDescription = null,
        bool logProcessedTime = true)
    {
        return logger.LogTaskAsync(
                () => Task.FromResult(action()),
                default,
                methodName,
                processDescription,
                logProcessedTime)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Выполняет синхронное действие с логированием его начала, окончания и ошибок.
    /// </summary>
    /// <param name="logger">Экземпляр логгера.</param>
    /// <param name="action">Синхронное действие, которое будет выполнено.</param>
    /// <param name="methodName">Имя метода, вызывающего логирование; автоматически определяется.</param>
    /// <param name="processDescription">Описание процесса, который необходимо залогировать.</param>
    /// <param name="logProcessedTime">Признак того, что необходимо залогировать время обработки.</param>
    public static void LogTask(
        this ILogger? logger,
        Action action,
        [CallerMemberName] string? methodName = null,
        string? processDescription = null,
        bool logProcessedTime = true)
    {
        logger.LogTask<object?>(
            () =>
            {
                action();
                return default;
            },
            methodName,
            processDescription,
            logProcessedTime);
    }
}
