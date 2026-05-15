# Quartz.NET Job Scheduling

**Сборка:** `Shared.Infrastructure.Job.Quartz.dll`  
**Namespace:** `Shared.Infrastructure.Job.Quartz`  
**Исходники:** `src/Shared/Job/Shared.Infrastructure.Job.Quartz/`

---

## 🚀 Quick Start

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Регистрация Quartz + задач
builder.Services
    // CRON-выражение + lambda action
    .RegisterJob(
        jobKey: "CleanExpiredSessions",
        cronExpression: "0 0/30 * * * ?",  // Каждые 30 минут
        job: async (sp, ct) =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            await dbContext.CleanExpiredSessionsAsync(ct);
        })

    // CRON-выражение + typed IJob
    .RegisterJob<ReportGenerationJob>("0 0 2 * * ?")  // Ежедневно в 02:00

    // JobTriggerFlags + lambda action
    .RegisterJob(
        jobKey: "RefreshCache",
        trigger: JobTriggerFlags.EveryHour,
        job: async (sp, ct) => await sp.GetRequiredService<ICacheService>().RefreshAsync(ct),
        specificTime: TimeSpan.Zero)

    // Scheduled cache refresh
    .RegisterCacheJob(
        cacheKey: "ActiveUsers",
        cronExpression: "0 0/5 * * * ?",  // Каждые 5 минут
        getOrCreateCacheFunc: async sp =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            return await dbContext.Users.Where(u => u.IsActive).ToListAsync();
        });

var app = builder.Build();
app.Run();
```

---

## 📐 Архитектура

Модуль Quartz предоставляет декларативную регистрацию фоновых задач поверх **Quartz.NET**:

| Компонент | Назначение |
|-----------|-----------|
| `QuartzJobRegistrar` | Extension-методы для регистрации задач в DI |
| `QuartzJobWrapper` | Обёртка lambda actions как Quartz `IJob` |
| `JobTriggerFlags` | Flags enum для декларативного scheduling |

### Поток выполнения

```
┌─────────────────────────────────────────────────────────┐
│  Program.cs: .RegisterJob(...)                           │
└──────────────────────┬──────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────┐
│  QuartzJobRegistrar                                      │
│  • AddQuartz(q => { ... })                               │
│  • q.AddJob<TJob>() / q.AddJob<QuartzJobWrapper>()       │
│  • q.AddTrigger() / q.AddTrigger() x N (для flags)       │
└──────────────────────┬──────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────┐
│  Quartz Scheduler                                        │
│  • CronSchedule / CalendarIntervalSchedule               │
│  • Trigger firing → IJob.Execute()                       │
└──────────────────────┬──────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────┐
│  QuartzJobWrapper (для lambda jobs)                      │
│  • JobCorrelationContext.TrySetCorrelationId()           │
│  • ProcessAsync() → invoke action                        │
│  • Auto-retry on failure (5 min delay)                   │
│  • JobCorrelationContext.ClearCorrelationId()            │
└──────────────────────┬──────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────┐
│  User Action: Func<IServiceProvider, CancellationToken,  │
│               Task>                                      │
└─────────────────────────────────────────────────────────┘
```

---

## 📝 RegisterJob()

`QuartzJobRegistrar` предоставляет **4 overload** метода `RegisterJob()` для гибкой регистрации задач.

### Overload 1: CRON + Lambda Action

```csharp
public static IServiceCollection RegisterJob(
    this IServiceCollection serviceCollection,
    string jobKey,
    string cronExpression,
    Func<IServiceProvider, CancellationToken, Task> job)
```

| Параметр | Описание |
|----------|----------|
| `jobKey` | Уникальный ключ задачи для идентификации в планировщике |
| `cronExpression` | CRON-выражение (например, `"0 0/5 * * * ?"` — каждые 5 минут) |
| `job` | Lambda-действие, выполняемое при запуске задачи |

**Пример:**

```csharp
services.RegisterJob(
    jobKey: "SendDailyReport",
    cronExpression: "0 0 9 * * ?",  // Ежедневно в 09:00
    job: async (sp, ct) =>
    {
        var emailService = sp.GetRequiredService<IEmailService>();
        await emailService.SendDailyReportAsync(ct);
    });
```

---

### Overload 2: CRON + Typed IJob

```csharp
public static IServiceCollection RegisterJob<TJob>(
    this IServiceCollection serviceCollection,
    string cronExpression)
    where TJob : IJob
```

| Параметр | Описание |
|----------|----------|
| `TJob` | Тип задачи, реализующий `Quartz.IJob` |
| `cronExpression` | CRON-выражение |

**Пример:**

```csharp
// Typed job
public class DataSyncJob : IJob
{
    private readonly IExternalApiService _apiService;
    private readonly ILogger<DataSyncJob> _logger;

    public DataSyncJob(IExternalApiService apiService, ILogger<DataSyncJob> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting data sync...");
        await _apiService.SyncDataAsync(context.CancellationToken);
    }
}

// Регистрация
services.RegisterJob<DataSyncJob>("0 0 */4 * * ?");  // Каждые 4 часа
```

**JobKey** автоматически генерируется как `typeof(TJob).FullName`.

---

### Overload 3: JobTriggerFlags + Lambda Action

```csharp
public static IServiceCollection RegisterJob(
    this IServiceCollection serviceCollection,
    string jobKey,
    JobTriggerFlags trigger,
    Func<IServiceProvider, CancellationToken, Task> job,
    TimeSpan specificTime)
```

| Параметр | Описание |
|----------|----------|
| `jobKey` | Уникальный ключ задачи |
| `trigger` | Флаги триггеров (можно комбинировать через `|`) |
| `job` | Lambda-действие |
| `specificTime` | Время выполнения для Daily/Weekly/Monthly триггеров |

**Пример:**

```csharp
services.RegisterJob(
    jobKey: "CleanupTempFiles",
    trigger: JobTriggerFlags.Daily | JobTriggerFlags.OnStartup,
    job: async (sp, ct) =>
    {
        var fileService = sp.GetRequiredService<IFileService>();
        await fileService.CleanupTempFilesAsync(ct);
    },
    specificTime: new TimeSpan(3, 0, 0));  // В 03:00
```

---

### Overload 4: JobTriggerFlags + Typed IJob

```csharp
public static IServiceCollection RegisterJob<TJob>(
    this IServiceCollection serviceCollection,
    JobTriggerFlags trigger,
    TimeSpan specificTime)
    where TJob : IJob
```

**Пример:**

```csharp
services.RegisterJob<MetricsAggregationJob>(
    trigger: JobTriggerFlags.EveryHour,
    specificTime: TimeSpan.Zero);
```

---

## 🚩 JobTriggerFlags

**Файл:** `Shared.Application.Core/Job/JobTriggerFlags.cs`

Flags enum для декларативного scheduling. Можно комбинировать через `|`.

| Флаг | Значение | Описание | Schedule |
|------|----------|----------|----------|
| `Daily` | `1 << 0` | Ежедневно | Calendar interval: 1 день |
| `Weekly` | `1 << 1` | Еженедельно | Calendar interval: 1 неделя |
| `Monthly` | `1 << 2` | Ежемесячно | Calendar interval: 1 месяц |
| `OnStartup` | `1 << 3` | При запуске приложения | `StartNow()` |
| `EveryMinute` | `1 << 4` | Каждую минуту | Calendar interval: 1 минута |
| `EveryHour` | `1 << 5` | Каждый час | Calendar interval: 1 час |

### Комбинирование флагов

```csharp
// Задача выполнится при запуске И ежедневно в указанное время
trigger: JobTriggerFlags.OnStartup | JobTriggerFlags.Daily

// Задача выполнится каждую минуту И каждый час (оба триггера)
trigger: JobTriggerFlags.EveryMinute | JobTriggerFlags.EveryHour
```

### Алгоритм создания триггеров

```csharp
private static void CreateTriggerForFlag(
    IServiceCollectionQuartzConfigurator configurator,
    JobTriggerFlags flag,
    string jobKey,
    TimeSpan specificTime)
{
    configurator.AddTrigger(opt =>
    {
        opt.ForJob(jobKey).WithIdentity($"{jobKey}.{flagName}.trigger");

        if (flag != JobTriggerFlags.OnStartup)
        {
            opt.StartAt(DateBuilder.DateOf(specificTime.Hours, specificTime.Minutes, 0));
        }

        switch (flag)
        {
            case JobTriggerFlags.OnStartup:
                opt.StartNow();
                break;
            case JobTriggerFlags.EveryMinute:
                opt.WithCalendarIntervalSchedule(b => b.WithIntervalInMinutes(1));
                break;
            case JobTriggerFlags.EveryHour:
                opt.WithCalendarIntervalSchedule(b => b.WithIntervalInHours(1));
                break;
            case JobTriggerFlags.Daily:
                opt.WithCalendarIntervalSchedule(b => b.WithIntervalInDays(1));
                break;
            case JobTriggerFlags.Weekly:
                opt.WithCalendarIntervalSchedule(b => b.WithIntervalInWeeks(1));
                break;
            case JobTriggerFlags.Monthly:
                opt.WithCalendarIntervalSchedule(b => b.WithIntervalInMonths(1));
                break;
        }
    });
}
```

---

## 🔄 QuartzJobWrapper

**Файл:** `Shared.Infrastructure.Job.Quartz/QuartzJobWrapper.cs`

Обёртка для lambda-действий, позволяющая передавать `Func<IServiceProvider, CancellationToken, Task>` как Quartz `IJob`.

### Ключевые особенности

| Фича | Описание |
|------|----------|
| **Correlation ID** | Устанавливает `JobCorrelationContext` для трассировки |
| **Logging** | Логирует начало и завершение выполнения |
| **Auto-retry** | При ошибке — повтор через 5 минут (SimpleTriggerImpl) |
| **DI Resolution** | Action получает `IServiceProvider` для резолва сервисов |

### Implementation

```csharp
public class QuartzJobWrapper(
    IServiceProvider serviceProvider,
    ILogger<QuartzJobWrapper> logger) : IJob
{
    public const string JobActionKey = "JobAction";

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var correlationIdCreated = JobCorrelationContext.TrySetCorrelationId();

        try
        {
            await ProcessAsync(context, cancellationToken);
        }
        catch (Exception ex)
        {
            // Auto-retry через 5 минут
            var retryTrigger = new SimpleTriggerImpl(Guid.NewGuid().ToString())
            {
                Description = "RetryTrigger",
                RepeatCount = 0,
                JobKey = context.JobDetail.Key,
                StartTimeUtc = DateBuilder.NextGivenMinuteDate(DateTime.Now, 5),
            };
            await context.Scheduler.ScheduleJob(retryTrigger, cancellationToken);
            throw new JobExecutionException(ex, false);
        }
        finally
        {
            if (correlationIdCreated)
            {
                JobCorrelationContext.ClearCorrelationId();
            }
        }
    }

    protected virtual async Task ProcessAsync(
        IJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Job {JobKey} is executing.", context.JobDetail.Key);
        await (context.JobDetail.JobDataMap[JobActionKey]
            as Func<IServiceProvider, CancellationToken, Task>)!
            .Invoke(serviceProvider, cancellationToken);
        logger.LogInformation("Job {JobKey} is completed.", context.JobDetail.Key);
    }
}
```

### Поток Correlation ID

```
Execute()
  │
  ├─ JobCorrelationContext.TrySetCorrelationId()  → correlationIdCreated = true
  │
  ├─ ProcessAsync()
  │     │
  │     ├─ logger.LogInformation("executing")
  │     ├─ action.Invoke(serviceProvider, ct)
  │     └─ logger.LogInformation("completed")
  │
  ├─ [EXCEPTION] → Schedule retry (5 min) → throw JobExecutionException
  │
  └─ finally → JobCorrelationContext.ClearCorrelationId()  (если correlationIdCreated)
```

### Retry поведение

| Параметр | Значение |
|----------|----------|
| **Trigger type** | `SimpleTriggerImpl` |
| **Repeat count** | `0` (однократный retry) |
| **Delay** | 5 минут от текущего времени |
| **JobKey** | Тот же, что у оригинальной задачи |
| **Refire** | `false` (Quartz не будет refire автоматически) |

---

## 💾 RegisterCacheJob

Регистрация задачи для **scheduled cache refresh** — автоматическое обновление кэша по расписанию.

### Overload 1: CRON + Cache

```csharp
public static IServiceCollection RegisterCacheJob<TData>(
    this IServiceCollection serviceCollection,
    string cacheKey,
    string cronExpression,
    Func<IServiceProvider, Task<TData>> getOrCreateCacheFunc)
```

| Параметр | Описание |
|----------|----------|
| `TData` | Тип данных кэша |
| `cacheKey` | Уникальный ключ кэша (job key = `"{cacheKey}.job"`) |
| `cronExpression` | CRON-выражение для расписания обновления |
| `getOrCreateCacheFunc` | Функция создания/обновления кэша |

**Пример:**

```csharp
services.RegisterCacheJob<IEnumerable<User>>(
    cacheKey: "ActiveUsers",
    cronExpression: "0 0/5 * * * ?",  // Каждые 5 минут
    getOrCreateCacheFunc: async sp =>
    {
        var dbContext = sp.GetRequiredService<AppDbContext>();
        return await dbContext.Users
            .Where(u => u.IsActive)
            .ToListAsync();
    });
```

**Что происходит:**

1. Регистрируется `ICacheService<TData>` через `RegisterCacheService(cacheKey, func)`
2. Регистрируется Quartz-задача с ключом `"ActiveUsers.job"`
3. Задача вызывает `cacheService.UpdateCacheAsync()` по CRON-расписанию

---

### Overload 2: JobTriggerFlags + Cache

```csharp
public static IServiceCollection RegisterCacheJob<TData>(
    this IServiceCollection serviceCollection,
    string cacheKey,
    JobTriggerFlags trigger,
    TimeSpan specificTime,
    Func<IServiceProvider, Task<TData>> getOrCreateCacheFunc)
```

**Пример:**

```csharp
services.RegisterCacheJob<Dictionary<string, Configuration>>(
    cacheKey: "AppConfig",
    trigger: JobTriggerFlags.EveryHour | JobTriggerFlags.OnStartup,
    specificTime: TimeSpan.Zero,
    getOrCreateCacheFunc: async sp =>
    {
        var configService = sp.GetRequiredService<IConfigurationService>();
        return await configService.LoadAllAsync();
    });
```

---

## 🔗 Интеграция с CQRS/Services

### Job → CQRS Command

```csharp
// Job вызывает CQRS Command через MediatR
services.RegisterJob(
    jobKey: "ProcessExpiredOrders",
    cronExpression: "0 0 1 * * ?",  // Ежедневно в 01:00
    job: async (sp, ct) =>
    {
        var mediator = sp.GetRequiredService<IMediator>();
        await mediator.Send(new ProcessExpiredOrdersCommand(), ct);
    });
```

### Job → Service Layer

```csharp
services.RegisterJob<NightlyDataSyncJob>("0 0 3 * * ?");

public class NightlyDataSyncJob : IJob
{
    private readonly IPersonsService _personsService;
    private readonly ILogger<NightlyDataSyncJob> _logger;

    public NightlyDataSyncJob(IPersonsService personsService, ILogger<NightlyDataSyncJob> logger)
    {
        _personsService = personsService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting nightly data sync...");

        var listRequest = new PersonListRequest(new DalPattern());
        var persons = await _personsService.GetListAsync(listRequest, context.CancellationToken);

        foreach (var person in persons.Items)
        {
            // Sync logic...
        }

        _logger.LogInformation("Nightly data sync completed.");
    }
}
```

### Cache Job → Read Query

```csharp
// Scheduled cache для ReadListQuery результатов
services.RegisterCacheJob<PersonListPayload>(
    cacheKey: "PersonListCache",
    cronExpression: "0 0/10 * * * ?",
    getOrCreateCacheFunc: async sp =>
    {
        var mediator = sp.GetRequiredService<IMediator>();
        var query = new PersonReadListQuery(
            new PersonListRequest(new DalPattern()) { PageSize = 1000 });
        var response = await mediator.Send(query);
        return response.Payload;
    });
```

---

## 📝 Best Practices

1. **Уникальные jobKey** — используйте осмысленные ключи, избегайте конфликтов
2. **CancellationToken** — всегда передавайте в async-операции для graceful shutdown
3. **Idempotency** — задачи должны быть идемпотентными (retry может вызвать повторное выполнение)
4. **Logging** — QuartzJobWrapper уже логирует start/complete, добавляйте детализацию в action
5. **Correlation ID** — автоматически устанавливается, используйте `JobCorrelationContext.GetCurrent()` для трассировки
6. **Cache Jobs** — используйте для данных, которые редко меняются, но часто читаются
7. **CRON expressions** — тестируйте выражения на [crontab.guru](https://crontab.guru)
8. **Error handling** — auto-retry срабатывает один раз через 5 минут; для сложных retry-стратегий используйте typed `IJob`

---

---

## См. также

| Документ | Описание |
|----------|----------|
| [Cache](cache.md) | Cache Service — RegisterCacheService, UpdateCacheAsync |
| [Correlation ID](correlation-id.md) | JobCorrelationContext — трассировка фоновых задач |
| [Configuration](configuration.md) | DI регистрация, ServiceCollection extensions |
