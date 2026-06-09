# Quartz Adapter

**Сборка:** `Shared.Infrastructure.Job.Quartz.dll`
**Namespace:** `Shared.Infrastructure.Job.Quartz`
**NuGet:** `Quartz 3.18.1`, `Quartz.Extensions.DependencyInjection 3.18.1`, `Quartz.Extensions.Hosting 3.18.1`
**Исходники:** `src/Shared/Job/Shared.Infrastructure.Job.Quartz/`

---

## Обзор

Quartz Adapter — основная реализация `IJobScheduler` на базе [Quartz.NET](https://www.quartz-scheduler.net/). Адаптер изолирует Quartz-типы внутри `Shared.Infrastructure.Job.Quartz` — бизнес-код работает с абстракциями из `Shared.Application.Core.Job`.

## Состав

| Класс | Назначение |
|-------|-----------|
| `Constants` | Контейнер констант — ключи `JobDataMap` (`JobTypeKey`, `ServiceKeyKey`, `ActionDataKey`, `RetryOptionsKey`). Раньше были разбросаны по `QuartzScheduledJobAdapter` / `JobDefinition` — теперь единая точка. |
| `QuartzJobScheduler` | `IJobScheduler` — маппит `JobDefinition` в Quartz Job + Trigger и регистрирует в `IScheduler`. Кладёт `RetryOptions` в `JobDataMap[Constants.RetryOptionsKey]`. |
| `QuartzScheduledJobAdapter` | `internal sealed` — Quartz-обёртка, реализующая `IJob`. Достаёт из `JobDataMap` `JobType` / `ServiceKey` / `Action` / `RetryOptions` и строит `ScheduledJobContext`. |
| `QuartzJobSchedulerBootstrapper` | `IHostedService` — на старте поднимает `IScheduler`, регистрирует все `JobDefinition` из `JobSchedulerOptions`, запускает scheduler. На shutdown — `Shutdown(true)`. |
| `QuartzDependencyInjector` | `DependencyInjectorBase` — регистрирует `ISchedulerFactory`, `IJobScheduler`, bootstrapper. |

## DI-регистрация

Через `QuartzDependencyInjector` (вызывается автоматически из `AddReferencedDependencyInjectors` при наличии `ProjectReference`):

```csharp
protected override IServiceCollection Process(IServiceCollection serviceCollection)
{
    return serviceCollection
        .AddSingleton<ISchedulerFactory, StdSchedulerFactory>()
        .AddSingleton<IJobScheduler, QuartzJobScheduler>()
        .AddHostedService<QuartzJobSchedulerBootstrapper>();
}
```

## Маппинг `JobSchedule` → Quartz Trigger

`QuartzJobScheduler.BuildTrigger` преобразует абстрактное расписание в конкретный `ITrigger`:

| `JobSchedule` | Quartz-реализация |
|---------------|-------------------|
| `Cron(expression)` | `TriggerBuilder.WithCronSchedule(expression)` |
| `OnStartup` | `TriggerBuilder.StartNow().Build()` |
| `Flags(Daily)` | `WithCalendarIntervalSchedule(b => b.WithIntervalInDays(1))` |
| `Flags(Weekly)` | `WithCalendarIntervalSchedule(b => b.WithIntervalInWeeks(1))` |
| `Flags(Monthly)` | `WithCalendarIntervalSchedule(b => b.WithIntervalInMonths(1))` |
| `Flags(EveryHour)` | `WithCalendarIntervalSchedule(b => b.WithIntervalInHours(1))` |
| `Flags(EveryMinute)` | `WithCalendarIntervalSchedule(b => b.WithIntervalInMinutes(1))` |
| Комбинация `Flags` | Несколько trigger-ов на одном job (по одному на каждый флаг) |

`JobBuilder` всегда создаёт `QuartzScheduledJobAdapter` (наш internal-обёртку) — Quartz-типы job-класса не утекают в бизнес-код.

### Пример

```csharp
// Бизнес-код:
opts.AddJob<MyJob>(
    new JobSchedule.Cron("0 0/5 * * * ?"),
    new RetryOptions { MaxAttempts = 3, Delay = TimeSpan.FromMinutes(1) });

// Внутри QuartzJobScheduler.ScheduleAsync:
var job = JobBuilder.Create<QuartzScheduledJobAdapter>()
    .WithIdentity(new JobKey(definition.JobKey))
    .SetJobData(jobData)             // JobType / ServiceKey / Action / RetryOptions
    .Build();

var trigger = TriggerBuilder.Create()
    .ForJob(jobKey)
    .WithIdentity($"{JobKey}.trigger")
    .WithCronSchedule("0 0/5 * * * ?")
    .Build();

await scheduler.ScheduleJob(job, trigger, ct);
```

## `QuartzScheduledJobAdapter` — запуск через pipeline

`internal sealed` класс, реализующий `Quartz.IJob`. Quartz-триггер дёргает его `Execute(IJobExecutionContext)`:

```csharp
internal sealed class QuartzScheduledJobAdapter(
    IServiceProvider serviceProvider,
    IScheduledJobExecutor executor,
    ILogger<QuartzScheduledJobAdapter> logger)
    : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var jobKey = context.JobDetail.Key.Name;
        var cancellationToken = context.CancellationToken;

        var jobType = context.JobDetail.JobDataMap[Constants.JobTypeKey] is not string jobTypeName
            ? null
            : Type.GetType(jobTypeName, throwOnError: false);
        var action = context.JobDetail.JobDataMap[Constants.ActionDataKey] as Func<IServiceProvider, CancellationToken, Task>;

        if (jobType is null && action is null)
        {
            logger.LogError(
                "Job '{JobKey}': {JobDataMap} not contains action for current keyed job type.",
                jobKey, nameof(IJobDetail.JobDataMap));
            return;
        }

        var serviceKey = context.JobDetail.JobDataMap[Constants.ServiceKeyKey] as string;
        var retryOptions = context.JobDetail.JobDataMap[Constants.RetryOptionsKey] as RetryOptions;
        var ctx = new ScheduledJobContext(jobKey, serviceProvider, cancellationToken)
        {
            JobType = jobType,
            ServiceKey = serviceKey,
            Action = action,
            RetryOptions = retryOptions,
        };

        await executor.ExecuteAsync(ctx);
    }
}
```

| Шаг | Действие |
|-----|----------|
| 1 | Достать из `JobDataMap` `JobType` (AssemblyQualifiedName) или `Action` (делегат). |
| 2 | Зарезолвить тип через `Type.GetType(...)`. |
| 3 | Достать `ServiceKey` (опционально) и `RetryOptions` (опционально, из `Constants.RetryOptionsKey`). |
| 4 | Создать `ScheduledJobContext`. |
| 5 | Вызвать `executor.ExecuteAsync(ctx)` — pipeline middleware (CorrelationId → Logging → Retry → Terminal). |
| 6 | Terminal: для задачи в виде класса — `sp.GetRequiredService(JobType).ExecuteAsync(ct)`; для задачи, заданной делегатом — `ctx.Action(ctx.ServiceProvider, ctx.CancellationToken)`. |

## Ключи `JobDataMap` — `Constants`

Класс `Shared.Infrastructure.Job.Quartz.Constants` инкапсулирует все ключи, под которыми Quartz-адаптер складывает данные в `JobDataMap`. Раньше они жили как `public const string` в `QuartzScheduledJobAdapter` (`JobTypeKey`, `ServiceKeyKey`) и в `JobDefinition` (`ActionDataKey`); `ActionDataKey` также дублировался по смыслу в Hangfire-адаптере. Теперь — единая точка истины на уровне Quartz-сборки:

| Ключ | Тип значения | Кто кладёт | Кто читает |
|------|--------------|-----------|------------|
| `Constants.JobTypeKey` (`"JobType"`) | `string` (AssemblyQualifiedName) | `QuartzJobScheduler` | `QuartzScheduledJobAdapter` |
| `Constants.ServiceKeyKey` (`"ServiceKey"`) | `string?` | `QuartzJobScheduler` | `QuartzScheduledJobAdapter` |
| `Constants.ActionDataKey` (`"JobAction"`) | `Func<IServiceProvider, CancellationToken, Task>?` | `QuartzJobScheduler` (только для лямбда-джоб) | `QuartzScheduledJobAdapter` |
| `Constants.RetryOptionsKey` (`"RetryOptions"`) | `RetryOptions?` | `QuartzJobScheduler` (если `JobDefinition.RetryOptions != null`) | `QuartzScheduledJobAdapter` |

## ⚠️ Ограничения для Quartz Cluster / persistent JobStore

Текущая реализация `JobDataMap` в `QuartzJobScheduler` / `QuartzScheduledJobAdapter` корректно работает **только в in-memory режиме** (`UseInMemoryStore` / `RAMJobStore`).

При переходе на **persistent JobStore** (ADO.NET JobStore, Quartz Cluster с общей БД) возникают следующие ограничения:

| Ключ | Тип значения | Проблема при persistence | Решение |
|------|--------------|--------------------------|---------|
| `Constants.ActionDataKey` | `Func<IServiceProvider, CancellationToken, Task>` | Делегат **не сериализуем** в принципе (замыкание на `Func`-переменную). При `JobStore` `BinaryFormatter` бросит `SerializationException` при сохранении триггера. | Не использовать lambda-jobs в кластере — только `IScheduledJob`-классы через `AddJob<T>()`. |
| `Constants.RetryOptionsKey` | `RetryOptions` (POCO) | POCO сериализуем, но **содержит `TimeSpan`**, который Quartz сериализует как `long` (ticks). Это работает, но в JobStore будут храниться **только те `RetryOptions`, которые были на момент `ScheduleJob`** — изменить retry-политику существующего триггера нельзя без перерегистрации. | Если нужна динамическая retry-политика — вынести `RetryOptions` в `IOptionsMonitor<>` по `JobKey` и резолвить внутри `IScheduledJob` (не через `ScheduledJobContext.RetryOptions`). |
| `Constants.JobTypeKey` / `ServiceKeyKey` | `string` | Сериализуемы, проблем нет. | — |

**Вывод:** в текущей итерации Job Scheduler **поддерживает только single-instance deployment с in-memory Quartz** (или in-memory Hangfire). Для multi-instance / кластерных сценариев требуется:

1. Убрать `Constants.ActionDataKey` из JobDataMap (запретить lambda-jobs);
2. Вынести `RetryOptions` в отдельный `IOptionsMonitor`-канал, не сериализуемый в JobStore;
3. Или использовать внешний storage для retry-конфигурации (БД / Redis).

См. также: [Hangfire storage caveats](hangfire-adapter.md#-ограничения-для-multi-instance) — Hangfire имеет аналогичные, но более мягкие ограничения.

## Bootstrapper

```csharp
public sealed class QuartzJobSchedulerBootstrapper : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // 1. Регистрация всех JobDefinition в Quartz
        foreach (var definition in _options.Definitions)
            await _scheduler.ScheduleAsync(definition, cancellationToken);

        // 2. Запуск scheduler
        var quartzScheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        await quartzScheduler.Start(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var quartzScheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        await quartzScheduler.Shutdown(true, cancellationToken);  // waitForJobsToComplete: true
    }
}
```

`Shutdown(true)` гарантирует, что in-flight итерации джоб завершатся до выхода из приложения.

## Где Quartz-типы утекают

**Нигде в бизнес-коде.** Все Quartz-ссылки (`IScheduler`, `IJob`, `IJobExecutionContext`, `ITrigger`, `JobBuilder`, `TriggerBuilder`, `DateBuilder`, `CronScheduleBuilder`) находятся **только** в:

- `Shared.Infrastructure.Job.Quartz` (4 файла адаптера);
- `Shared.Infrastructure.Job.Quartz.Tests` (тесты).

В `Shared.Application.Core.Job` и в сервисах (`Bff.Application`, `Setter.Infrastructure` и т.д.) — **нет** ни одного `using Quartz;`. Это легко проверить:

```powershell
PS> Get-ChildItem -Path F:\template\src\Services -Recurse -Include *.cs |
      Where-Object { (Get-Content $_.FullName) -match 'using Quartz' } |
      Measure-Object
Count : 0
```

## Миграция со старого API

| Старое | Новое |
|--------|-------|
| `: QuartzJobWrapper` | `: IScheduledJob` |
| `ProcessAsync(IJobExecutionContext, CancellationToken)` | `ExecuteAsync(CancellationToken)` |
| `RegisterJob<T>(cron)` | `AddJobs(opts => opts.AddJob<T>(new JobSchedule.Cron(cron)))` |
| `RegisterCacheJob(...)` | `AddCronCacheJob(...)` / `AddFlagsCacheJob(...)` |
| `RegisterDbSeederJob()` | `RegisterDbSeederJob()` (без изменений) |

Подробности — в [Migration Guide](migration-guide.md).

## Связанные документы

| Документ | Описание |
|----------|----------|
| [Job Scheduler (top-level)](../job-scheduler.md) | Обзор |
| [Hangfire Adapter](hangfire-adapter.md) | Альтернативная реализация |
| [Architecture](architecture.md) | Слои и обоснование |
| [Pipeline](pipeline.md) | Middleware-цепочка |
| [Migration Guide](migration-guide.md) | Переезд с `QuartzJobWrapper` |
