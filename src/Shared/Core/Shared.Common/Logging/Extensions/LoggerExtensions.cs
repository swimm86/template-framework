// ----------------------------------------------------------------------------------------------
// <copyright file="LoggerExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Shared.Common.Logging.Extensions;

/// <summary>
/// Методы расширения для <see cref="ILogger"/>: логирование выполнения задач с автоматическим отслеживанием начала, завершения и ошибок.
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Выполняет асинхронную задачу с логированием её начала, окончания и ошибок.
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения задачи.</typeparam>
    /// <param name="logger">Экземпляр <see cref="ILogger"/> для работы с логированием.</param>
    /// <param name="action">Асинхронная функция, результат которой будет возвращён.</param>
    /// <param name="methodName">Имя вызывающего метода (определяется автоматически через <see cref="CallerMemberNameAttribute"/>).</param>
    /// <param name="processDescription">Описание процесса, который необходимо залогировать.</param>
    /// <param name="logProcessedTime">Признак того, что необходимо залогировать время обработки.</param>
    /// <param name="logLevel">Уровень логирования.</param>
    /// <returns>Результат выполнения асинхронной функции.</returns>
    public static async Task<T> LogTaskAsync<T>(
        this ILogger? logger,
        Func<Task<T>> action,
        [CallerMemberName] string? methodName = null,
        string? processDescription = null,
        bool logProcessedTime = true,
        LogLevel logLevel = LogLevel.Information)
    {
        var process = string.IsNullOrWhiteSpace(processDescription) ? $"'{methodName}'" : processDescription;
        var stopwatch = logProcessedTime ? Stopwatch.StartNew() : null;
        try
        {
            LogStarted(logger, process, logLevel);
            var result = await action();
            LogCompleted(logger, process, logLevel);
            return result;
        }
        catch (Exception ex)
        {
            LogFailed(logger, process, ex);
            logLevel = LogLevel.Error;
            throw;
        }
        finally
        {
            LogElapsed(logger, process, logLevel, stopwatch);
        }
    }

    /// <summary>
    /// Выполняет асинхронную задачу без возвращаемого значения с логированием её начала, окончания и ошибок.
    /// </summary>
    /// <param name="action">Синхронное действие для выполнения.</param>
    /// <inheritdoc cref="LogTaskAsync{T}" />
    /// <param name="logger"/><param name="cancellationToken"/><param name="methodName"/>
    /// <param name="processDescription"/><param name="logProcessedTime"/><param name="logLevel"/>
    public static Task LogTaskAsync(
        this ILogger? logger,
        Func<Task> action,
        CancellationToken cancellationToken,
        [CallerMemberName] string? methodName = null,
        string? processDescription = null,
        bool logProcessedTime = true,
        LogLevel logLevel = LogLevel.Information)
    {
        return logger.LogTaskAsync(
            async () =>
            {
                await action().WaitAsync(cancellationToken);
                return default(object);
            },
            methodName,
            processDescription,
            logProcessedTime,
            logLevel);
    }

    /// <summary>
    /// Выполняет синхронную задачу с логированием её начала, окончания и ошибок.
    /// </summary>
    /// <param name="action">Синхронная функция, результат которой будет возвращён.</param>
    /// <returns>Результат выполнения синхронной функции.</returns>
    /// <inheritdoc cref="LogTaskAsync{T}" />
    /// <param name="logger"/><param name="methodName"/>
    /// <param name="processDescription"/><param name="logProcessedTime"/><param name="logLevel"/>
    public static T LogTask<T>(
        this ILogger? logger,
        Func<T> action,
        [CallerMemberName] string? methodName = null,
        string? processDescription = null,
        bool logProcessedTime = true,
        LogLevel logLevel = LogLevel.Information)
    {
        return logger.LogTaskAsync(
                () => Task.FromResult(action()),
                methodName,
                processDescription,
                logProcessedTime,
                logLevel)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Выполняет синхронное действие с логированием его начала, окончания и ошибок.
    /// </summary>
    /// <param name="action">Асинхронное действие для выполнения.</param>
    /// <inheritdoc cref="LogTaskAsync{T}" />
    /// <param name="logger"/><param name="methodName"/>
    /// <param name="processDescription"/><param name="logProcessedTime"/><param name="logLevel"/>
    public static void LogTask(
        this ILogger? logger,
        Action action,
        [CallerMemberName] string? methodName = null,
        string? processDescription = null,
        bool logProcessedTime = true,
        LogLevel logLevel = LogLevel.Information)
    {
        logger.LogTask<object?>(
            () =>
            {
                action();
                return null;
            },
            methodName,
            processDescription,
            logProcessedTime,
            logLevel);
    }

    /// <summary>
    /// Записывает structured-logging сообщение о начале выполнения процесса.
    /// Используется как точка переиспользования между <see cref="LogTaskAsync{T}"/>
    /// и <see cref="Attributes.LogMethodAttribute"/>.
    /// </summary>
    /// <param name="logger"><inheritdoc cref="LogTaskAsync{T}" path="/param[@name='logger']"/></param>
    /// <param name="process">Имя процесса.</param>
    /// <param name="logLevel"><inheritdoc cref="LogTaskAsync{T}" path="/param[@name='logLevel']"/></param>
    internal static void LogStarted(
        this ILogger? logger,
        string process,
        LogLevel logLevel) =>
        logger?.Log(logLevel, LogMessages.Started, process);

    /// <summary>
    /// Записывает structured-logging сообщение об успешном завершении процесса.
    /// Используется как точка переиспользования между <see cref="LogTaskAsync{T}"/>
    /// и <see cref="Attributes.LogMethodAttribute"/>.
    /// </summary>
    /// <param name="logger"><inheritdoc cref="LogTaskAsync{T}" path="/param[@name='logger']"/></param>
    /// <param name="process"><inheritdoc cref="LogStarted" path="/param[@name='process']"/></param>
    /// <param name="logLevel"><inheritdoc cref="LogTaskAsync{T}" path="/param[@name='logLevel']"/></param>
    internal static void LogCompleted(
        this ILogger? logger,
        string process,
        LogLevel logLevel) =>
        logger?.Log(logLevel, LogMessages.Completed, process);

    /// <summary>
    /// Записывает structured-logging сообщение об ошибке выполнения процесса,
    /// включая объект исключения для сохранения стека вызовов.
    /// Используется как точка переиспользования между <see cref="LogTaskAsync{T}"/>
    /// и <see cref="Attributes.LogMethodAttribute"/>.
    /// </summary>
    /// <param name="logger"><inheritdoc cref="LogTaskAsync{T}" path="/param[@name='logger']"/></param>
    /// <param name="process"><inheritdoc cref="LogStarted" path="/param[@name='process']"/></param>
    /// <param name="exception">Перехваченное исключение.</param>
    internal static void LogFailed(
        this ILogger? logger,
        string process,
        Exception exception) =>
        logger?.LogError(exception, LogMessages.Failed, process);

    /// <summary>
    /// Останавливает <paramref name="stopwatch"/> и записывает сообщение о времени выполнения процесса.
    /// Если <paramref name="stopwatch"/> равен <c>null</c>, вызов игнорируется.
    /// Используется как точка переиспользования между <see cref="LogTaskAsync{T}"/>
    /// и <see cref="Attributes.LogMethodAttribute"/>.
    /// </summary>
    /// <param name="logger"><inheritdoc cref="LogTaskAsync{T}" path="/param[@name='logger']"/></param>
    /// <param name="process"><inheritdoc cref="LogStarted" path="/param[@name='process']"/></param>
    /// <param name="logLevel"><inheritdoc cref="LogTaskAsync{T}" path="/param[@name='logLevel']"/></param>
    /// <param name="stopwatch">Таймер выполнения; если <c>null</c>, сообщение не записывается.</param>
    internal static void LogElapsed(
        this ILogger? logger,
        string process,
        LogLevel logLevel,
        Stopwatch? stopwatch)
    {
        if (stopwatch is null)
        {
            return;
        }

        stopwatch.Stop();
        logger?.Log(
            logLevel,
            LogMessages.Elapsed,
            process,
            stopwatch.ElapsedMilliseconds);
    }
}
