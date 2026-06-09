# Архитектура Job Scheduler

**Сборки:** `Shared.Application.Core.dll`, `Shared.Infrastructure.Job.Quartz.dll`, `Shared.Infrastructure.Job.Hangfire.dll`
**Исходники:** `src/Shared/Core/Shared.Application.Core/Job/`, `src/Shared/Job/Shared.Infrastructure.Job.{Quartz, Hangfire}/`

---

## Цели

Job Scheduler спроектирован в соответствии с тремя принципами:

| Принцип | Как реализуется |
|---------|-----------------|
| **Clean Architecture** | Бизнес-логика (Application-слои сервисов) зависит только от абстракций в `Shared.Application.Core.Job`. Конкретные планировщики (Quartz, Hangfire) — внешние адаптеры, изолированные в `Shared.Infrastructure.Job.*`. |
| **DIP (Dependency Inversion Principle)** | `IJobScheduler` объявлен в Application.Core. Адаптеры реализуют его, но не наоборот. `IScheduledJobMiddleware` объявлен в Application.Core, конкретные middleware — там же. |
| **SOLID** | Single responsibility (одна джоба = один класс), Open/Closed (новые провайдеры добавляются без правок бизнес-кода), Liskov (любая реализация `IJobScheduler` взаимозаменяема), Interface Segregation (`IScheduledJob` минимален), Dependency Inversion (см. выше). |

## Слои

```
┌────────────────────────────────────────────────────────────────────────┐
│  Application.Core (Domain / Application)                               │
│                                                                        │
│  Contracts:    IScheduledJob, IJobScheduler, JobSchedule,              │
│                JobDefinition, JobTriggerFlags, JobSchedulerBuilder,    │
│                JobSchedulerOptions                                     │
│  Pipeline:     IScheduledJobMiddleware, IScheduledJobExecutor,         │
│                ScheduledJobContext, ScheduledJobDelegate,              │
│                CorrelationIdMiddleware, LoggingMiddleware,             │
│                RetryMiddleware, RetryOptions                           │
│  Extensions:   ServiceCollectionExtensions (AddJobs),                  │
│                CacheJobExtensions, DbSeederExtensions,                 │
│                ScheduledJobExtensions, CacheUpdateJob, DbSeederJob     │
│                                                                        │
│  Зависимости: только Shared.* Core, БЕЗ Quartz, БЕЗ Hangfire.          │
└────────────────────────────────────────────────────────────────────────┘
                              ▲
                              │ depends on (DIP)
                              │
┌─────────────────────────────────┐  ┌──────────────────────────────────┐
│  Infrastructure.Job.Quartz      │  │  Infrastructure.Job.Hangfire     │
│  ───────────────────────────    │  │  ───────────────────────────     │
│  QuartzJobScheduler             │  │  HangfireJobScheduler            │
│  QuartzScheduledJobAdapter      │  │  HangfireScheduledJobAdapter     │
│  QuartzJobSchedulerBootstrapper │  │  HangfireJobSchedulerBootstrapper│
│  QuartzDependencyInjector       │  │  HangfireDependencyInjector      │
│                                 │  │                                  │
│  Зависимости: Quartz NuGet      │  │  Зависимости: Hangfire NuGet     │
└─────────────────────────────────┘  └──────────────────────────────────┘
                              ▲                       ▲
                              │                       │
                        Quartz.NET                  Hangfire
```

**Стрелка зависимости — только в одну сторону:** `Application.Core` ничего не знает про Quartz и Hangfire. Адаптеры зависят от `Application.Core` и от своих NuGet-пакетов. Сервисы (`Bff.Application`, `Setter.Infrastructure` и т.д.) ссылаются на `Application.Core` + **ровно один** из адаптеров.

## Почему pipeline

Cross-cutting concerns — логирование, correlation ID, retry, метрики, авторизация — повторяются в каждой джобе. Если размещать их в каждой джобе вручную, возникают проблемы:

- **Дублирование** — каждый раз копировать `try/finally JobCorrelationContext`, `logger.LogInformation("starting/completed")`, retry-loop;
- **Расхождение** — одни джобы логируют, другие нет; одни ретраят, другие нет;
- **Смешивание ответственности** — бизнес-логика перемешана с инфраструктурой.

Job Scheduler решает это через **middleware-pipeline** по аналогии с [Pipeline Behaviors](../pipeline-behaviors.md) для MediatR: каждая middleware — отдельный класс, реализующий `IScheduledJobMiddleware`. Executor строит цепочку (первый зарегистрированный = самый внешний) и оборачивает ей терминальный action. Бизнес-джоба остаётся "тонкой" — `ExecuteAsync` без логирования, без retry, без correlation.

```
┌────────────────────────────────────────────────────────┐
│  CorrelationIdMiddleware (1-й — самый внешний)         │
│  │  JobCorrelationContext.TrySetCorrelationId()        │
│  │  try { await next(); }                              │
│  │  finally { ClearCorrelationId() }                   │
│  ├─▶ LoggingMiddleware (2-й)                           │
│  │   │  logger.LogInformation("Job X is executing")    │
│  │   │  try { await next(); }                          │
│  │   │  catch { logger.LogError; throw; }              │
│  │   │  logger.LogInformation("Job X is completed")    │
│  │   ├─▶ RetryMiddleware (3-й — самый внутренний)      │
│  │   │   │  if (context.RetryOptions is null) → next()  │
│  │   │   │  while (attempt < context.RetryOptions.MaxAttempts)            │
│  │   │   │    try { await next(); return; }            │
│  │   │   │    catch { delay; ++attempt; }              │
│  │   │   ├─▶ Terminal:                                 │
│  │   │   │   • class job → resolve from DI + Execute   │
│  │   │   │   • lambda    → ctx.Action(ctx.ServiceProvider, ctx.CancellationToken)              │
│  └─▶  │   │                                            │
│        └─▶  return;                                    │
└────────────────────────────────────────────────────────┘
```

## Почему абстракция над обоими

Если бизнес-джобы зависят от конкретного планировщика (Quartz), переход на Hangfire требует:

- сменить базовый класс (`QuartzJobWrapper` → нет аналога);
- переписать конструктор (убрать `IServiceProvider`, изменить DI);
- переписать `Execute(IJobExecutionContext)` → `ExecuteAsync(CancellationToken)`;
- удалить `using Quartz;` из бизнес-файлов;
- переписать регистрацию (`RegisterJob<T>(cron)` → свой аналог в Hangfire);
- перетестировать всё.

С абстракцией `IScheduledJob` + `IJobScheduler` переход выглядит так:

1. Сменить `<ProjectReference>` в `.csproj` сервиса: `Job.Quartz` → `Job.Hangfire`;
2. Пересобрать. Если есть лямбда-джобы — заменить их на классовые (Hangfire не сериализует closure);
3. Запустить тесты.

Бизнес-джобы не меняются вообще. Подробности — в [Migration Guide](migration-guide.md) и [Zero-Touch Proof](zero-touch-proof.md).

## Поток выполнения

### Задача в виде класса

```
1. Program.cs → builder.ImplementDependencies()
                 └─▶ AddReferencedDependencyInjectors()
                      └─▶ QuartzDependencyInjector.Process() / HangfireDependencyInjector.Process()
                           ├─▶ AddQuartz() / AddHangfire()
                           ├─▶ AddSingleton<IJobScheduler, ...>()
                           └─▶ AddHostedService<...Bootstrapper>()

2. Сервис в DependencyInjector:
   services.AddJobs(opts => opts.AddJob<MyJob>(new JobSchedule.Cron("0 0/5 * * * ?")));
   ├─▶ AddSingleton<JobSchedulerOptions>   { Definitions: [ { JobKey, JobType, ... } ] }
   ├─▶ AddSingleton<IScheduledJobExecutor, ScheduledJobExecutor>
   └─▶ AddEnumerable<IScheduledJobMiddleware>(CorrelationId, Logging, Retry)

3. app.Build() → HostedServices.StartAsync:
   └─▶ QuartzJobSchedulerBootstrapper / HangfireJobSchedulerBootstrapper
        ├─▶ scheduler.ScheduleAsync(def) для каждой JobDefinition
        └─▶ (Quartz) scheduler.Start()

4. Cron tick → Quartz/Hangfire trigger:
   └─▶ QuartzScheduledJobAdapter.Execute(ctx) / HangfireScheduledJobAdapter.RunScheduledJob(...)
        ├─▶ Restore JobType (и Action для лямбд) из JobDataMap / args
        ├─▶ new ScheduledJobContext(...) { JobType, Action, ServiceProvider, CancellationToken }
        └─▶ executor.ExecuteAsync(ctx)
             └─▶ middleware1(next=middleware2(next=middleware3(next=Terminal)))
                  Terminal:
                    • ctx.JobType != null → sp.GetRequiredService(JobType) → IScheduledJob.ExecuteAsync
                    • ctx.Action != null  → ctx.Action(ctx.ServiceProvider, ctx.CancellationToken)
```

### Задача, заданная делегатом

Отличается только точкой 4: вместо получения задачи из DI терминал вызывает захваченный `Func<IServiceProvider, CancellationToken, Task>` через `ctx.Action(ctx.ServiceProvider, ctx.CancellationToken)`.

### Полная диаграмма (задача в виде класса)

```
┌──────────────┐    ┌────────────────────┐    ┌─────────────────────┐
│ JobScheduler │    │ JobSchedulerBoot-  │    │  Quartz / Hangfire  │
│ Options      │───▶│ strapper           │───▶│  Trigger/Recurring  │
│ {Definitions}│    │ (IHostedService)   │    │  (next tick)        │
└──────────────┘    └────────────────────┘    └──────────┬──────────┘
                                                         │
                                                         ▼
                       ┌──────────────────────────────────────┐
                       │  QuartzScheduledJobAdapter           │
                       │  или HangfireScheduledJobAdapter     │
                       │  (internal)                          │
                       └──────────────────┬───────────────────┘
                                          │ restore JobType / Action
                                          ▼
                        ┌──────────────────────────────────────┐
                        │  ScheduledJobExecutor (Pipeline)     │
                        │  CorrelationId → Logging → Retry →   │
                        │  Terminal                            │
                       └──────────────────┬───────────────────┘
                                          │ for class: resolve
                                          ▼
                       ┌──────────────────────────────────────┐
                       │  IScheduledJob (бизнес-джоба)        │
                       │  ExecuteAsync(ct)                    │
                       └──────────────────────────────────────┘
```

## Связанные документы

| Документ | Описание |
|----------|----------|
| [Job Scheduler (top-level)](../job-scheduler.md) | Обзор и Quick Start |
| [Pipeline](pipeline.md) | Подробности о middleware и executor |
| [Quartz Adapter](quartz-adapter.md) | Конкретная Quartz-реализация |
| [Hangfire Adapter](hangfire-adapter.md) | Конкретная Hangfire-реализация |
| [Design](design.md) | Контракты и обоснование |
| [Migration Guide](migration-guide.md) | Переезд с `QuartzJobWrapper` |
| [Zero-Touch Proof](zero-touch-proof.md) | Доказательство нулевых правок при смене провайдера |
| [Pipeline Behaviors](../pipeline-behaviors.md) | Аналог для MediatR (концептуально близок) |
