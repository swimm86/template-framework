// ----------------------------------------------------------------------------------------------
// <copyright file="LogMethodAttribute.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Diagnostics;
using MethodBoundaryAspect.Fody.Attributes;
using Microsoft.Extensions.Logging;
using Shared.Common.Logging.Extensions;

namespace Shared.Common.Logging.Attributes;

/// <summary>
/// AOP-атрибут для автоматического логирования начала, окончания (или ошибки) и времени выполнения метода.
/// Поддерживает как синхронные методы, так и async-методы, возвращающие <see cref="Task"/> / <see cref="Task{TResult}"/>.
/// </summary>
/// <remarks>
/// Использует IL-weaving через MethodBoundaryAspect.Fody.
/// Для работы требует предварительного вызова <see cref="LoggingServiceAccessor.Configure"/> при старте приложения.
/// <para>
/// Поведение логирования можно переопределить в производном атрибуте через защищённые виртуальные методы
/// <see cref="OnLogStarted"/>, <see cref="OnLogCompleted"/>, <see cref="OnLogFailed"/>, <see cref="OnLogElapsed"/>.
/// Все они имеют доступ к логгеру, имени процесса и уровню логирования, что позволяет добавлять
/// произвольные structured-logging свойства (например, correlation ID, tenant ID и т.д.).
/// </para>
/// <para>
/// <b>Архитектурное примечание:</b> для получения <see cref="ILogger"/> используется паттерн Service Locator
/// (<see cref="LoggingServiceAccessor"/>). Это вынужденный компромисс: атрибуты в .NET не поддерживают
/// инъекцию зависимостей через конструктор, а IL-weaving Fody исключает использование DI-прокси.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [LogMethod]
/// public async Task&lt;IEnumerable&lt;DataDto&gt;&gt; GetAllAsync(CancellationToken ct)
/// {
///     return await repository.GetAllAsync(ct);
/// }
///
/// [LogMethod(ProcessDescription = "Импорт данных", LogProcessedTime = true)]
/// public void ImportData(byte[] data)
/// {
///     repository.Import(data);
/// }
/// </code>
/// </example>
/// <param name="processDescription">
/// Описание процесса для логирования. Если не задано, используется имя метода.
/// </param>
/// <param name="logProcessedTime">
/// Признак того, что необходимо залогировать время обработки. По умолчанию <c>true</c>.
/// </param>
/// <param name="logLevel">Уровень логирования.</param>
[ProvideAspectRole("Logging")]
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class LogMethodAttribute(
    string processDescription = "",
    bool logProcessedTime = true,
    LogLevel logLevel = LogLevel.Information)
    : OnMethodBoundaryAspect
{
    private sealed record ExecutionContext(
        ILogger? Logger,
        string Process,
        Stopwatch? Stopwatch);

    /// <inheritdoc />
    public override void OnEntry(
        MethodExecutionArgs args)
    {
        var logger = GetLogger(args);
        var process = GetProcessName(args);
        var stopwatch = logProcessedTime ? Stopwatch.StartNew() : null;

        args.MethodExecutionTag = new ExecutionContext(logger, process, stopwatch);

        OnLogStarted(logger, process);
    }

    /// <inheritdoc />
    public override void OnExit(
        MethodExecutionArgs args)
    {
        if (args.Exception is not null)
        {
            return;
        }

        var ctx = (ExecutionContext)args.MethodExecutionTag;
        if (args.ReturnValue is Task)
        {
            args.ReturnValue = AsyncMethodLogger.Wrap(
                args.ReturnValue,
                () => DoLogCompleted(ctx.Logger, ctx.Process, ctx.Stopwatch),
                ex => DoLogFailed(ctx.Logger, ctx.Process, ex, ctx.Stopwatch));
            return;
        }

        DoLogCompleted(ctx.Logger, ctx.Process, ctx.Stopwatch);
    }

    /// <inheritdoc />
    public override void OnException(
        MethodExecutionArgs args)
    {
        var ctx = (ExecutionContext)args.MethodExecutionTag;
        DoLogFailed(ctx.Logger, ctx.Process, args.Exception, ctx.Stopwatch);
    }

    /// <summary>
    /// Выполняет логирование начала выполнения метода.
    /// Переопределите для добавления дополнительных structured-logging свойств.
    /// </summary>
    /// <param name="logger">Экземпляр логгера.</param>
    /// <param name="process">Имя процесса.</param>
    protected virtual void OnLogStarted(ILogger? logger, string process) =>
        logger?.LogStarted(process, logLevel);

    /// <summary>
    /// Выполняет логирование успешного завершения метода.
    /// Переопределите для добавления дополнительных structured-logging свойств.
    /// </summary>
    /// <param name="logger">Экземпляр логгера.</param>
    /// <param name="process">Имя процесса.</param>
    protected virtual void OnLogCompleted(ILogger? logger, string process) =>
        logger?.LogCompleted(process, logLevel);

    /// <summary>
    /// Выполняет логирование ошибки выполнения метода.
    /// Переопределите для добавления дополнительных structured-logging свойств.
    /// </summary>
    /// <param name="logger">Экземпляр логгера.</param>
    /// <param name="process">Имя процесса.</param>
    /// <param name="exception">Перехваченное исключение.</param>
    protected virtual void OnLogFailed(ILogger? logger, string process, Exception exception) =>
        logger?.LogFailed(process, exception);

    /// <summary>
    /// Выполняет логирование времени выполнения метода.
    /// Переопределите для изменения формата или добавления дополнительных свойств.
    /// </summary>
    /// <param name="logger">Экземпляр логгера.</param>
    /// <param name="process">Имя процесса.</param>
    /// <param name="level">Уровень логирования.</param>
    /// <param name="stopwatch">Таймер.</param>
    protected virtual void OnLogElapsed(
        ILogger? logger,
        string process,
        LogLevel level,
        Stopwatch? stopwatch) =>
        logger?.LogElapsed(process, level, stopwatch);

    private static ILogger? GetLogger(MethodExecutionArgs args)
    {
        var declaringType = args.Method.DeclaringType;
        return declaringType is not null
            ? LoggingServiceAccessor.GetLogger(declaringType)
            : null;
    }

    private void DoLogCompleted(
        ILogger? logger,
        string process,
        Stopwatch? stopwatch)
    {
        OnLogCompleted(logger, process);
        OnLogElapsed(
            logger,
            process,
            logLevel,
            stopwatch);
    }

    private void DoLogFailed(
        ILogger? logger,
        string process,
        Exception exception,
        Stopwatch? stopwatch)
    {
        OnLogFailed(logger, process, exception);
        OnLogElapsed(logger, process, LogLevel.Error, stopwatch);
    }

    private string GetProcessName(MethodExecutionArgs args)
    {
        if (!string.IsNullOrWhiteSpace(processDescription))
        {
            return processDescription;
        }

        var className = args.Method.DeclaringType?.Name;
        return className is not null
            ? $"'{className}.{args.Method.Name}'"
            : $"'{args.Method.Name}'";
    }
}
