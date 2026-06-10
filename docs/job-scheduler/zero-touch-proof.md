# Zero-Touch Proof: смена провайдера без правок бизнес-кода

**Цель:** доказать, что переход `Shared.Infrastructure.Job.Quartz` → `Shared.Infrastructure.Job.Hangfire` (или обратно) **не требует изменений** в сервисах и доменном коде — только в `.csproj` и DI.

---

## 1. Фоновая задача: `HelloWorldJob`

`src/Services/Bff/Template.Bff.Application/Jobs/HelloWorldJob.cs`:

```csharp
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Job;

namespace Template.Bff.Application.Jobs;

/// <summary>
/// Тестовая фоновая задача. НЕ зависит от Quartz/Hangfire.
/// </summary>
public sealed class HelloWorldJob(ILogger<HelloWorldJob> logger) : IScheduledJob
{
    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Hello from background at {Time}", DateTime.UtcNow);
        return Task.CompletedTask;
    }
}
```

**Что важно:**

- ❌ `using Quartz;` — отсутствует.
- ❌ `using Hangfire;` — отсутствует.
- ✅ `using Shared.Application.Core.Job;` — единственная зависимость.
- ✅ DI через конструктор.
- ✅ Единственный метод — `ExecuteAsync(CancellationToken)`.

## 2. Регистрация в DI: `Bff.Application.DependencyInjection`

```csharp
public class DependencyInjector(ILoggerFactory loggerFactory) : DependencyInjectorBase(loggerFactory)
{
    protected override IServiceCollection Process(IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSingleton<HelloWorldJob>()
            .AddJobs(opts => opts.AddJob<HelloWorldJob>(new JobSchedule.OnStartup()));
    }
}
```

**Что важно:**

- ❌ `using Shared.Infrastructure.Job.Quartz;` — отсутствует.
- ❌ `using Shared.Infrastructure.Job.Hangfire;` — отсутствует.
- ✅ `using Shared.Application.Core.Job;` + `.Extensions` — только абстракции.

## 3. Program.cs: без правок

```csharp
using Shared.Presentation.Core.Extensions;
using Template.Presentation.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.ImplementDependencies();   // ← внутри — AddReferencedDependencyInjectors

var app = builder.Build();
app.UseCommonPresentation();
app.Run();
```

`Program.cs` **не знает** какой именно Job-адаптер подключён. `ImplementDependencies()` поднимает все `DependencyInjector`-ы из ссылочных сборок через reflection (`AddReferencedDependencyInjectors`).

## 4. Смена провайдера: одна строка в `.csproj`

**Quartz (по умолчанию):**

```xml
<!-- src/Services/Bff/Template.Bff.Application/Template.Bff.Application.csproj -->
<ItemGroup>
  <ProjectReference Include="..\..\..\Shared\Job\Shared.Infrastructure.Job.Quartz\Shared.Infrastructure.Job.Quartz.csproj" />
</ItemGroup>
```

**Замена на Hangfire:**

```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\Shared\Job\Shared.Infrastructure.Job.Hangfire\Shared.Infrastructure.Job.Hangfire.csproj" />
</ItemGroup>
```

**Правки в коде:** 0.

## 5. Альтернатива: зарегистрировать оба адаптера, переключать флагом

Если хочется переключать провайдер без перекомпиляции:

```csharp
#if USE_HANGFIRE
    services.AddHangfire(config => config.UseInMemoryStorage());
    services.AddHangfireServer();
    services.AddSingleton<IJobScheduler, HangfireJobScheduler>();
#else
    services.AddQuartz(configure => configure
        .UseMicrosoftDependencyInjectionJobFactory()
        .UseInMemoryStore());
    services.AddSingleton<IJobScheduler, QuartzJobScheduler>();
#endif
```

Но это **не обязательно** — `AddReferencedDependencyInjectors` сам поднимет ровно тот адаптер, на который есть `ProjectReference`.

## 6. Доказательство в коде: Bff содержит `HelloWorldJob`, без Quartz-импортов

```powershell
PS> Get-ChildItem -Path src\Services\Bff\Template.Bff.Application\Jobs -Recurse -Include *.cs |
      ForEach-Object { Select-String -Path $_.FullName -Pattern 'using (Quartz|Hangfire)' }
# Должно вернуть 0 строк.
```

## 7. Скрипт-переключатель (опционально)

Создайте `src/Services/Bff/Template.Bff.Api/switch-provider.sh` (для Linux/macOS) или `.ps1` (для Windows):

### PowerShell

```powershell
# switch-job-provider.ps1
param(
    [Parameter(Mandatory)]
    [ValidateSet('Quartz', 'Hangfire')]
    [string]$Provider
)

$csproj = "src/Services/Bff/Template.Bff.Api/Template.Bff.Api.csproj"
$content = Get-Content $csproj -Raw

$content = $content -replace 'Shared\\Infrastructure\\Job\\Quartz', "Shared\Infrastructure\Job\$Provider"
$content = $content -replace 'Shared\\Infrastructure\\Job\\Hangfire', "Shared\Infrastructure\Job\$Provider"

Set-Content -Path $csproj -Value $content -NoNewline
Write-Host "Provider switched to $Provider"
```

### Bash

```bash
#!/bin/bash
# switch-job-provider.sh
set -e
PROVIDER=${1:-Quartz}
CSPROJ="src/Services/Bff/Template.Bff.Api/Template.Bff.Api.csproj"

sed -i "s|Shared.Infrastructure.Job.Quartz|Shared.Infrastructure.Job.${PROVIDER}|g" "$CSPROJ"
sed -i "s|Shared.Infrastructure.Job.Hangfire|Shared.Infrastructure.Job.${PROVIDER}|g" "$CSPROJ"

echo "Provider switched to $PROVIDER"
```

```bash
chmod +x switch-job-provider.sh
./switch-job-provider.sh Quartz
./switch-job-provider.sh Hangfire
```

## 8. Сравнение: что меняется при смене провайдера

| Файл/аспект | Quartz | Hangfire |
|--------------|--------|----------|
| `HelloWorldJob.cs` | без изменений | без изменений |
| `Bff.Application/DependencyInjection.cs` | без изменений | без изменений |
| `Program.cs` | без изменений | без изменений |
| `Bff.Api.csproj` | `Job.Quartz` | `Job.Hangfire` |
| Shared Pipeline (Logging/Correlation/Retry) | через `QuartzScheduledJobAdapter` | через `HangfireScheduledJobAdapter` |
| Quartz/Hangfire-типы в бизнес-коде | ❌ нет | ❌ нет |
| Задачи, заданные делегатом | ✅ | ❌ (только задачи в виде класса) |

## Связанные документы

| Документ | Описание |
|----------|----------|
| [Job Scheduler (top-level)](../job-scheduler.md) | Обзор |
| [Architecture](architecture.md) | Слои |
| [Quartz Adapter](quartz-adapter.md) | Quartz-реализация |
| [Hangfire Adapter](hangfire-adapter.md) | Hangfire-реализация |
