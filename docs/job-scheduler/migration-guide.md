# Migration Guide: QuartzJobWrapper → IScheduledJob

**Сборка:** `Shared.Application.Core.dll` (новый API), `Shared.Infrastructure.Job.Quartz.dll` (legacy)
**Цель:** перевести существующее приложение с legacy `QuartzJobRegistrar` / `QuartzJobWrapper` на новый абстрактный `IScheduledJob` + `AddJobs(...)`.

---

## Цель

Старый API (`QuartzJobRegistrar` / `QuartzJobWrapper`) утекал Quartz-типами в бизнес-код и требовал service-locator. Новый API:

- изолирует Quartz/Hangfire в Infrastructure-адаптере;
- даёт джобам чистый контракт `ExecuteAsync(CancellationToken)`;
- DI через конструктор;
- единый pipeline (Logging / Correlation / Retry) — не нужно копировать boilerplate в каждую джобу;
- смена провайдера (Quartz ↔ Hangfire) = смена ProjectReference, без правок джоб.

## До (legacy)

```csharp
// В Startup.cs / Program.cs
services
    .RegisterJob<MyJob>("0 0/5 * * * ?")
    .RegisterJob(
        jobKey: "CleanExpiredSessions",
        cronExpression: "0 0/30 * * * ?",
        job: async (sp, ct) =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            await dbContext.CleanExpiredSessionsAsync(ct);
        })
    .RegisterCacheJob(
        cacheKey: "ActiveUsers",
        cronExpression: "0 0/5 * * * ?",
        getOrCreateCacheFunc: async sp =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            return await dbContext.Users.Where(u => u.IsActive).ToListAsync();
        })
    .RegisterDbSeederJob();
```

```csharp
// Старая джоба
public class MyJob : QuartzJobWrapper
{
    public MyJob(IServiceProvider serviceProvider, ILogger<MyJob> logger)
        : base(serviceProvider, logger) { }

    protected override async Task ProcessAsync(
        IJobExecutionContext context,
        CancellationToken ct)
    {
        // ручной try/finally + correlation-id + retry — boilerplate
        var correlationIdCreated = JobCorrelationContext.TrySetCorrelationId();
        try
        {
            // ... бизнес-логика
        }
        finally
        {
            if (correlationIdCreated) JobCorrelationContext.ClearCorrelationId();
        }
    }
}
```

## После (новый API)

```csharp
// В DependencyInjector сервиса
services
    .AddJobs(opts => opts
        .AddJob<MyJob>(new JobSchedule.Cron("0 0/5 * * * ?"))
        .AddJob<CleanExpiredSessionsJob>(new JobSchedule.Cron("0 0/30 * * * ?")))
    .AddCronCacheJob(
        cacheKey: "ActiveUsers",
        cronExpression: "0 0/5 * * * ?",
        getOrCreateFunc: async sp =>
        {
            var dbContext = sp.GetRequiredService<AppDbContext>();
            return await dbContext.Users.Where(u => u.IsActive).ToListAsync();
        })
    .RegisterDbSeederJob();   // без изменений — extension в Application.Core
```

```csharp
// Новая джоба
public sealed class MyJob(
    AppDbContext dbContext,
    ILogger<MyJob> logger) : IScheduledJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("MyJob: starting");
        // ... бизнес-логика
        // Logging/Correlation/Retry — в pipeline, не здесь
    }
}
```

## Шаги миграции

### Шаг 1. Обновить using

```diff
- using Shared.Infrastructure.Job.Quartz;   // legacy
+ using Shared.Application.Core.Job;         // новый API
+ using Shared.Application.Core.Job.Extensions;   // AddJobs / AddCronCacheJob
```

Если в файле был `using Quartz;` (для `IJob`, `IJobExecutionContext`) — удалите: больше не нужны.

### Шаг 2. Заменить базу класса

```diff
- public class MyJob : QuartzJobWrapper
+ public sealed class MyJob(AppDbContext db, ILogger<MyJob> logger) : IScheduledJob
  {
-     public MyJob(IServiceProvider sp, ILogger<MyJob> logger) : base(sp, logger) { }
-
-     protected override async Task ProcessAsync(IJobExecutionContext ctx, CancellationToken ct)
+     public async Task ExecuteAsync(CancellationToken cancellationToken = default)
      {
          // ...
      }
  }
```

**Правила:**

| Правило | Пояснение |
|---------|-----------|
| Убрать `IServiceProvider` из конструктора | Зависимости — через DI в конструктор (автоматическое получение). |
| Убрать базовый `QuartzJobWrapper` | Реализовать `IScheduledJob` напрямую. |
| `ProcessAsync(IJobExecutionContext, CT)` → `ExecuteAsync(CT)` | Убрать Quartz-тип из сигнатуры. |
| Сделать `sealed` | Финальный класс — без наследников. |
| Убрать ручной `JobCorrelationContext` | Correlation — в `CorrelationIdMiddleware`. |
| Убрать ручной `try/finally` для повторных попыток | Повторные попытки — в `RetryMiddleware`. |
| Убрать ручной `LogInformation("executing/completed")` | Логирование — в `LoggingMiddleware`. |

### Шаг 3. Заменить `RegisterJob<T>(cron)` на `AddJob<T>(new JobSchedule.Cron(cron))`

| Legacy | Новое |
|--------|-------|
| `RegisterJob<MyJob>("0 0/5 * * * ?")` | `AddJob<MyJob>(new JobSchedule.Cron("0 0/5 * * * ?"))` (внутри `AddJobs(opts => ...)`) |
| `RegisterJob<MyJob>(JobTriggerFlags.Daily, TimeSpan.FromHours(3))` | `AddJob<MyJob>(new JobSchedule.Flags(JobTriggerFlags.Daily, TimeSpan.FromHours(3)))` |
| `RegisterJob<MyJob>(JobTriggerFlags.OnStartup, TimeSpan.Zero)` | `AddJob<MyJob>(new JobSchedule.OnStartup())` |
| `RegisterJob(jobKey, "0 0/5 * * * ?", async (sp, ct) => ...)` | `AddJob<MyClassJob>(new JobSchedule.Cron("0 0/5 * * * ?"))` (замените делегат на класс) |
| `RegisterCacheJob<TData>(key, cron, factory)` | `AddCronCacheJob<TData>(key, cron, factory)` (прямо на `IServiceCollection`) |
| `RegisterCacheJob<TData>(key, flags, time, factory)` | `AddFlagsCacheJob<TData>(key, flags, time, factory)` |
| `RegisterDbSeederJob()` | `RegisterDbSeederJob()` (без изменений) |

### Шаг 4. Удалить `using Quartz;` из бизнес-кода

```powershell
PS> Get-ChildItem -Path F:\template\src\Services -Recurse -Include *.cs |
      Where-Object { (Get-Content $_.FullName) -match 'using Quartz' } |
      Select-Object FullName
# Должно вернуть 0 строк.
```

Если есть legacy QuartzJobRegistrar / QuartzJobWrapper в коде, который пока не мигрирован — оставьте его в `Setter.Infrastructure/Quartz/`, но пометьте как `[Obsolete]`. Цель — постепенно мигрировать по одной джобе.

### Шаг 5. Build + тесты

```bash
cd F:\template
dotnet build src\Template.sln
dotnet test src\Tests\Shared\Job\Shared.Infrastructure.Job.Quartz.Tests
```

## Чеклист миграции

- [ ] `grep -r "using Quartz" src/Services/` возвращает 0 (бизнес-код).
- [ ] Все джобы реализуют `IScheduledJob` (не `: QuartzJobWrapper`).
- [ ] Все регистрации идут через `AddJobs(opts => opts.AddJob<T>(...))` (не `RegisterJob<T>`).
- [ ] `RegisterJob<T>(...)` не используется в бизнес-коде.
- [ ] В DI-конфигурации сервиса подключён `AddJobs(...)`.
- [ ] Кэш-джобы зарегистрированы через `AddCronCacheJob` / `AddFlagsCacheJob`.
- [ ] `dotnet build` зелёный.
- [ ] Все тесты проходят.

## Связанные документы

| Документ | Описание |
|----------|----------|
| [Job Scheduler (top-level)](../job-scheduler.md) | Обзор нового API |
| [Architecture](architecture.md) | Слои |
| [Quartz Adapter](quartz-adapter.md) | Адаптер |
| [Zero-Touch Proof](zero-touch-proof.md) | Доказательство нулевых правок |
