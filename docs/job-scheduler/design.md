# Job Scheduler: дизайн рефакторинга

**Контекст:** `Shared.Infrastructure.Job.Quartz` в Template/PPS нарушает DIP/OCP. `QuartzJobRegistrar` (статичный) и `QuartzJobWrapper` (публичный базовый) утекают Quartz-типы в домен. Переезд на Hangfire невозможен без правок бизнес-кода.

**Цель:** полная абстракция планировщика в `Shared.Application.Core/Job`. Quartz/Hangfire — адаптеры. Бизнес-код не знает про провайдера.

## Архитектура

```
Shared.Application.Core/Job/                  ← единственный источник правды
├── IScheduledJob.cs                          маркер для классовых джоб
├── JobDefinition.cs                          POCO для лямбда-кейса
├── JobSchedule.cs                            Cron | Flags | OnStartup
├── IJobScheduler.cs                          runtime API
├── JobSchedulerOptions.cs                    контейнер для bootstrapper
├── JobSchedulerBuilder.cs                    fluent: AddJob<TJob>, AddJob(...)
├── JobTriggerFlags.cs                        (уже есть)
├── Pipeline/
│   ├── IScheduledJobMiddleware.cs
│   ├── ScheduledJobContext.cs
│   ├── ScheduledJobDelegate.cs
│   ├── IScheduledJobExecutor.cs
│   └── ScheduledJobExecutor.cs               (internal) собирает pipeline
├── Middlewares/
│   ├── LoggingMiddleware.cs                  log start/complete
│   ├── CorrelationIdMiddleware.cs            JobCorrelationContext
│   └── RetryMiddleware.cs                    in-process retry (Polly-style)
└── Extensions/
    ├── ServiceCollectionExtensions.cs        AddJobs(opts => ...)
    ├── ScheduledJobExtensions.cs             AddScheduledJob<TJob>
    └── CacheJobExtensions.cs                 AddCronCacheJob / AddFlagsCacheJob
```

### Контракты

```csharp
public interface IScheduledJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}

public sealed record JobDefinition(
    string JobKey,
    Func<IServiceProvider, CancellationToken, Task>? Action,
    JobSchedule Schedule,
    Type? JobType = null,
    string? ServiceKey = null);

public abstract record JobSchedule
{
    public sealed record Cron(string Expression) : JobSchedule;
    public sealed record Flags(JobTriggerFlags Flags, TimeSpan SpecificTime) : JobSchedule;
    public sealed record OnStartup() : JobSchedule;
}

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

public interface IScheduledJobExecutor
{
    Task ExecuteAsync(ScheduledJobContext context);
}
```

### Pipeline ordering

Middleware-ы регистрируются в DI как `IEnumerable<IScheduledJobMiddleware>`. Executor строит pipeline так, что **первый зарегистрированный — самый внешний**. Типичный порядок:

1. `CorrelationIdMiddleware` (внешний — `AsyncLocal` correlation-id оборачивает всю итерацию, включая retry-attempts)
2. `LoggingMiddleware` (логирует start/complete, видит финальный результат после retry)
3. `RetryMiddleware` (внутренний — retry только бизнес-логики; активируется только при наличии `RetryOptions` в `ScheduledJobContext`)
4. Terminal: `ctx => ctx.Action(ctx.ServiceProvider, ctx.CancellationToken)`

> Исторически порядок был `Logging → Correlation → Retry`, но в реализации первым ставится `CorrelationId` — `LoggingMiddleware` использует `JobKey` из контекста (через `LogTaskAsync(methodName: context.JobKey)`) и должен видеть его. См. [Pipeline](pipeline.md).

### Retry-семантика (in-process, per-job)

```csharp
public sealed class RetryMiddleware(ILogger<RetryMiddleware> logger)
    : IScheduledJobMiddleware
{
    public async Task InvokeAsync(ScheduledJobContext ctx, ScheduledJobDelegate next)
    {
        var attempt = 0;
        while (true)
        {
            try { await next(ctx); return; }
            catch (Exception ex) when (ctx.RetryOptions is not null && ++attempt < ctx.RetryOptions.MaxAttempts)
            {
                logger.LogWarning(ex, "Job {JobKey} failed (attempt {Attempt}/{MaxAttempts}), retrying in {Delay}.",
                    ctx.JobKey, attempt, ctx.RetryOptions.MaxAttempts, ctx.RetryOptions.Delay);
                await Task.Delay(ctx.RetryOptions.Delay, ctx.CancellationToken);
            }
        }
    }
}
```

`RetryOptions` живут **на уровне отдельной джобы** (в `JobDefinition.RetryOptions` и далее в `ScheduledJobContext.RetryOptions`), а не глобально через `IOptions<RetryOptions>` в DI. Если `ctx.RetryOptions == null`, middleware пропускает повторы — это позволяет включать retry выборочно, не затрагивая остальные джобы. Конкретные значения (`Delay`, `MaxAttempts`) настраиваются в `JobSchedulerBuilder.AddJob<T>(schedule, retryOptions)`.

**Семантическое отличие от текущего QuartzJobWrapper**: текущий перепланирует через 5 минут (новый триггер в планировщике). In-process retry — внутри одной задачи. Для cron-джоб разница минимальна (следующий запуск всё равно по расписанию). Для OnStartup — поведение иное, но это редкий кейс.

## Адаптеры (Infrastructure)

Оба адаптера реализуют один и тот же контракт: транслируют вызов планировщика (Quartz `IJob.Execute` или Hangfire `BackgroundJob`) в наш `IScheduledJob` + pipeline (`IScheduledJobExecutor`). Для этого в каждом адаптере есть ровно один класс роли «adapter», который:

- читает идентификатор/тип/ключ джобы из провайдер-специфичного контекста,
- резолвит `IScheduledJob` из DI (с поддержкой keyed-сервисов),
- строит `ScheduledJobContext` и гонит его через pipeline.

| Провайдер | Класс адаптера | Видимость | DI-жизненный цикл | Как вызывается |
|-----------|---------------|-----------|-------------------|----------------|
| Quartz    | `QuartzScheduledJobAdapter`     | `internal sealed` | Singleton (Quartz создаёт) | Quartz вызывает `IJob.Execute(IJobExecutionContext)` |
| Hangfire  | `HangfireScheduledJobAdapter`   | `internal sealed` | Transient (Hangfire создаёт) | Hangfire вызывает `MethodCallExpression` на `RunScheduledJob` |

Имя «adapter» (а не «bridge») выбрано намеренно: роль классов идентична — переходник между API провайдера и нашим `IScheduledJob`. У Hangfire есть специфика (требуется `MethodCallExpression` для сериализации), но это деталь реализации, а не отдельная роль. Тесты лежат в `QuartzScheduledJobAdapterTests` и `HangfireScheduledJobAdapterTests` соответственно — с симметричным покрытием.

### Quartz

```
Shared.Infrastructure.Job.Quartz/
├── QuartzJobScheduler.cs                  IJobScheduler implementation
├── QuartzJobSchedulerBootstrapper.cs      IHostedService: читает JobSchedulerOptions, регистрирует в IScheduler
├── QuartzScheduledJobAdapter.cs           (internal) IJob-обёртка для IScheduledJob
└── QuartzDependencyInjector.cs            DI-конфиг
```

### Hangfire

```
Shared.Infrastructure.Job.Hangfire/
├── HangfireJobScheduler.cs                IJobScheduler implementation
├── HangfireJobSchedulerBootstrapper.cs    IHostedService
├── HangfireScheduledJobAdapter.cs         (internal) MethodCall-цель для Hangfire сериализации
└── HangfireDependencyInjector.cs          DI-конфиг
```

## Миграция PPS

**4 классовые джобы** в `F:\gpn\back\pps` — `: QuartzJobWrapper` → `: ScheduledJobBase` (или `IScheduledJob`):

- `src/Shared/Job/Shared.Infrastructure.Job.Quartz/Job/DbSeederJob.cs`
- `src/Services/Setter/.../Quartz/Jobs/SyncAnalyticalCodeAttributesWithRoJob.cs`
- `src/Services/Setter/.../Quartz/Jobs/SyncPirProjectsJob.cs`
- `src/Services/Setter/.../Quartz/Jobs/FixAttributeValueJob.cs`

**Паттерн правки каждой джобы** (5 строк):
- удалить `using Quartz;`
- удалить `using Shared.Infrastructure.Job.Quartz;`
- удалить `IServiceProvider serviceProvider` из конструктора
- `: QuartzJobWrapper(serviceProvider, logger)` → `: ScheduledJobBase(logger)` (или `: IScheduledJob` + явная реализация `ExecuteAsync` + `protected abstract Task ProcessAsync` если без базы)
- `protected override async Task ProcessAsync(IJobExecutionContext context, CancellationToken ct)` → `protected override async Task ProcessAsync(CancellationToken ct)`

**Регистраторы** — `.RegisterJob<TJob>(...)` → `AddJobs(opts => opts.AddJob<TJob>(...))`, `.RegisterCacheJob(...)` → `AddCronCacheJob(...)` / `AddFlagsCacheJob(...)`.

## Чего НЕ делать

- Не оставлять `QuartzJobRegistrar` (legacy API).
- Не оставлять `QuartzJobWrapper` как публичный базовый (после миграции — `internal` adapter внутри Quartz-слоя).
- Не делать `Quartz`-типы видимыми в `Application.Core`.
- Не смешивать business logic и cross-cutting concerns в одном классе (принцип pipeline).
- Не использовать `Func<IServiceProvider, CancellationToken, Task>` в публичном API (DI через конструктор + closure для лямбд).

## Verification

- `dotnet build F:\template\Template.sln` — должен быть зелёным
- `dotnet test F:\template\Template.sln` — все unit-тесты на builder, executor, middleware
- `dotnet build F:\gpn\back\pps\Gpn.Contour.Pps.sln` — должен компилироваться без `using Quartz;` в бизнес-коде
- Smoke: `grep -r "using Quartz" F:\gpn\back\pps\src\Services` — должно быть пусто (Quartz типы — только в Infrastructure-слое)
- Hangfire PoC: подключить в Bff из template, доказать что бизнес-код не трогаем
