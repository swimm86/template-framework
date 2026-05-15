# 📝 Request Logging — Логирование аргументов контроллера

> **Assembly:** `Shared.Presentation.Core.dll`  
> **Namespace:** `Shared.Presentation.Core.RequestLogging`

---

## 1. Обзор

Фильтр **Request Logging** автоматически логирует аргументы всех контроллеров в формате JSON. Это критически важно для:

- **Отладки** — понимание, какие данные пришли в endpoint
- **Аудита** — отслеживание действий пользователей
- **Мониторинга** — анализ паттернов использования API

### Проблемы, которые решает фильтр

| Проблема | Решение |
|----------|---------|
| Логирование бинарных файлов | `IFormFile` заменяется на `<file>` |
| Логирование паролей/токенов | `[DoNotLog]` заменяет значение на `***` |
| Переполнение логов большими payload | Ограничение `MaxJsonPayloadLength` |
| Циклические ссылки в объектах | `ReferenceHandler.IgnoreCycles` |
| Блокировка потока запроса | Выполняется **после** model binding, без буферизации |

---

## 2. RequestLoggingFilter

### 2.1. Обзор

`RequestLoggingFilter` — это `IAsyncActionFilter`, который выполняется **после model binding**, когда все аргументы контроллера уже десериализованы и находятся в памяти.

```csharp
public sealed class RequestLoggingFilter(
    ILogger<RequestLoggingFilter> logger)
    : IAsyncActionFilter
{
    public Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next);
}
```

### 2.2. Как работает

```
Request → Model Binding → RequestLoggingFilter → Action → Response
                                ↓
                    Аргументы → ProcessObject() → JSON → HttpContext.Items
```

1. Извлекает аргументы из `ActionExecutingContext.ActionArguments`
2. Пропускает `CancellationToken` и `IFormFileCollection`
3. Заменяет `IFormFile` на заглушку `<file>`
4. Для сложных объектов вызывает `ProcessObject()`:
   - Свойства с `[DoNotLog]` → `***`
   - Свойства типа `IFormFile` → `<file>`
   - Остальные свойства сериализуются как есть
5. Сериализует результат в JSON
6. Проверяет размер payload против `MaxJsonPayloadLength`
7. Сохраняет JSON в `HttpContext.Items["RequestArgumentsKey"]` для использования в ExceptionHandler

### 2.3. Кэш сериализации

Для производительности фильтр кэширует информацию о типах в `ConcurrentDictionary<Type, TypeSerializationInfo?>`:

```csharp
private sealed record TypeSerializationInfo(
    PropertyInfo[] Properties,          // Свойства для прямой сериализации
    Dictionary<PropertyInfo, string> RedactedProperties);  // Свойства-заглушки
```

Кэш заполняется лениво при первом encountering типа и переиспользуется для всех последующих запросов.

### 2.4. Обработка больших payload

Если сериализованный JSON превышает `MaxJsonPayloadLength`, фильтр возвращает валидный JSON с ошибкой вместо усечения:

```json
{
  "error": "payload_too_large",
  "length": 15728640,
  "maxAllowed": 10485760
}
```

### 2.5. Регистрация фильтра

```csharp
builder.Services.AddControllers(options =>
{
    options.Filters.Add<RequestLoggingFilter>();
});
```

---

## 3. Атрибут `[DoNotLog]`

### 3.1. Назначение

Атрибут `[DoNotLog]` маркирует свойства, которые **не должны попадать в логи**. Используется для защиты чувствительных данных:

- Пароли
- Токены авторизации
- Номер карты
- Персональные данные

```csharp
[AttributeUsage(AttributeTargets.Property)]
public sealed class DoNotLogAttribute : Attribute;
```

### 3.2. Пример использования

```csharp
using Shared.Presentation.Core.RequestLogging.Attributes;

namespace MyService.Api.Requests;

public record CreateUserRequest
{
    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    [DoNotLog]
    public string Password { get; init; } = string.Empty;

    [DoNotLog]
    public string? Token { get; init; }
}
```

### 3.3. Результат логирования

**Исходный запрос:**

```json
{
  "name": "Иван Иванов",
  "email": "ivan@example.com",
  "password": "s3cr3t_p@ss",
  "token": "eyJhbGciOiJIUzI1NiIs..."
}
```

**Запись в логе:**

```json
{
  "name": "Иван Иванов",
  "email": "ivan@example.com",
  "password": "***",
  "token": "***"
}
```

### 3.4. Приоритет обработки

| Тип свойства | Заглушка |
|--------------|----------|
| `IFormFile` | `<file>` |
| `[DoNotLog]` | `***` |
| Обычное свойство | Сериализуется как есть |

---

## 4. Configuration — RequestLoggingSettings

### 4.1. Настройки

```csharp
public record RequestLoggingSettings
{
    public int MaxDepth { get; init; } = 50;
    public int MaxJsonPayloadLength { get; init; } = 10 * 1024 * 1024;  // 10MB
    public bool IsEnabled { get; init; } = true;
}
```

| Параметр | По умолчанию | Описание |
|----------|--------------|----------|
| `MaxDepth` | `50` | Максимальная глубина сериализации вложенных объектов |
| `MaxJsonPayloadLength` | `10485760` (10MB) | Максимальный размер JSON payload в байтах |
| `IsEnabled` | `true` | Включён/выключен фильтр |

### 4.2. Настройка через конфигурацию

Настройки загружаются из environment variables через `ConfigurationBuilder`:

**appsettings.json:**

```json
{
  "RequestLoggingSettings": {
    "MaxDepth": 32,
    "MaxJsonPayloadLength": 5242880,
    "IsEnabled": true
  }
}
```

**Environment variables:**

```bash
REQUESTLOGGINGSETTINGS__MAXDEPTH=32
REQUESTLOGGINGSETTINGS__MAXJSONPAYLOADLENGTH=5242880
REQUESTLOGGINGSETTINGS__ISENABLED=true
```

### 4.3. Отключение логирования

Для отключения логирования (например, в production для высоконагруженных endpoint'ов):

```json
{
  "RequestLoggingSettings": {
    "IsEnabled": false
  }
}
```

Или через environment variable:

```bash
REQUESTLOGGINGSETTINGS__ISENABLED=false
```

---

## 5. Интеграция с ExceptionHandler

Аргументы сохраняются в `HttpContext.Items` и могут быть использованы в exception handler для логирования запроса при ошибке:

```csharp
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var requestArgs = context.Items[RequestLoggingFilter.RequestArgumentsKey] as string;

        logger.LogError(
            "Ошибка при обработке запроса. Аргументы: {RequestArgs}",
            requestArgs);

        // ... формирование ErrorResponse
    });
});
```

---

## 6. JsonSerializerOptions

Фильтр использует собственные настройки сериализации:

```csharp
private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
{
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    ReferenceHandler = ReferenceHandler.IgnoreCycles,
    MaxDepth = Settings.MaxDepth
};
```

| Настройка | Значение | Зачем |
|-----------|----------|-------|
| `JsonNamingPolicy.CamelCase` | camelCase | Соответствие веб-стандартам |
| `JsonStringEnumConverter` | Enum как строки | Читаемость логов |
| `ReferenceHandler.IgnoreCycles` | Игнор циклов | Предотвращение StackOverflow |
| `MaxDepth` | Из настроек | Защита от глубокой вложенности |

---

---

## См. также

| Документ | Описание |
|----------|----------|
| [Logging](logging.md) | Общее логирование в Shared |
| [Controllers](controllers.md) | Контроллеры и фильтры |
| [Correlation ID](correlation-id.md) | Корреляция запросов |
