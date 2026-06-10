# NLog Configuration

## Обзор

**Assembly:** `Shared.Infrastructure.Logging.dll`  
**Namespace:** `Shared.Infrastructure.Logging`

Структурированное логирование через NLog с поддержкой correlation IDs для HTTP-запросов и фоновых задач (jobs). Включает кастомные layout renderers, автоматическую регистрацию и инъекцию correlation ID во все target'ы.

---

## NlogSettings

**Класс:** `NlogSettings` (`Shared.Infrastructure.Logging.Settings`)

Конфигурация NLog для DI-регистрации.

| Свойство | Тип | Описание | Значение по умолчанию |
|----------|-----|----------|----------------------|
| `Path` | `string` (required) | Путь к файлу `nlog.config` | обязателен к заполнению |
| `LogLevel` | `LogLevel` (required) | Минимальный уровень логирования | `LogLevel.Information` (default применяется только при отсутствии значения) |

### Пример регистрации

```csharp
builder.Services.AddSingleton(new NlogSettings
{
    Path = Path.Combine(AppContext.BaseDirectory, "nlog.config"),
    LogLevel = LogLevel.Information
});
```

---

## Custom Layout Renderers

### CorrelationIdScopePropertyKeys

**Класс:** `CorrelationIdScopePropertyKeys` (`Shared.Infrastructure.Logging.Constants`)

Константы для ключей correlation ID в NLog ScopeContext:

| Константа | Значение | Назначение |
|-----------|----------|------------|
| `Http` | `"http-correlation-id"` | Correlation ID HTTP-запросов |
| `Job` | `"job-correlation-id"` | Correlation ID фоновых задач |

### HttpCorrelationIdLayoutRenderer

**Класс:** `HttpCorrelationIdLayoutRenderer` (`Shared.Infrastructure.Logging.LayoutRenderers`)  
**Layout:** `${http-correlation-id}`

Извлекает correlation ID из `HttpContext.Request` через extension-метод `GetCorrelationId()`. Наследуется от `AspNetLayoutRendererBase`, поэтому имеет доступ к `HttpContextAccessor`.

```csharp
[LayoutRenderer(CorrelationIdScopePropertyKeys.Http)]
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

### JobCorrelationIdLayoutRenderer

**Класс:** `JobCorrelationIdLayoutRenderer` (`Shared.Infrastructure.Logging.LayoutRenderers`)  
**Layout:** `${job-correlation-id}`

Извлекает correlation ID из `JobCorrelationContext.GetCorrelationId()` для фоновых задач (Quartz, MassTransit consumers и т.д.).

```csharp
[LayoutRenderer(CorrelationIdScopePropertyKeys.Job)]
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

---

## Автоматическая конфигурация

### LoggingConfigurationExtensions

**Класс:** `LoggingConfigurationExtensions` (`Shared.Infrastructure.Logging.Extensions`)

Метод `AddCorrelationIdToTargetLayouts()` автоматически:

1. **Регистрирует** оба layout renderer'а через `LogManager.Setup().SetupExtensions()` (однократно, thread-safe через `lock` + `volatile`)
2. **Инъектирует** correlation ID block в layout каждого target'а (кроме консольных `coloredSystemEventConsole` и `coloredBusinessEventConsole`)
3. **Вставляет** блок `corId=` **после** `${message}` и **перед** `${logger}` в существующий layout

### Correlation ID Block

```
${when:when='${http-correlation-id}'!='':inner= corId=${http-correlation-id}}
${when:when='${job-correlation-id}'!='':inner= corId=${job-correlation-id}}
```

Блок выводит `corId=<id>` только если correlation ID установлен. Если оба ID отсутствуют — ничего не выводится.

### Логика вставки

Метод `InsertCorrelationIdIntoLayout()` находит позицию закрывающей `}` после `${message` и вставляет correlation ID block сразу после него:

```
До:   time=${longdate} level=${level} msg=${message} logger=${logger}
После: time=${longdate} level=${level} msg=${message}${when:...corId=...}${when:...corId=...} logger=${logger}
```

### Пример использования

```csharp
// В Program.cs или Startup
var config = LogManager.Configuration;
config.AddCorrelationIdToTargetLayouts();
LogManager.ReconfigExistingLoggers();
```

---

## Пример вывода лога

### HTTP-запрос

```
2024-03-15 14:30:00.0000 INFO corId=a1b2c3d4-e5f6-7890-abcd-ef1234567890 msg=Заказ создан logger=Setter.Api.Controllers.OrdersController
```

### Фоновая задача

```
2024-03-15 14:30:00.0000 INFO corId=job-00123 msg=Обработка очереди завершена logger=Setter.Infrastructure.Jobs.OrderProcessingJob
```

---

---

## См. также

| Документ | Описание |
|----------|----------|
| [Correlation ID](correlation-id.md) | Механизм propagation correlation IDs |
| [Logging](logging.md) | Общее руководство по логированию |
| [Job Scheduler](job-scheduler.md) | Фоновые задачи и JobCorrelationContext |
