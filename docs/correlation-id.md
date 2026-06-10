# Correlation ID: Distributed Tracing для HTTP и Background Jobs

**Assembly:** `Shared.Application.Core.dll`, `Shared.Presentation.Core.dll`, `Shared.Infrastructure.Logging.dll`  
**Namespaces:** `Shared.Application.Core.CorrelationId`, `Shared.Presentation.Core.CorrelationId.Middlewares`, `Shared.Infrastructure.Logging.LayoutRenderers`  
**Исходники:** `src/Shared/Core/Shared.Application.Core/CorrelationId/`, `src/Shared/Core/Shared.Presentation.Core/CorrelationId/`, `src/Shared/Logging/Shared.Infrastructure.Logging/`

---

## 🚀 Quick Start

```csharp
// Program.cs — middleware добавляется автоматически через UsePresentationCore
app.UsePresentationCore(); // внутри регистрирует CorrelationIdMiddleware

// NLog config — добавление correlation ID в layout
<target xsi:type="Console" name="console">
  <layout>
    ${longdate} | ${level:uppercase=true} | ${http-correlation-id} | ${job-correlation-id} | ${message}
  </layout>
</target>

// Результат в логах:
// 2024-01-15 10:30:45.1234 | INFO | a1b2c3d4-e5f6-7890-abcd-ef1234567890 | | PersonsService.GetListAsync started.
// 2024-01-15 10:30:45.5678 | INFO | a1b2c3d4-e5f6-7890-abcd-ef1234567890 | f1e2d3c4-b5a6-7890-1234-567890abcdef | CacheRefreshJob executed.
```

---

## Обзор

Correlation ID — это уникальный идентификатор, который проходит через всю цепочку обработки запроса: от HTTP-запроса через background jobs до логов. Позволяет:

- **Трассировать** один запрос через все сервисы и компоненты
- **Связывать** HTTP-запрос с порождёнными background jobs
- **Фильтровать логи** по одному идентификатору для отладки проблем
- **Аудировать** — кто и когда вызвал операцию

### Архитектура

```
HTTP Request (X-Correlation-Id header)
         ↓
  CorrelationIdMiddleware  ← генерирует если отсутствует
         ↓
  ┌──────┴──────┐
  ↓             ↓
HTTP Logs    Background Job
${http-         ${job-
correlation-id} correlation-id}
```

---

## HTTP Correlation

### Constants

```csharp
namespace Shared.Application.Core.CorrelationId;

public static class Constants
{
    /// <summary>
    /// Заголовок для идентификатора корреляции запроса.
    /// </summary>
    public const string CorrelationIdHeader = "X-Correlation-Id";
}
```

Единая константа для имени HTTP-заголовка. Используйте её вместо хардкода строки `"X-Correlation-Id"`.

### CorrelationIdMiddleware

```csharp
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        context.Request.TryAddCorrelationId();
        context.Response.Headers[CorrelationIdConstants.CorrelationIdHeader] =
            context.Request.Headers[CorrelationIdConstants.CorrelationIdHeader];
        return next(context);
    }
}
```

**Что делает:**

1. **TryAddCorrelationId()** — если в запросе нет валидного `X-Correlation-Id`, генерирует новый `Guid`
2. **Echo в response** — копирует correlation ID из request headers в response headers
3. **Передаёт дальше** — вызывает следующий middleware в pipeline

### HttpRequestExtensions

```csharp
public static class HttpRequestExtensions
{
    // Получить correlation ID из заголовка (или null если отсутствует)
    public static Guid? GetCorrelationId(this HttpRequest? request);

    // Добавить correlation ID если отсутствует (возвращает true если добавлен)
    public static bool TryAddCorrelationId(this HttpRequest? request);
}
```

#### GetCorrelationId

Парсит заголовок `X-Correlation-Id` и возвращает `Guid?`:

```csharp
public static Guid? GetCorrelationId(this HttpRequest? request)
{
    if (request?.Headers.TryGetValue(Constants.CorrelationIdHeader, out var headerValue) == true
        && !string.IsNullOrWhiteSpace(headerValue)
        && Guid.TryParse(headerValue.ToString(), out var correlationId))
    {
        return correlationId;
    }
    return null;
}
```

#### TryAddCorrelationId

Генерирует новый `Guid` только если заголовок отсутствует или невалиден:

```csharp
public static bool TryAddCorrelationId(this HttpRequest? request)
{
    if (request == null || request.HasValidCorrelationId())
    {
        return false; // уже есть валидный ID
    }

    request.Headers[Constants.CorrelationIdHeader] = Guid.NewGuid().ToString("D");
    return true; // новый ID сгенерирован
}
```

**Формат GUID:** `"D"` — стандартный формат с дефисами (`a1b2c3d4-e5f6-7890-abcd-ef1234567890`).

### Регистрация Middleware

Middleware регистрируется автоматически при вызове `UsePresentationCore()`:

```csharp
// Program.cs
app.UsePresentationCore(); // CorrelationIdMiddleware добавляется внутрь
```

Если регистрируете вручную — middleware должен быть **как можно раньше** в pipeline:

```csharp
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseRouting();
app.UseAuthentication();
// ...
```

---

## Job Correlation — Bridging HTTP to Background Jobs

### JobCorrelationContext

```csharp
public static class JobCorrelationContext
{
    private static readonly AsyncLocal<Guid?> CorrelationId = new();

    public static Guid? GetCorrelationId();
    public static bool TrySetCorrelationId();
    public static void ClearCorrelationId();
}
```

`AsyncLocal<Guid?>` обеспечивает хранение correlation ID в контексте выполнения — значение доступно во всех async-методах текущего flow, но изолировано между разными потоками.

### API

| Метод | Описание | Возвращает |
|-------|----------|-----------|
| `GetCorrelationId()` | Получить текущий correlation ID job | `Guid?` (null если не установлен) |
| `TrySetCorrelationId()` | Установить новый correlation ID (**однократно**) | `true` если установлен, `false` если уже был |
| `ClearCorrelationId()` | Очистить correlation ID | `void` |

### Once-Only Semantics

`TrySetCorrelationId()` устанавливает значение **только один раз** — повторные вызовы игнорируются:

```csharp
public static bool TrySetCorrelationId()
{
    if (CorrelationId.Value.HasValue)
    {
        return false; // уже установлен — не перезаписываем
    }

    CorrelationId.Value = Guid.NewGuid();
    return true;
}
```

Это предотвращает случайную перезапись correlation ID, если job вызывает другие компоненты, которые тоже пытаются установить его.

### Использование в фоновых задачах Quartz

```csharp
public class CacheRefreshJob : IJob
{
    public async Task Execute(IJobExecutionContext context, CancellationToken ct)
    {
        // Устанавливаем correlation ID для этой фоновой задачи
        JobCorrelationContext.TrySetCorrelationId();

        // Теперь все логи внутри фоновой задачи будут содержать job-correlation-id
        _logger.LogInformation("Starting cache refresh");

        await _cacheService.UpdateCacheAsync();

        _logger.LogInformation("Cache refresh completed");

        // Очистка после выполнения
        JobCorrelationContext.ClearCorrelationId();
    }
}
```

### Связывание HTTP → фоновая задача

Когда HTTP-запрос порождает фоновую задачу, correlation ID можно передать:

```csharp
// В HTTP-обработчике
public async Task<IActionResult> TriggerImport(
    [FromBody] ImportRequest request, CancellationToken ct)
{
    var correlationId = HttpContext.Request.GetCorrelationId();

    // Передаём correlation ID в фоновую задачу через очередь сообщений
    await _bus.Publish(new ImportCommand
    {
        Data = request.Data,
        CorrelationId = correlationId // связываем фоновую задачу с HTTP-запросом
    }, ct);

    return Accepted();
}

// В обработчике фоновой задачи
public class ImportCommandHandler : IConsumer<ImportCommand>
{
    public async Task Consume(ConsumeContext<ImportCommand> context)
    {
        // Устанавливаем correlation ID из сообщения
        if (context.Message.CorrelationId.HasValue)
        {
            // Можно установить через AsyncLocal если нужно
            JobCorrelationContext.TrySetCorrelationId();
        }

        await _importService.ImportAsync(context.Message.Data);
    }
}
```

---

## Logging Integration — NLog Layout Renderers

### CorrelationIdScopePropertyKeys

```csharp
namespace Shared.Infrastructure.Logging.Constants;

public static class CorrelationIdScopePropertyKeys
{
    /// <summary>
    /// Ключ для идентификатора корреляции HTTP запросов в NLog ScopeContext.
    /// </summary>
    public const string Http = "http-correlation-id";

    /// <summary>
    /// Ключ для идентификатора корреляции фоновых задач в NLog ScopeContext.
    /// </summary>
    public const string Job = "job-correlation-id";
}
```

Константы для имён layout renderer'ов в NLog конфигурации.

### HttpCorrelationIdLayoutRenderer

```csharp
[LayoutRenderer(CorrelationIdScopePropertyKeys.Http)] // "http-correlation-id"
public class HttpCorrelationIdLayoutRenderer : AspNetLayoutRendererBase
{
    protected override void Append(StringBuilder builder, LogEventInfo logEvent)
    {
        var httpContext = HttpContextAccessor?.HttpContext;
        var correlationId = httpContext?.Request.GetCorrelationId();
        if (correlationId.HasValue)
        {
            builder.Append(correlationId.Value.ToString());
        }
    }
}
```

**Имя в NLog config:** `${http-correlation-id}`

Извлекает correlation ID из текущего `HttpContext`. Работает только в контексте HTTP-запроса.

### JobCorrelationIdLayoutRenderer

```csharp
[LayoutRenderer(CorrelationIdScopePropertyKeys.Job)] // "job-correlation-id"
public class JobCorrelationIdLayoutRenderer : LayoutRenderer
{
    protected override void Append(StringBuilder builder, LogEventInfo logEvent)
    {
        var correlationId = JobCorrelationContext.GetCorrelationId();
        if (correlationId.HasValue)
        {
            builder.Append(correlationId.Value.ToString());
        }
    }
}
```

**Имя в NLog config:** `${job-correlation-id}`

Извлекает correlation ID из `JobCorrelationContext` (AsyncLocal). Работает в контексте background jobs.

### NLog Configuration

```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">

  <targets>
    <!-- Console с correlation IDs -->
    <target xsi:type="Console" name="console">
      <layout>
        ${longdate} | ${level:uppercase=true:padding=-5} | ${http-correlation-id} | ${job-correlation-id} | ${message}
      </layout>
    </target>

    <!-- File с correlation IDs -->
    <target xsi:type="File" name="file" fileName="logs/app.log">
      <layout>
        ${longdate} | ${level:uppercase=true} | ${http-correlation-id} | ${job-correlation-id} | ${logger} | ${message} ${exception:format=tostring}
      </layout>
    </target>
  </targets>

  <rules>
    <logger name="*" minlevel="Info" writeTo="console,file" />
  </rules>
</nlog>
```

### Пример вывода логов

**HTTP-запрос:**
```
2024-01-15 10:30:45.1234 | INFO  | a1b2c3d4-e5f6-7890-abcd-ef1234567890 | | CorrelationIdMiddleware.InvokeAsync started.
2024-01-15 10:30:45.2345 | INFO  | a1b2c3d4-e5f6-7890-abcd-ef1234567890 | | PersonsService.GetListAsync started.
2024-01-15 10:30:45.3456 | INFO  | a1b2c3d4-e5f6-7890-abcd-ef1234567890 | | PersonsService.GetListAsync completed.
2024-01-15 10:30:45.3457 | INFO  | a1b2c3d4-e5f6-7890-abcd-ef1234567890 | | PersonsService.GetListAsync processed time: 111ms.
```

**Фоновая задача:**
```
2024-01-15 10:35:00.0001 | INFO  | | f1e2d3c4-b5a6-7890-1234-567890abcdef | CacheRefreshJob started.
2024-01-15 10:35:00.1234 | INFO  | | f1e2d3c4-b5a6-7890-1234-567890abcdef | CacheService.UpdateCacheAsync started.
2024-01-15 10:35:00.5678 | INFO  | | f1e2d3c4-b5a6-7890-1234-567890abcdef | CacheService.UpdateCacheAsync completed.
```

**HTTP-запрос породил фоновую задачу:**
```
2024-01-15 10:30:45.1234 | INFO  | a1b2c3d4-e5f6-7890-abcd-ef1234567890 | | ImportController.TriggerImport started.
2024-01-15 10:30:45.2345 | INFO  | a1b2c3d4-e5f6-7890-abcd-ef1234567890 | | ImportCommand published to queue.
2024-01-15 10:30:46.0001 | INFO  | a1b2c3d4-e5f6-7890-abcd-ef1234567890 | f1e2d3c4-b5a6-7890-1234-567890abcdef | ImportJob started (triggered by a1b2c3d4).
2024-01-15 10:30:50.5678 | INFO  | a1b2c3d4-e5f6-7890-abcd-ef1234567890 | f1e2d3c4-b5a6-7890-1234-567890abcdef | ImportJob completed.
```

> В последнем примере оба correlation ID присутствуют в логах фоновой задачи, позволяя связать её с исходным HTTP-запросом.

---

### Расширения HttpRequest

`Shared.Application.Core` предоставляет расширения для работы с Correlation ID в HTTP-запросах:

| Метод | Описание |
|-------|----------|
| `HttpRequestExtensions.GetCorrelationId(this HttpRequest)` | Извлекает `X-Correlation-Id` из заголовков запроса |
| `HttpRequestExtensions.TryAddCorrelationId(this HttpRequest?)` | Добавляет сгенерированный `Guid.NewGuid()` в заголовок запроса, если валидное значение отсутствует |
| `HttpContextAccessorExtensions.GetCorrelationId(this IHttpContextAccessor)` | Удобный доступ к Correlation ID через `IHttpContextAccessor` |

Константа `CorrelationIdHeader = "X-Correlation-Id"` используется как имя заголовка.

---

## См. также

| Документ | Описание |
|----------|----------|
| [Logging](logging.md) | Логирование методов: LogTask и [LogMethod] |
| [API Client](api-client.md) | HTTP-клиенты для внешних сервисов |
| [Job Scheduler](job-scheduler.md) | Планировщик — AddJobs, JobTriggerFlags |
| [Request Logging](request-logging.md) | Логирование HTTP-запросов |
| [NLog Configuration](nlog-configuration.md) | Структурированное логирование через NLog |
