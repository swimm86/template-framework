# Маппинг Исключений (Exception Mapping)

**Сборка:** `Shared.Presentation.Core`  
**Namespace:** `Shared.Presentation.Core.Exceptions`

---

## Быстрый старт

```csharp
// Program.cs — одна строка регистрации
builder.Services.AddExceptionHandling();
```

Все исключения автоматически преобразуются в RFC 7807 Problem Details с корректным HTTP-статусом:

```csharp
// Любой controller / Minimal API
[HttpGet("{id}")]
public async Task<PersonDto> Get(int id, CancellationToken ct)
{
    // NotFoundException → 404 с телом ErrorResponse
    // BusinessLogicException → 422 с телом ErrorResponse
    // ValidationException → 400 с телом ErrorResponse
    return await _mediator.Send(new GetPersonByIdQuery(id), ct);
}
```

---

## Архитектура

Маппинг исключений построен на паттерне **Chain of Responsibility** с порядконезависимым резолвом по иерархии типов.

```
┌─────────────────────────────────────────────────┐
│              ASP.NET Core Pipeline               │
│         (IExceptionHandler middleware)            │
└──────────────────────┬──────────────────────────┘
                       │ Exception
                       ▼
┌─────────────────────────────────────────────────┐
│              ExceptionHandler                    │
│   (IExceptionHandler.TryHandleAsync)             │
└──────────────────────┬──────────────────────────┘
                       │ Map(exception)
                       ▼
┌─────────────────────────────────────────────────┐
│          ExceptionMapperResolver                 │
│   (hierarchy walk: type → BaseType → ... → Exception)
└──┬──────────┬──────────┬──────────┬─────────────┘
   │          │          │          │
   ▼          ▼          ▼          ▼
┌──────┐  ┌──────┐  ┌──────┐  ┌──────────┐
│Mapper│  │Mapper│  │Mapper│  │Default   │
│ 404  │  │ 422  │  │ 400  │  │Mapper 500│
└──────┘  └──────┘  └──────┘  └──────────┘
```

### Ключевые принципы

| Принцип | Описание |
|---------|----------|
| **Порядконезависимость** | Порядок регистрации мапперов в DI **не влияет** на результат |
| **Most derived wins** | Для исключения выбирается маппер самого производного типа |
| **Fallback** | `DefaultExceptionMapper` обрабатывает всё, что не замаплено явно |
| **Auto-registration** | Все `IExceptionMapper` регистрируются автоматически через `AssemblyHelper` |

---

## Core Interfaces

### IExceptionMapper (non-generic)

```csharp
// Shared.Presentation.Core/Exceptions/Interfaces/IExceptionMapper.cs
public interface IExceptionMapper
{
    /// Тип исключения, который обрабатывает данный маппер
    Type HandledType { get; }

    /// Преобразует исключение в ErrorResponse
    ErrorResponse Map(Exception exception);
}
```

Не-generic контракт используется `ExceptionMapperResolver` для динамического резолва маппера по типу исключения.

### IExceptionMapper<TException> (generic)

```csharp
public interface IExceptionMapper<in TException> : IExceptionMapper
    where TException : Exception
{
    /// Типобезопасный маппинг конкретного исключения
    ErrorResponse Handle(TException exception);
}
```

Generic-контракт обеспечивает type-safety при реализации. Non-generic `Map` делегирует в `Handle` после проверки типа.

---

## ExceptionMapperResolver

**Файл:** `Shared.Presentation.Core/Exceptions/ExceptionMapperResolver.cs`

Резолвер обходит иерархию типов исключения от наиболее производного к базовому и возвращает первый найденный зарегистрированный маппер.

### Алгоритм hierarchy walk

```csharp
public ErrorResponse Map(Exception exception)
{
    // Walk: exception.GetType() → BaseType → ... → Exception
    for (var type = exception.GetType(); type is not null; type = type.BaseType)
    {
        if (_map.TryGetValue(type, out var mapper))
        {
            return mapper.Map(exception);  // Самый производный выигрывает
        }
    }

    throw new InvalidOperationException(
        $"Не зарегистрирован маппер для типа {exception.GetType().Name}");
}
```

### Пример работы

| Брошенное исключение | Обход иерархии | Выбранный маппер |
|---------------------|----------------|------------------|
| `NotFoundException` | `NotFoundException` → `AppException` → `Exception` | `NotFoundExceptionMapper` (404) |
| `BusinessLogicException` | `BusinessLogicException` → `AppException` → `Exception` | `BusinessLogicExceptionMapper` (422) |
| `ArgumentNullException` | `ArgumentNullException` → `ArgumentException` → `SystemException` → `Exception` | `DefaultExceptionMapper` (500) |
| `AggregateException` | `AggregateException` → `Exception` | `AggregateExceptionMapper` (500) |

### Валидация при инициализации

```csharp
public ExceptionMapperResolver(IEnumerable<IExceptionMapper> mappers)
{
    _map = CreateMap(mappers);

    // DefaultExceptionMapper ОБЯЗАТЕЛЕН — гарантирует fallback для любого исключения
    if (_map.GetValueOrDefault(typeof(Exception)) == null)
    {
        throw new InvalidOperationException(
            "DefaultExceptionMapper (IExceptionMapper<Exception>) не зарегистрирован");
    }
}
```

### Защита от дубликатов

```csharp
private static Dictionary<Type, IExceptionMapper> CreateMap(IEnumerable<IExceptionMapper> mappers)
{
    var result = new Dictionary<Type, IExceptionMapper>();
    foreach (var mapper in mappers)
    {
        if (!result.TryAdd(mapper.HandledType, mapper))
        {
            throw new InvalidOperationException(
                $"Для типа {mapper.HandledType.Name} зарегистрировано несколько мапперов");
        }
    }
    return result;
}
```

---

## ExceptionMapperBase<TException>

**Файл:** `Shared.Presentation.Core/Exceptions/Mappers/Base/ExceptionMapperBase.cs`

Базовый класс для всех мапперов. Реализует:

- `HandledType` → `typeof(TException)`
- `Map(Exception)` → type-check → делегирование в `Handle(TException)`
- Форматирование `Details` (stack trace с защитой от циклических ссылок)
- Конфигурация через `ExceptionMapperSettings`

### Структура ErrorResponse

```csharp
public record ErrorResponse : ResponseBase, IWithAdditionalData
{
    /// HTTP-статус код ответа
    public int StatusCode { get; init; }

    /// Коллекция ProblemDetails (RFC 7807)
    public IReadOnlyCollection<ProblemDetails> Errors { get; init; }

    /// Подробное описание (stack trace + inner exceptions)
    public string? Details { get; init; }

    /// Дополнительные данные для потребителей API
    public IReadOnlyDictionary<string, object>? AdditionalData { get; init; }
}
```

### Переопределяемые методы

| Метод | Назначение | Default |
|-------|-----------|---------|
| `GetResponseStatusCode(TException)` | HTTP-статус код | **abstract** — обязательно override |
| `Title` | Заголовок ошибки | **abstract** — обязательно override |
| `ShouldEnrichWithTrace` | Добавлять stack trace в `Details` | `_settings.ShouldEnrichWithTrace` |
| `GetAdditionalData(TException)` | Доп. данные из исключения | `null` |
| `GetProblemDetailsDetail(TException)` | Текст `ProblemDetails.Detail` | `exception.Message` |
| `GetProblemDetails(TException)` | Коллекция `ProblemDetails` | Single item с `Title` + `Detail` |

---

## Таблица всех мапперов

| # | Маппер | Обрабатываемый тип | HTTP Status | Title |
|---|--------|-------------------|-------------|-------|
| 1 | `DefaultExceptionMapper` | `Exception` | **500** Internal Server Error | "Ошибка сервера" |
| 2 | `AppExceptionMapper` | `AppException` | **500** Internal Server Error | "Ошибка приложения" |
| 3 | `NotFoundExceptionMapper` | `NotFoundException` | **404** Not Found | "Ошибка - не найден" |
| 4 | `BusinessLogicExceptionMapper` | `BusinessLogicException` | **422** Unprocessable Entity | "Ошибка бизнес-логики" |
| 5 | `UnauthorizedExceptionMapper` | `UnauthorizedException` | **401** Unauthorized | "Пользователь не аутентифицирован" |
| 6 | `ValidationExceptionMapper` | `FluentValidation.ValidationException` | **400** Bad Request | "Ошибка валидации" |
| 7 | `ProxiedExceptionMapper` | `ProxiedException` | **dynamic** (из `exception.StatusCode`) | `string.Empty` |
| 8 | `AggregateExceptionMapper` | `AggregateException` | **500** Internal Server Error | "Ошибка сервера" |

---

## ProxiedException и ProxiedExceptionMapper

### ProxiedException

**Файл:** `Shared.Application.Core/Exceptions/Models/ProxiedException.cs`

Исключение, возникающее при проксировании HTTP-запросов к внешним сервисам. Содержит полную информацию об ошибке от upstream-сервиса.

```csharp
public class ProxiedException : AppException
{
    /// Детали ошибки в формате RFC 7807 (ProblemDetails)
    public ProblemDetails ProblemDetails { get; }

    /// HTTP-статус код ошибки от upstream-сервиса
    public int StatusCode { get; }

    /// Типизированное получение дополнительных данных
    public bool TryGetAdditionalData<T>(string key, out T? value);
}
```

### ProxiedExceptionMapper

**Файл:** `Shared.Presentation.Core/Exceptions/Mappers/ProxiedExceptionMapper.cs`

Ключевые особенности:

| Особенность | Значение |
|-------------|----------|
| `ShouldEnrichWithTrace` | **`false`** — проксированные ошибки содержат данные от upstream, а не локальный stack trace |
| `StatusCode` | **Динамический** — берётся из `exception.StatusCode` |
| `ProblemDetails` | **Полная копия** — `Title`, `Detail`, `Instance`, `Type`, `Extensions` передаются как есть от upstream |
| `AdditionalData` | **Доступно** — через `AppExceptionMapperBase` передаётся из `AppException.AdditionalData` |

```csharp
public sealed class ProxiedExceptionMapper(IConfiguration configuration)
    : AppExceptionMapperBase<ProxiedException>(configuration)
{
    protected override string Title => string.Empty;
    protected override bool ShouldEnrichWithTrace => false;
    protected override int GetResponseStatusCode(ProxiedException exception)
        => exception.StatusCode;

    protected override IReadOnlyCollection<ProblemDetails> GetProblemDetails(
        ProxiedException exception)
    {
        // Полная копия ProblemDetails от upstream-сервиса
        var result = new ProblemDetails
        {
            Status = exception.StatusCode,
            Title = exception.ProblemDetails.Title,
            Detail = exception.ProblemDetails.Detail,
            Instance = exception.ProblemDetails.Instance,
            Type = exception.ProblemDetails.Type,
            Extensions = exception.ProblemDetails.Extensions,
        };
        return [result];
    }
}
```

---

## AggregateExceptionMapper

**Файл:** `Shared.Presentation.Core/Exceptions/Mappers/AggregateExceptionMapper.cs`

Маппирует `AggregateException` в набор `ProblemDetails`, делегируя каждое внутреннее исключение соответствующему мапперу.

### Ключевые особенности

| Особенность | Описание |
|-------------|----------|
| `Flatten()` | Раскрывает вложенные `AggregateException`, предотвращая рекурсию |
| **Lazy resolver** | `IExceptionMapperResolver` резолвится через `IServiceProvider` при первом вызове (избегание circular dependency) |
| **StatusCode** | Всегда **500** — агрегированная ошибка не может быть представлена одним HTTP-статусом |
| **Errors** | Объединённая коллекция `ProblemDetails` от всех внутренних исключений |

```csharp
public sealed class AggregateExceptionMapper(
    IConfiguration configuration,
    IServiceProvider serviceProvider)
    : ExceptionMapperBase<AggregateException>(configuration)
{
    private IExceptionMapperResolver? _resolver;

    protected override int GetResponseStatusCode(AggregateException exception)
        => StatusCodes.Status500InternalServerError;

    protected override IReadOnlyCollection<ProblemDetails> GetProblemDetails(
        AggregateException exception)
    {
        var innerExceptions = exception.Flatten().InnerExceptions;
        if (innerExceptions.Count == 0)
        {
            return base.GetProblemDetails(exception);
        }

        _resolver ??= serviceProvider.GetRequiredService<IExceptionMapperResolver>();

        return innerExceptions
            .SelectMany(inner => _resolver.Map(inner).Errors)
            .ToArray();
    }
}
```

### Почему lazy resolver?

```
Dispatcher → Mappers → AggregateMapper → Dispatcher  (circular!)
```

`AggregateExceptionMapper` зависит от `IExceptionMapperResolver`, который зависит от всех мапперов. Чтобы избежать circular dependency, resolver резолвится лениво через `IServiceProvider` при первом вызове.

---

## ExceptionHandler

**Файл:** `Shared.Presentation.Core/Exceptions/ExceptionHandler.cs`

Реализация `Microsoft.AspNetCore.Diagnostics.IExceptionHandler` — центральная точка перехвата необработанных исключений.

```csharp
internal sealed class ExceptionHandler(IExceptionMapperResolver exceptionMapperDispatcher)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // 1. Маппинг исключения в ErrorResponse
        var response = exceptionMapperDispatcher.Map(exception);

        // 2. Установка HTTP-статуса
        httpContext.Response.StatusCode = response.StatusCode;

        // 3. Запись JSON-тела ответа
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        // 4. Исключение обработано (дальнейшие обработчики не вызываются)
        return true;
    }
}
```

---

## ExceptionMapperSettings

**Файл:** `Shared.Presentation.Core/Exceptions/Settings/ExceptionMapperSettings.cs`

```csharp
public record ExceptionMapperSettings(
    bool ShouldEnrichWithTrace = true,
    int StackTraceDepth = 10,
    int MaxExceptionDepth = 5);
```

| Параметр | Default | Описание |
|----------|---------|----------|
| `ShouldEnrichWithTrace` | `true` | Добавлять stack trace и детали в `ErrorResponse.Details`. Переопределите в `false` для мапперов, которые не должны обогащать ответ (например, `ProxiedExceptionMapper`) |
| `StackTraceDepth` | `10` | Количество строк стека вызовов. Баланс между полезностью отладки и размером ответа |
| `MaxExceptionDepth` | `5` | Максимальная глубина `InnerException` для защиты от циклических ссылок |

### Обоснование MaxExceptionDepth = 5

| Глубина | Покрытие |
|---------|----------|
| 1-2 уровня | 95% исключений |
| 3-4 уровня | 99% исключений |
| 5 уровней | Edge-cases: `AggregateException` + `ProxiedException` + вложенные `AppException` |

Увеличение свыше 5 не даёт диагностической ценности, но растёт риск переполнения стека.

### Конфигурация (appsettings.json)

```json
{
  "ExceptionMapperSettings": {
    "ShouldEnrichWithTrace": false,
    "StackTraceDepth": 5,
    "MaxExceptionDepth": 3
  }
}
```

---

## DI Регистрация

**Файл:** `Shared.Presentation.Core/Exceptions/Extensions/DependencyInjectionExtensions.cs`

```csharp
public static IServiceCollection AddExceptionHandling(this IServiceCollection services)
{
    return services
        .AddProblemDetails()                              // Microsoft.AspNetCore.Mvc.ProblemDetails
        .AddExceptionMappers()                            // Auto-register all IExceptionMapper
        .AddSingleton<IExceptionMapperResolver, ExceptionMapperResolver>()
        .AddExceptionHandler<ExceptionHandler>();         // ASP.NET Core IExceptionHandler
}
```

### Что происходит внутри

| Шаг | Действие |
|-----|----------|
| 1 | `AddProblemDetails()` — регистрирует сервисы ProblemDetails |
| 2 | `AddExceptionMappers()` — сканирует все сборки через `AssemblyHelper.GetDerivedTypesFromAssemblies<IExceptionMapper>()`, исключает типы с `[ManualConfigurationAttribute]`, регистрирует каждый как `Singleton` |
| 3 | `AddSingleton<IExceptionMapperResolver, ExceptionMapperResolver>()` — резолвер с валидацией наличия `DefaultExceptionMapper` |
| 4 | `AddExceptionHandler<ExceptionHandler>()` — регистрирует глобальный обработчик |

### Auto-registration мапперов

```csharp
private static IServiceCollection AddExceptionMappers(this IServiceCollection services)
{
    var interfaceType = typeof(IExceptionMapper);
    var typesToRegister = AssemblyHelper.GetDerivedTypesFromAssemblies<IExceptionMapper>(
        excludedAttributesTypes: [typeof(ManualConfigurationAttribute)])
        .ToArray();

    typesToRegister.ForEach(type => services.AddSingleton(interfaceType, type));
    return services;
}
```

Все классы, реализующие `IExceptionMapper`, регистрируются автоматически. Для исключения маппера из авто-регистрации используйте `[ManualConfigurationAttribute]`.

---

## Расширение: собственный маппер

```csharp
// 1. Создайте маппер (автоматически зарегистрируется)
public sealed class ConflictExceptionMapper(
    IConfiguration configuration)
    : ExceptionMapperBase<ConflictException>(configuration)
{
    protected override string Title => "Конфликт данных";

    protected override int GetResponseStatusCode(ConflictException exception)
        => StatusCodes.Status409Conflict;
}

// 2. Добавьте в DI — готово!
builder.Services.AddExceptionHandling();
```

### Оптимизация производительности: пул StringBuilder

`AppExceptionMapperBase` использует `ExceptionFormattingPool` — пул объектов `StringBuilder` (`DefaultObjectPool<StringBuilder>`) с политикой `StringBuilderPolicy`. Это снижает аллокации в hot path при форматировании исключений с глубоким стеком вызовов.

Если вы создаёте собственный маппер с интенсивной работой со строками, используйте `DefaultObjectPoolExtensions.UsePool<T>()` / `UsePoolAsync<T>()` для аналогичной оптимизации.

---

## См. также

| Документ | Описание |
|----------|----------|
| [CQRS](cqrs.md) | Разделение команд и запросов |
| [Pipeline Behaviors](pipeline-behaviors.md) | Pipeline Behaviours — Logging, Validation |
| [Auto-Registration](auto-registration.md) | AssemblyHelper — авто-регистрация DI |
