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
| `QuartzJobScheduler` | `IJobScheduler` — маппит `JobDefinition` в Quartz Job + Trigger и регистрирует в `IScheduler`. |
| `QuartzScheduledJobAdapter` | `internal sealed` — Quartz-обёртка, реализующая `IJob`. Получает `ScheduledJobContext`, вызывает executor. |
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
opts.AddJob<MyJob>(new JobSchedule.Cron("0 0/5 * * * ?"));

// Внутри QuartzJobScheduler.ScheduleAsync:
var job = JobBuilder.Create<QuartzScheduledJobAdapter>()
    .WithIdentity(new JobKey(definition.JobKey))
    .SetJobData(jobData)             // JobType / ServiceKey / Action
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
internal sealed class QuartzScheduledJobAdapter : IJob
{
    public const string JobTypeKey = "JobType";
    public const string ServiceKeyKey = "ServiceKey";

    public async Task Execute(IJobExecutionContext context)
    {
        var jobTypeName = context.JobDetail.JobDataMap[JobTypeKey] as string;
        var jobType = jobTypeName is null ? null : Type.GetType(jobTypeName, throwOnError: false);
        var serviceKey = context.JobDetail.JobDataMap[ServiceKeyKey] as string;
        var action = context.JobDetail.JobDataMap[JobDefinition.ActionDataKey] as Func<IServiceProvider, CancellationToken, Task>;

        var ctx = new ScheduledJobContext(jobKey, serviceProvider, cancellationToken)
        {
            JobType = jobType,
            ServiceKey = serviceKey,
            Action = action,
        };

        await executor.ExecuteAsync(ctx);
    }
}
```

| Шаг | Действие |
|-----|----------|
| 1 | Достать из `JobDataMap` `JobType` (AssemblyQualifiedName) или `Action` (делегат). |
| 2 | Зарезолвить тип через `Type.GetType(...)`. |
| 3 | Создать `ScheduledJobContext`. |
| 4 | Вызвать `executor.ExecuteAsync(ctx)` — pipeline middleware (Logging → Correlation → Retry → Terminal). |
| 5 | Terminal: для задачи в виде класса — `sp.GetRequiredService(JobType).ExecuteAsync(ct)`; для задачи, заданной делегатом — `ctx.Action(ctx.ServiceProvider, ctx.CancellationToken)`. |

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
