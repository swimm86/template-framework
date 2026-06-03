# Hangfire Adapter

**Сборка:** `Shared.Infrastructure.Job.Hangfire.dll`
**Namespace:** `Shared.Infrastructure.Job.Hangfire`
**NuGet:** `Hangfire.Core 1.8.14`, `Hangfire.InMemory 0.11.0`, `Hangfire.NetCore 1.8.14`
**Исходники:** `src/Shared/Job/Shared.Infrastructure.Job.Hangfire/`

---

## Обзор

Hangfire Adapter — альтернативная реализация `IJobScheduler` на базе [Hangfire](https://www.hangfire.io/). Адаптер даёт возможность использовать Hangfire без правок бизнес-кода: задачи в виде класса (`IScheduledJob`) работают как в Quartz-адаптере, задачи, заданные делегатом, **не поддерживаются** из-за ограничений Hangfire-сериализации.

## Состав

| Класс | Назначение |
|-------|-----------|
| `HangfireJobScheduler` | `IJobScheduler` — маппит `JobDefinition` в Hangfire RecurringJob / BackgroundJob. |
| `HangfireScheduledJobAdapter` | Единственный сериализуемый мост: `public Task RunScheduledJobAsync(string jobTypeName, string? serviceKey, CancellationToken ct)`. Резолвит `IScheduledJob` из DI по `Type.AssemblyQualifiedName` и прогоняет через pipeline. |
| `HangfireJobSchedulerBootstrapper` | `IHostedService` — на старте регистрирует все `JobDefinition` в Hangfire. |
| `HangfireDependencyInjector` | `DependencyInjectorBase` — регистрирует Hangfire (in-memory storage, AspNetCoreJobActivator), `IJobScheduler`, bridge, bootstrapper. |

## DI-регистрация

```csharp
protected override IServiceCollection Process(IServiceCollection serviceCollection)
{
    return serviceCollection
        .AddHangfire(config => config
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseInMemoryStorage())
        .AddHangfireServer()
        .AddTransient<HangfireScheduledJobAdapter>()
        .AddSingleton<IJobScheduler, HangfireJobScheduler>()
        .AddHostedService<HangfireJobSchedulerBootstrapper>();
}
```

> **Production:** замените `UseInMemoryStorage()` на `UseSqlServerStorage(...)` / `UseRedisStorage(...)`, чтобы задачи пережили рестарт и работали в multi-instance.

## Почему `HangfireScheduledJobAdapter`

Hangfire требует, чтобы тело лямбды в `BackgroundJob.Schedule` / `RecurringJob.AddOrUpdate` было **`MethodCallExpression`** на конкретный метод. Лямбда вида `() => capturedFunc(...)` даёт `InvocationExpression` и падает с:

```
Expression body should be of type MethodCallExpression
```

**Решение:** всегда вызывать `bridge => bridge.RunScheduledJobAsync(typeName, serviceKey, ct)` — это MethodCall на единственный публичный метод `HangfireScheduledJobAdapter`. Внутри bridge:

1. Резолвит `Type` по `AssemblyQualifiedName` из аргумента.
2. Резолвит `IScheduledJob` из DI (или `GetRequiredKeyedService`).
3. Строит `ScheduledJobContext` и вызывает `executor.ExecuteAsync(ctx)`.

```csharp
public sealed class HangfireScheduledJobAdapter : ... // transient
{
    public async Task RunScheduledJobAsync(string jobTypeName, string? serviceKey, CancellationToken ct)
    {
        var jobType = Type.GetType(jobTypeName, throwOnError: false)
            ?? throw new InvalidOperationException($"Failed to resolve type '{jobTypeName}'.");

        var job = serviceKey is null
            ? serviceProvider.GetRequiredService(jobType)
            : serviceProvider.GetRequiredKeyedService(jobType, serviceKey);

        if (job is not IScheduledJob)
            throw new InvalidOperationException(
                $"Type {jobType.FullName} is registered in DI but does not implement {nameof(IScheduledJob)}.");

        var ctx = new ScheduledJobContext(jobType.FullName!, serviceProvider, ct)
        {
            JobType = jobType,
            ServiceKey = serviceKey,
        };

        await executor.ExecuteAsync(ctx);
    }
}
```

## Маппинг `JobSchedule` → Hangfire

| `JobSchedule` | Hangfire-вызов |
|---------------|----------------|
| `Cron(expression)` | `RecurringJob.AddOrUpdate<HangfireScheduledJobAdapter>(jobKey, b => b.RunScheduledJobAsync(...), expression)` |
| `OnStartup` | `BackgroundJob.Schedule<HangfireScheduledJobAdapter>(b => b.RunScheduledJobAsync(...), TimeSpan.Zero)` |
| `Flags(OnStartup)` | `BackgroundJob.Schedule<HangfireScheduledJobAdapter>(b => b.RunScheduledJobAsync(...), TimeSpan.Zero)` |
| `Flags(Daily / Weekly / Monthly / EveryHour / EveryMinute)` | Маппится в синтетическое cron-выражение через `BuildCronFromFlag` + `RecurringJob.AddOrUpdate<HangfireScheduledJobAdapter>($"{jobKey}#{flag}", b => b.RunScheduledJobAsync(...), cron)` |

`BuildCronFromFlag` (из `HangfireJobScheduler`):

| `JobTriggerFlags` | Cron |
|-------------------|------|
| `EveryMinute` | `* * * * *` |
| `EveryHour` | `{minute} * * * *` |
| `Daily` | `{minute} {hour} * * *` |
| `Weekly` | `{minute} {hour} * * 1` |
| `Monthly` | `{minute} {hour} 1 * *` |

Каждый флаг из комбинации порождает **отдельный** RecurringJob с ключом `"{jobKey}#{flag}"`. Несколько флагов → несколько независимых cron-расписаний.

## Ограничение: задачи, заданные делегатом, не поддерживаются

```csharp
// ❌ НЕ работает в Hangfire:
opts.AddJob("MyLambdaJob", new JobSchedule.Cron("0 0/5 * * * ?"),
    ct => sp.GetRequiredService<IFoo>().DoWork(ct));
```

При попытке зарегистрировать `HangfireJobScheduler` бросает:

```
NotSupportedException: HangfireJobScheduler: задача, заданная делегатом 'MyLambdaJob', не поддерживается в Hangfire.
Используйте задачи в виде класса через opts.AddJob<TClassJob>(...).
```

**Причина:** Hangfire сериализует аргументы и тело делегата. `Func<CancellationToken, Task>` через `ActionDataKey` сериализуется, но `IServiceProvider` внутри замыкания — нет (сервис-провайдер не Serializable). Без service-locator'а задача, заданная делегатом, не имеет доступа к `IServiceProvider` для получения зависимостей.

**Решение:** используйте задачи в виде класса:

```csharp
// ✅ Работает в Hangfire:
opts.AddJob<MyClassJob>(new JobSchedule.Cron("0 0/5 * * * ?"));

public sealed class MyClassJob(IFoo foo, ILogger<MyClassJob> logger) : IScheduledJob
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        await foo.DoWork(ct);
    }
}
```

## Bootstrapper

```csharp
public sealed class HangfireJobSchedulerBootstrapper : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var definition in _options.Definitions)
            await _scheduler.ScheduleAsync(definition, cancellationToken);
        // Hangfire-сервер поднимается автоматически через AddHangfireServer()
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    // Сервер останавливается через IDisposable/IHostedService, зарегистрированный AddHangfireServer()
}
```

## Сравнение Quartz и Hangfire

| Возможность | Quartz | Hangfire |
|-------------|--------|----------|
| Задачи в виде класса (`IScheduledJob`) | ✅ Полная поддержка | ✅ Полная поддержка |
| Задачи, заданные делегатом | ✅ Через `JobDataMap` + `QuartzScheduledJobAdapter` | ❌ `NotSupportedException` — замыкание не сериализуемо |
| `JobSchedule.Cron` | ✅ Нативный `WithCronSchedule` | ✅ `RecurringJob.AddOrUpdate` + cron |
| `JobSchedule.OnStartup` | ✅ `StartNow()` | ✅ `BackgroundJob.Schedule` + `TimeSpan.Zero` |
| `JobSchedule.Flags` | ✅ Несколько Quartz-триггеров (нативный calendar interval) | ⚠️ Синтетические cron-выражения для каждого флага |
| In-memory storage | ✅ По умолчанию | ✅ `UseInMemoryStorage` |
| Multi-instance persistence | ⚠️ Нужен Quartz Cluster + DB | ⚠️ Нужен `UseSqlServerStorage` / `UseRedisStorage` |
| DI-инжекция в job-тип | ✅ `IServiceProvider` через `QuartzScheduledJobAdapter` | ✅ `AspNetCoreJobActivator` (стандартный Hangfire) |
| Cross-cutting через pipeline | ✅ Middleware (Logging, Correlation, Retry) | ✅ Те же middleware — мост вызывает `executor.ExecuteAsync` |
| Надёжность | Battle-tested с 2003 | Battle-tested с 2011 |
| Production-готовность | ✅ | ✅ |

**Когда выбирать:** Quartz — если есть задачи, заданные делегатом, или нужны сложные trigger-стратегии (calendar exclusions, misfire instructions). Hangfire — если планируется переход на SQL/Redis storage или уже есть Hangfire-инфраструктура.

## Связанные документы

| Документ | Описание |
|----------|----------|
| [Job Scheduler (top-level)](../job-scheduler.md) | Обзор |
| [Quartz Adapter](quartz-adapter.md) | Альтернативная реализация |
| [Architecture](architecture.md) | Слои и обоснование |
| [Pipeline](pipeline.md) | Middleware-цепочка |
| [Migration Guide](migration-guide.md) | Переезд с `QuartzJobWrapper` |
| [Zero-Touch Proof](zero-touch-proof.md) | Доказательство нулевых правок при смене провайдера |
