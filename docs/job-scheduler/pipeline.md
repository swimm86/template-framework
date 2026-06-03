# Pipeline фоновых задач

**Сборка:** `Shared.Application.Core.dll`
**Namespace:** `Shared.Application.Core.Job.Pipeline`
**Исходники:** `src/Shared/Core/Shared.Application.Core/Job/Pipeline/`

---

## Обзор

Pipeline фоновых задач — это цепочка посредников, через которую проходит каждая итерация фоновой задачи перед `ExecuteAsync`. Аналогично [Pipeline Behaviors](../pipeline-behaviors.md) для MediatR, pipeline Job Scheduler решает сквозные задачи: логирование, correlation ID, повторные попытки. Каждая задача решается отдельным классом, реализующим `IScheduledJobMiddleware` — и в фоновой задаче нет ни строчки инфраструктурного кода.

## Контракт `IScheduledJobMiddleware`

```csharp
public interface IScheduledJobMiddleware
{
    Task InvokeAsync(ScheduledJobContext context, ScheduledJobDelegate next);
}

public delegate Task ScheduledJobDelegate(ScheduledJobContext context);

public sealed class ScheduledJobContext(
    string jobKey,
    IServiceProvider serviceProvider,
    CancellationToken cancellationToken)
{
    public string JobKey { get; } = jobKey;
    public Type? JobType { get; init; }
    public string? ServiceKey { get; init; }
    public Func<IServiceProvider, CancellationToken, Task>? Action { get; init; }
    public IServiceProvider ServiceProvider { get; } = serviceProvider;
    public CancellationToken CancellationToken { get; } = cancellationToken;
    public IDictionary<string, object?> Properties { get; } = new Dictionary<string, object?>();
}
```

`ScheduledJobContext` создаётся **один раз** на итерацию в адаптере (`QuartzScheduledJobAdapter` / `HangfireScheduledJobAdapter`) и пробрасывается через все посредники до терминального вызова.

## Встроенные middleware

`AddJobs(opts => ...)` автоматически регистрирует три middleware в DI:

| Посредник | Назначение |
|------------|-----------|
| `LoggingMiddleware` | Логирует start/complete + errors через `LogTaskAsync` |
| `CorrelationIdMiddleware` | Устанавливает `JobCorrelationContext` на время итерации |
| `RetryMiddleware` | Повторная попытка в процессе выполнения (с задержкой) при исключениях |

### `LoggingMiddleware`

```csharp
public sealed class LoggingMiddleware(ILogger<LoggingMiddleware> logger) : IScheduledJobMiddleware
{
    public Task InvokeAsync(ScheduledJobContext context, ScheduledJobDelegate next)
    {
        return logger.LogTaskAsync(action: () => next(context), CancellationToken.None);
    }
}
```

| Аспект | Описание |
|--------|----------|
| **Что логирует** | `Information` start + complete + duration через `LogTaskAsync` |
| **Исключения** | Пробрасывает дальше — `LogTaskAsync` логирует Error автоматически |
| **Где взять logger** | DI (`ILogger<LoggingMiddleware>`) |

### `CorrelationIdMiddleware`

```csharp
public sealed class CorrelationIdMiddleware(ILogger<CorrelationIdMiddleware> logger) : IScheduledJobMiddleware
{
    public async Task InvokeAsync(ScheduledJobContext context, ScheduledJobDelegate next)
    {
        var correlationIdCreated = JobCorrelationContext.TrySetCorrelationId();
        if (!correlationIdCreated)
        {
            logger.LogWarning(
                "Correlation ID already initialized before job '{jobKey}' and not cleared.",
                context.JobKey);
        }
        try
        {
            await next(context);
        }
        finally
        {
            if (correlationIdCreated) JobCorrelationContext.ClearCorrelationId();
        }
    }
}
```

| Аспект | Описание |
|--------|----------|
| **Что делает** | Устанавливает уникальный `JobCorrelationContext.Id` |
| **Зачем** | Distributed tracing: логи и внешние вызовы (HTTP, DB) получают correlation-id |
| **Очистка** | Только если middleware сам создал id (не трогает "чужой") |
| **Связь** | См. [Correlation ID](../correlation-id.md) и `JobCorrelationContext` |

### `RetryMiddleware`

```csharp
public sealed class RetryMiddleware(IOptions<RetryOptions> options, ILogger<RetryMiddleware> logger) : IScheduledJobMiddleware
{
    public async Task InvokeAsync(ScheduledJobContext context, ScheduledJobDelegate next)
    {
        var attempt = 0;
        while (true)
        {
            try { await next(context); return; }
            catch (Exception ex) when (++attempt < options.Value.MaxAttempts)
            {
                logger.LogWarning(ex, "Job {JobKey} failed (attempt {Attempt}/{MaxAttempts}), retrying in {Delay}.",
                    context.JobKey, attempt, options.Value.MaxAttempts, options.Value.Delay);
                await Task.Delay(options.Value.Delay, context.CancellationToken);
            }
        }
    }
}
```

`RetryOptions`:

```csharp
public sealed class RetryOptions
{
    public int MaxAttempts { get; set; } = 3;                       // включая первую
    public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(30);
}
```

| Аспект | Описание |
|--------|----------|
| **Семантика** | Повторная попытка в процессе выполнения внутри одной итерации (НЕ новый cron-тик) |
| **Отличие от QuartzJobWrapper** | Старый `QuartzJobWrapper` повторял попытку через 5 минут отдельным Quartz-триггером; новый — без задержки в планировщике |
| **CancellationToken** | Уважает — если планировщик отменил выполнение, `Task.Delay` бросит `OperationCanceledException` |
| **Настройка** | Зарегистрируйте свой `RetryOptions` в DI (`services.Configure<RetryOptions>(o => ...)`) |

## Порядок регистрации = порядок выполнения

`AddJobs` регистрирует посредников в DI так:

```
1. LoggingMiddleware        (1-й → самый внешний)
2. CorrelationIdMiddleware
3. RetryMiddleware           (3-й → самый внутренний)
```

`ScheduledJobExecutor` собирает цепочку:

```csharp
var pipeline = _middlewares.Reverse()
    .Aggregate((ScheduledJobDelegate)TerminalAsync,
               (next, mw) => ctx => mw.InvokeAsync(ctx, next));
```

Reverse + Aggregate: первый в DI = самый внешний. **Это означает:** `Logging` оборачивает всё (включая retry-attempts), `Correlation` — внутри logging, `Retry` — внутри correlation. Наружу (в лог) попадают только финальные результаты.

## Как добавить свой middleware

### 1. Реализация

```csharp
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Job.Pipeline;

public sealed class MetricsMiddleware(ILogger<MetricsMiddleware> logger) : IScheduledJobMiddleware
{
    public async Task InvokeAsync(ScheduledJobContext context, ScheduledJobDelegate next)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await next(context);
            logger.LogInformation("Job {JobKey} took {Elapsed} ms", context.JobKey, sw.ElapsedMilliseconds);
        }
        catch
        {
            logger.LogWarning("Job {JobKey} failed after {Elapsed} ms", context.JobKey, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
```

### 2. Регистрация

```csharp
services.AddJobs(opts => opts.AddJob<MyJob>(...));

// До Logging (самый внешний) — сначала в списке
services.AddEnumerable(ServiceDescriptor.Singleton<IScheduledJobMiddleware, MetricsMiddleware>());

// Хотите убрать дефолтные — переопределите порядок через Clear+Add:
// services.RemoveAll<IScheduledJobMiddleware>();
// services.AddEnumerable(... в нужном порядке);
```

### 3. Где хранить данные между посредниками

`context.Properties` — словарь для проброса значений:

```csharp
// В одном middleware
context.Properties["UserId"] = GetCurrentUserId();

// В следующем
if (context.Properties.TryGetValue("UserId", out var userId)) { ... }
```

## Входы/выходы middleware

| Посредник | Вход (что получает) | Выход (что делает при успехе) | При исключении |
|------------|---------------------|--------------------------------|-----------------|
| `LoggingMiddleware` | `ScheduledJobContext` | Логирует start → `next()` → complete + duration через `LogTaskAsync` | `LogTaskAsync` логирует Error, пробрасывает |
| `CorrelationIdMiddleware` | `ScheduledJobContext` | `TrySetCorrelationId()` → `next()` → `ClearCorrelationId()` | Clear в `finally`, пробрасывает |
| `RetryMiddleware` | `ScheduledJobContext` | `next()` один раз, return | Цикл: `delay` → повтор до `MaxAttempts` |

## Что НЕ делать в посредниках

- **Не выполнять долгие блокирующие операции** до `next()` — это ломает семантику повторных попыток.
- **Не глотать исключения** без `throw` — повторная попытка не сработает.
- **Не использовать `IServiceProvider` напрямую из посредника** — пробрасывайте через конструктор или `context.ServiceProvider` только если действительно нужен scoped-сервис.

## Связанные документы

| Документ | Описание |
|----------|----------|
| [Job Scheduler (top-level)](../job-scheduler.md) | Обзор |
| [Architecture](architecture.md) | Слои и обоснование |
| [Correlation ID](../correlation-id.md) | `JobCorrelationContext` |
| [Logging](../logging.md) | `LogTask` и `[LogMethod]` |
| [Pipeline Behaviors](../pipeline-behaviors.md) | Аналог для MediatR |
