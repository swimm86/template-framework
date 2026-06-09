# 🕓 Job Scheduler — Планировщик фоновых задач

**Сборки:** `Shared.Application.Core.dll` (абстракции), `Shared.Infrastructure.Job.Quartz.dll` / `Shared.Infrastructure.Job.Hangfire.dll` (адаптеры)
**Namespaces:** `Shared.Application.Core.Job`, `Shared.Infrastructure.Job.Quartz`, `Shared.Infrastructure.Job.Hangfire`
**Исходники:** `src/Shared/Core/Shared.Application.Core/Job/`, `src/Shared/Job/Shared.Infrastructure.Job.Quartz/`, `src/Shared/Job/Shared.Infrastructure.Job.Hangfire/`

---

## Обзор

Job Scheduler — модуль фреймворка Shared для регистрации и запуска фоновых задач по расписанию. Модуль построен на принципах **Clean Architecture** и **DIP**: бизнес-код работает с абстракциями из `Shared.Application.Core.Job`, а конкретный планировщик (Quartz или Hangfire) подключается через **адаптер** в `Shared.Infrastructure.Job.*`. Бизнес-логика не знает про выбранный планировщик — замена провайдера выполняется сменой `ProjectReference` и одной строки в DI, без правок доменного кода.

Внутри Job Scheduler реализован **middleware-pipeline** (по аналогии с [Pipeline Behaviors](pipeline-behaviors.md) для MediatR): кросс-секинг concerns — логирование, correlation ID, retry — выносятся в отдельные middleware и не загрязняют сами задачи. Retry настраивается **per-job** через `RetryOptions`, а не глобально через DI (см. [Pipeline](job-scheduler/pipeline.md) и [Architecture](job-scheduler/architecture.md)).

## Проблема со старым API

В Template и PPS исторически использовался прямой `Shared.Infrastructure.Job.Quartz`-API: статичный `QuartzJobRegistrar` (через `services.RegisterJob<T>(...)`) и публичный базовый класс `QuartzJobWrapper : IJob` (через `: QuartzJobWrapper(serviceProvider, logger)`). Подход работал, но нарушал **DIP** и **OCP**:

- `using Quartz;` утекал в бизнес-слой — `IJob`, `IJobExecutionContext`, `JobBuilder`, `TriggerBuilder` фигурировали в доменном коде.
- Базовый класс `QuartzJobWrapper` требовал `IServiceProvider` в конструкторе — service locator-антипаттерн.
- `QuartzJobWrapper.ProcessAsync(IJobExecutionContext, CancellationToken)` подменял стандартный контракт `Task ExecuteAsync(CancellationToken)` — запутано и не DI-friendly.
- Переезд на другой планировщик (например, Hangfire) требовал **правок в каждой джобе** — сменить базу, метод, конструктор, `using`.

Новый Job Scheduler решает все эти проблемы: бизнес-джоба реализует `IScheduledJob` с единственным методом `ExecuteAsync(CancellationToken)`, Quartz-типы остаются за `internal`-адаптером, а провайдер меняется одной строкой.

## Архитектура

```
┌──────────────────────────────────────────────────────────────────────────┐
│  Application.Core (домен)                                                │
│                                                                          │
│   IScheduledJob     JobDefinition     JobSchedule (Cron | Flags | Startup)│
│   IJobScheduler     JobSchedulerBuilder                                  │
│   IScheduledJobExecutor + IScheduledJobMiddleware (Pipeline)            │
│   CorrelationIdMiddleware / LoggingMiddleware / RetryMiddleware          │
│   ServiceCollectionExtensions (AddJobs)                                  │
│   CacheJobExtensions / DbSeederExtensions                                │
└──────────────────────────────────────────────────────────────────────────┘
                              ▲
                              │ depends on (DIP)
                              │
┌─────────────────────────────────┐   ┌─────────────────────────────────┐
│  Infrastructure.Job.Quartz      │   │  Infrastructure.Job.Hangfire     │
│                                 │   │                                  │
│  QuartzJobScheduler             │   │  HangfireJobScheduler            │
│  QuartzScheduledJobAdapter      │   │  HangfireScheduledJobAdapter               │
│  QuartzJobSchedulerBootstrapper │   │  HangfireJobSchedulerBootstrapper│
│  QuartzDependencyInjector       │   │  HangfireDependencyInjector      │
└─────────────────────────────────┘   └─────────────────────────────────┘
                              ▲                       ▲
                              │                       │
                         Quartz.NET              Hangfire
```

Бизнес-код ссылается **только** на `Shared.Application.Core` (или `Shared.Application.Core.Job`). `Shared.Infrastructure.Job.Quartz` / `Shared.Infrastructure.Job.Hangfire` — это полные взаимозаменяемые адаптеры, различающиеся только тем, как они маппят `JobSchedule` → конкретный триггер.

## Быстрый старт

### 1. Фоновая задача в виде класса (рекомендуется)

```csharp
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Job;

public sealed class HelloWorldJob(ILogger<HelloWorldJob> logger) : IScheduledJob
{
    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Hello from background at {Time}", DateTime.UtcNow);
        return Task.CompletedTask;
    }
}
```

### 2. Регистрация

```csharp
// В любом DependencyInjector (например, Bff.Application.DependencyInjection)
services
    .AddSingleton<HelloWorldJob>()
    .AddJobs(opts => opts
        .AddJob<HelloWorldJob>(new JobSchedule.OnStartup())
        .AddJob<NightlySyncJob>(new JobSchedule.Cron("0 0 3 * * ?")));

// Если джобе нужны повторы при неудаче — навешиваем RetryOptions per-job:
services.AddJobs(opts => opts
    .AddJob<NetworkSyncJob>(
        new JobSchedule.Cron("0 0/5 * * * ?"),
        new RetryOptions
        {
            MaxAttempts = 3,
            Delay = TimeSpan.FromMinutes(1),
        }));

// Если джоба зарегистрирована как keyed-сервис (несколько экземпляров под разными ключами):
services
    .AddKeyedScoped<CacheUpdateJob, string>("region-eu")
    .AddKeyedScoped<CacheUpdateJob, string>("region-us");

services.AddJobs(opts => opts
    .AddJob<CacheUpdateJob>("region-eu", new JobSchedule.Cron("0 */5 * * * ?"))
    .AddJob<CacheUpdateJob>("region-us", new JobSchedule.Cron("0 */5 * * * ?")));
```

### 3. Выбор провайдера — смена ProjectReference

```xml
<!-- .csproj сервиса: Quartz (по умолчанию) -->
<ProjectReference Include="..\..\Shared\Job\Shared.Infrastructure.Job.Quartz\Shared.Infrastructure.Job.Quartz.csproj" />

<!-- Замена на Hangfire: -->
<ProjectReference Include="..\..\Shared\Job\Shared.Infrastructure.Job.Hangfire\Shared.Infrastructure.Job.Hangfire.csproj" />
```

Больше ничего менять не нужно. `AddJobs(...)` уже зарегистрировал `JobSchedulerOptions`, executor и middleware. Адаптер поднимет свой планировщик при старте (`IHostedService`) и заберёт `JobDefinition`-ы из DI. Внутри `JobDefinition` адаптер пробрасывает per-job `RetryOptions` (через `JobDataMap` в Quartz, через аргумент bridge-метода в Hangfire) — см. [Pipeline](job-scheduler/pipeline.md) и [Quartz Adapter](job-scheduler/quartz-adapter.md).

## API Reference

| Тип | Где живёт | Назначение |
|-----|-----------|-----------|
| `IScheduledJob` | `Shared.Application.Core.Job` | Маркер для фоновой задачи в виде класса. Метод: `ExecuteAsync(CancellationToken)`. |
| `JobDefinition` | `Shared.Application.Core.Job.Scheduler` | `sealed record`: ключ, расписание, делегат или тип фоновой задачи, опциональный `ServiceKey` (keyed-DI) и `RetryOptions` (per-job retry-политика). |
| `JobSchedule` | `Shared.Application.Core.Job` | Discriminated union: `Cron(expr)`, `Flags(flags, time)`, `OnStartup`. |
| `JobTriggerFlags` | `Shared.Application.Core.Job` | `[Flags]`-перечисление: `Daily`, `Weekly`, `Monthly`, `OnStartup`, `EveryMinute`, `EveryHour`. |
| `IJobScheduler` | `Shared.Application.Core.Job` | Runtime API: `ScheduleAsync(JobDefinition)`. |
| `JobSchedulerBuilder` | `Shared.Application.Core.Job` | Fluent-конструктор для bootstrap-регистрации. |
| `JobSchedulerOptions` | `Shared.Application.Core.Job` | Контейнер, который bootstrapper читает из DI. |
| `IScheduledJobMiddleware` | `Shared.Application.Core.Job.Pipeline` | Контракт middleware (logging / correlation / retry). |
| `IScheduledJobExecutor` | `Shared.Application.Core.Job.Pipeline` | Исполнитель — собирает pipeline и применяет его к `ScheduledJobContext`. |
| `AddJobs(opts => ...)` | `Shared.Application.Core.Job.Extensions` | Главная точка входа: регистрирует опции, executor, дефолтные middleware. |
| `AddCronCacheJob` / `AddFlagsCacheJob` | `Shared.Application.Core.Job.Extensions` | Упрощённая запись: `ICacheService<T>` + периодическое обновление по расписанию. |
| `RegisterDbSeederJob` | `Shared.Application.Core.Job.Extensions` | Регистрирует `DbSeederJob` на `OnStartup`. |
| `IJobScheduler` impl | `Shared.Infrastructure.Job.Quartz` / `Hangfire` | Адаптерная реализация, выбранная DI. |
| `*JobSchedulerBootstrapper` | Адаптеры | `IHostedService`: поднимает планировщик и регистрирует все `JobDefinition`. |
| `QuartzScheduledJobAdapter` / `HangfireScheduledJobAdapter` | Адаптеры | Внутренний мост: провайдер-специфичный `IJob`/action-вызов. |

## Когда использовать

| Сценарий | Подходит ли Job Scheduler |
|----------|---------------------------|
| Периодическая синхронизация / очистка (cron, daily, weekly) | ✅ |
| Кэш-обновление по расписанию | ✅ (`AddCronCacheJob`) |
| Onetime-seed при старте приложения | ✅ (`RegisterDbSeederJob`) |
| Длинные ETL-процессы, требующие UI-управления | ❌ — используйте отдельный orchestration tool |
| Очередь сообщений / fire-and-forget fan-out | ❌ — для этого есть Kafka/RabbitMQ (см. `Gpn.Contour.Pps.Kafka`) |
| Задача должна выполняться строго один раз в multi-instance | ⚠️ — in-memory Quartz подходит, Hangfire требует общего storage (SQL/Redis) |

## Когда НЕ использовать

- **Real-time обработка событий** — Job Scheduler не event-driven. Используйте Kafka/SignalR.
- **Задачи с жёсткими latency-SLA** — cron-trigger не гарантирует точность в миллисекундах.
- **Бизнес-логика в shared-слое** — задачи должны жить в Application-слое сервиса и зависеть от его Application-интерфейсов, а не наоборот.

## Сравнение Quartz и Hangfire

Краткая сводка. Подробности — в [quartz-adapter.md](job-scheduler/quartz-adapter.md) и [hangfire-adapter.md](job-scheduler/hangfire-adapter.md).

| Возможность | Quartz | Hangfire |
|-------------|--------|----------|
| Задачи в виде класса (`IScheduledJob`) | ✅ | ✅ |
| Задачи, заданные делегатом | ✅ (через `JobDataMap` + adapter) | ❌ (NotSupportedException — замыкание не сериализуемо) |
| `JobSchedule.Cron` | ✅ (`WithCronSchedule`) | ✅ (`RecurringJob.AddOrUpdate` + cron) |
| `JobSchedule.OnStartup` | ✅ (`StartNow()`) | ✅ (`BackgroundJobClient.Create` + `new ScheduledState(TimeSpan.Zero)`) |
| `JobSchedule.Flags` (Daily/Weekly/...) | ✅ (`WithCalendarIntervalSchedule`) | ⚠️ Маппится в синтетические cron-выражения |
| In-memory storage | ✅ (по умолчанию) | ✅ (`UseInMemoryStorage`) |
| DI-инжекция в `IJob`/`Bridge` | ✅ | ✅ (`AspNetCoreJobActivator`) |
| Multi-instance persistence | ⚠️ Нужен Quartz Cluster + DB | ⚠️ Нужен SQL/Redis storage |
| Production-ready | ✅ Battle-tested | ✅ Battle-tested |

## Документация Job Scheduler

| Документ | Описание |
|----------|----------|
| [Architecture](job-scheduler/architecture.md) | Слои, DIP/SOLID, поток выполнения для классовой и лямбда-джобы |
| [Pipeline](job-scheduler/pipeline.md) | `IScheduledJobMiddleware`, дефолтные middleware, как добавить свой |
| [Quartz Adapter](job-scheduler/quartz-adapter.md) | `QuartzJobScheduler`, `QuartzScheduledJobAdapter`, bootstrapper, DI |
| [Hangfire Adapter](job-scheduler/hangfire-adapter.md) | `HangfireJobScheduler`, `HangfireScheduledJobAdapter`, ограничение на лямбды, маппинг cron |
| [Migration Guide](job-scheduler/migration-guide.md) | Переезд с `QuartzJobRegistrar` / `QuartzJobWrapper` на новый API |
| [Zero-Touch Proof](job-scheduler/zero-touch-proof.md) | Доказательство, что смена провайдера = 0 правок в бизнес-коде |

## См. также

| Документ | Описание |
|----------|----------|
| [Design](job-scheduler/design.md) | Дизайн-решение и контракты (предшественник этой документации) |
| [Pipeline Behaviors](pipeline-behaviors.md) | Аналогичный pipeline для MediatR — концептуально близок к Job Scheduler pipeline |
| [Cache](cache.md) | `ICacheService<T>` + `AddCronCacheJob` / `AddFlagsCacheJob` |
| [Db Seeder](db-seeder.md) | `RegisterDbSeederJob` запускает сиды при старте |
| [Correlation ID](correlation-id.md) | `JobCorrelationContext` — distributed tracing для фоновых задач |
| [Service Startup](service-startup.md) | Полный bootstrap-флоу и `AddReferencedDependencyInjectors` |
