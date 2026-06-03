# Shared.Infrastructure.Job.Quartz

Quartz.NET-адаптер для [Job Scheduler](../docs/job-scheduler.md).

## Что внутри

| Класс | Назначение |
|-------|-----------|
| `QuartzJobScheduler` | `IJobScheduler` — маппит `JobDefinition` в Quartz Job + Trigger |
| `QuartzScheduledJobAdapter` | `internal` Quartz-обёртка, реализующая `IJob` |
| `QuartzJobSchedulerBootstrapper` | `IHostedService` — старт/стоп Quartz-планировщика |
| `QuartzDependencyInjector` | DI-регистрация (auto-discover через `AddReferencedDependencyInjectors`) |

Подробности — в [`docs/job-scheduler/quartz-adapter.md`](../../../docs/job-scheduler/quartz-adapter.md).

## Как регистрируется

Адаптер подключается автоматически, если в `.csproj` сервиса есть `ProjectReference` на этот проект:

```xml
<ProjectReference Include="..\..\..\Shared\Job\Shared.Infrastructure.Job.Quartz\Shared.Infrastructure.Job.Quartz.csproj" />
```

При старте `AddReferencedDependencyInjectors()` найдёт `QuartzDependencyInjector` и зарегистрирует:

```csharp
services
    .AddSingleton<ISchedulerFactory, StdSchedulerFactory>()
    .AddSingleton<IJobScheduler, QuartzJobScheduler>()
    .AddSingleton<QuartzJobScheduler>()
    .AddHostedService<QuartzJobSchedulerBootstrapper>();
```

## Отличия от старого API

| Старый API (`QuartzJobRegistrar` / `QuartzJobWrapper`) | Новый API (`IScheduledJob` + `AddJobs`) |
|---|---|
| `services.RegisterJob<MyJob>("0 0/5 * * * ?")` | `services.AddJobs(opts => opts.AddJob<MyJob>(new JobSchedule.Cron("0 0/5 * * * ?")))` |
| `class MyJob : QuartzJobWrapper` | `class MyJob(...) : IScheduledJob` |
| `ProcessAsync(IJobExecutionContext, CancellationToken)` | `ExecuteAsync(CancellationToken)` |
| `using Quartz;` в бизнес-коде | ❌ нет |
| `IServiceProvider` в конструкторе (service-locator) | DI через конструктор |
| Auto-retry через 5 мин (Quartz-триггер) | In-process retry через `RetryMiddleware` |
| Смена на Hangfire = правки в каждой джобе | Смена `ProjectReference` = 0 правок в джобах |

## Зависимости

- `Quartz 3.18.1`
- `Quartz.Extensions.DependencyInjection 3.18.1`
- `Quartz.Extensions.Hosting 3.18.1`
- `Shared.Application.Core`
- `Shared.Infrastructure.Core`

## Документация

- [Job Scheduler (top-level)](../../../docs/job-scheduler.md)
- [Architecture](../../../docs/job-scheduler/architecture.md)
- [Quartz Adapter (детали)](../../../docs/job-scheduler/quartz-adapter.md)
- [Pipeline](../../../docs/job-scheduler/pipeline.md)
- [Migration Guide](../../../docs/job-scheduler/migration-guide.md)
- [Zero-Touch Proof](../../../docs/job-scheduler/zero-touch-proof.md)
