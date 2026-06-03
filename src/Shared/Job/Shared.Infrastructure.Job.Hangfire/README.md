# Shared.Infrastructure.Job.Hangfire

Hangfire-адаптер для [Job Scheduler](../docs/job-scheduler.md).

## Что внутри

| Класс | Назначение |
|-------|-----------|
| `HangfireJobScheduler` | `IJobScheduler` — маппит `JobDefinition` в Hangfire RecurringJob/BackgroundJob |
| `HangfireScheduledJobAdapter` | `transient` мост с единственным методом `RunScheduledJobAsync(typeName, serviceKey, ct)` — сериализуемая точка входа для Hangfire |
| `HangfireJobSchedulerBootstrapper` | `IHostedService` — регистрирует все `JobDefinition` в Hangfire |
| `HangfireDependencyInjector` | DI-регистрация (auto-discover через `AddReferencedDependencyInjectors`) |

Подробности — в [`docs/job-scheduler/hangfire-adapter.md`](../../../docs/job-scheduler/hangfire-adapter.md).

## Как регистрируется

```xml
<ProjectReference Include="..\..\..\Shared\Job\Shared.Infrastructure.Job.Hangfire\Shared.Infrastructure.Job.Hangfire.csproj" />
```

```csharp
services
    .AddHangfire(config => config
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseInMemoryStorage())           // ← замените на UseSqlServerStorage / UseRedisStorage в production
    .AddHangfireServer()
    .AddTransient<HangfireScheduledJobAdapter>()
    .AddSingleton<IJobScheduler, HangfireJobScheduler>()
    .AddHostedService<HangfireJobSchedulerBootstrapper>();
```

## Ограничения

| Что | Статус |
|-----|--------|
| Классовые джобы (`IScheduledJob`) | ✅ Полная поддержка |
| Лямбда-джобы (`AddJob(key, schedule, Func<...>)`) | ❌ `NotSupportedException` — closure над `Func` не сериализуется. Используйте классовые джобы. |
| Multi-instance persistence | ⚠️ In-memory storage не подходит. Используйте SQL/Redis. |

## Почему `HangfireScheduledJobAdapter`

Hangfire требует, чтобы тело лямбды в `BackgroundJob.Schedule` / `RecurringJob.AddOrUpdate` было `MethodCallExpression` на конкретный метод. `HangfireScheduledJobAdapter` предоставляет единственный публичный метод `RunScheduledJobAsync` — это MethodCall, который Hangfire умеет сериализовать.

Внутри bridge резолвит `IScheduledJob` из DI по `Type.AssemblyQualifiedName` (передаётся в аргументе) и прогоняет её через pipeline (`LoggingMiddleware` → `CorrelationIdMiddleware` → `RetryMiddleware` → Terminal).

## Отличия от старого API

| Старый API (`QuartzJobRegistrar` / `QuartzJobWrapper`) | Новый API через Hangfire |
|---|---|
| `services.RegisterJob<MyJob>(...)` | `services.AddJobs(opts => opts.AddJob<MyJob>(...))` — без изменений |
| `class MyJob : QuartzJobWrapper` | `class MyJob(...) : IScheduledJob` — без изменений |
| Quartz-specific | `JobSchedule.Cron` / `Flags` / `OnStartup` — единый контракт; Quartz- и Hangfire-адаптеры оба его поддерживают |
| Смена на Quartz = правки | Смена `ProjectReference` — 0 правок в джобах |

## Зависимости

- `Hangfire.Core 1.8.14`
- `Hangfire.InMemory 0.11.0`
- `Hangfire.NetCore 1.8.14`
- `Shared.Application.Core`
- `Shared.Infrastructure.Core`

## Документация

- [Job Scheduler (top-level)](../../../docs/job-scheduler.md)
- [Architecture](../../../docs/job-scheduler/architecture.md)
- [Hangfire Adapter (детали)](../../../docs/job-scheduler/hangfire-adapter.md)
- [Pipeline](../../../docs/job-scheduler/pipeline.md)
- [Migration Guide](../../../docs/job-scheduler/migration-guide.md)
- [Zero-Touch Proof](../../../docs/job-scheduler/zero-touch-proof.md)
