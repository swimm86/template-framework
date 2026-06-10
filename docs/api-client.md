# ApiClient — Service-to-Service HTTP-коммуникация

> **Assembly:** `Shared.Application.Core.dll`, `Shared.Infrastructure.Core.dll`
> **Namespace:** `Shared.Application.Core.ApiClient`, `Shared.Infrastructure.Core.ApiClient`
> **Исходники:** `src/Shared/Core/Shared.Application.Core/ApiClient/ApiClient.cs`, `ApiClient.Typed.cs`

---

## 1. Обзор

`ApiClient` — это подсистема для **service-to-service HTTP-взаимодействия** в рамках микросервисной архитектуры. Обеспечивает:

- Типизированные HTTP-методы с автоматической JSON-сериализацией/десериализацией
- Валидацию URI (защита от SSRF и path traversal)
- Валидацию ответов с преобразованием HTTP-ошибок в `ProxiedException`
- Автоматическую простановку `X-Correlation-Id`
- Конвейер delegating handlers с настраиваемым порядком
- Загрузку файлов через `multipart/form-data`
- Гибкую DI-регистрацию с авто-обнаружением handlers

### Ключевые компоненты

| Компонент | Assembly | Назначение |
|-----------|----------|------------|
| `ApiClient` | `Shared.Application.Core` | Базовый класс клиента |
| `ApiClientSettingsBase` | `Shared.Application.Core` | Базовые настройки (BaseUrl, Timeout) |
| `RelativeUriValidator` | `Shared.Application.Core` | Валидация URI (SSRF-защита) |
| `ProxiedResponseValidator` | `Shared.Application.Core` | Валидация HTTP-ответов |
| `CorrelationIdHeaderDelegatingHandler` | `Shared.Infrastructure.Core` | Добавление correlation ID |
| `ProxiedException` | `Shared.Application.Core` | Исключение при ошибке upstream |
| `IApiClientBuilderConfigurator` | `Shared.Application.Core` | Кастомизация `IHttpClientBuilder` |

---

## 2. Quick Start

### Шаг 1: Определить настройки клиента

```csharp
using Shared.Application.Core.ApiClient.Settings.Base;

namespace Template.Bff.Application.HttpClients.Settings;

/// <summary>
/// Настройки API-клиента Getter-а.
/// </summary>
public sealed class GetterApiClientSettings
    : ApiClientSettingsBase;
```

Конфигурация в `appsettings.json`:

Конфигурация в `appsettings.json` (или `.env`). Реальный пример из `.\src\.env`:

```env
Template__GetterApiClientSettings__BaseUrl="http://localhost:5081/api/template/getter/v1/"
Template__SetterApiClientSettings__BaseUrl="http://localhost:5082/api/template/setter/v1/"
```

> `BaseUrl` уже включает базовый маршрут сервиса (`api/template/getter/v1/`), а не только origin. Это согласуется с тем, что `GetterClient` обращается к ресурсам через относительные пути (`persons/cqrs/list`).

### Шаг 2: Создать интерфейс и реализацию

Реальные типы из `.\src\Services\Bff\Template.Bff.Application\`:

```csharp
// Интерфейс: .\src\Services\Bff\Template.Bff.Application\Interfaces\HttpClients\IGetterClient.cs
using Template.Bff.Application.HttpClients.Enums;
using Template.Getter.Application.Abstractions.Features.Person.List.Request;
using Template.Getter.Application.Abstractions.Features.Person.List.Response;

namespace Template.Bff.Application.Interfaces.HttpClients;

public interface IGetterClient
{
    Task<PersonListResponse> GetPersonsAsync(
        PersonListRequest request,
        GetPersonsPattern pattern,
        CancellationToken cancellationToken = default);
}
```

```csharp
// Реализация: .\src\Services\Bff\Template.Bff.Application\HttpClients\GetterClient.cs
using Shared.Application.Core.ApiClient;
using Shared.Application.Core.ApiClient.Attributes;
using Shared.Application.Core.ApiClient.Validators.Interfaces;
using Shared.Domain.Core.Utils.Interfaces;
using Template.Bff.Application.HttpClients.Enums;
using Template.Bff.Application.HttpClients.Settings;
using Template.Bff.Application.Interfaces.HttpClients;
using Template.Getter.Application.Abstractions.Features.Person.List.Request;
using Template.Getter.Application.Abstractions.Features.Person.List.Response;

namespace Template.Bff.Application.HttpClients;

[ApiClientRegistration(typeof(GetterApiClientSettings), typeof(IGetterClient))]
public sealed class GetterClient(
    IHttpClientFactory httpClientFactory,
    IUriValidator uriValidator,
    IResponseValidator responseValidator,
    IPropertyGetter propertyGetter)
    : ApiClient(
        httpClientFactory,
        uriValidator,
        responseValidator,
        propertyGetter), IGetterClient
{
    public Task<PersonListResponse> GetPersonsAsync(
        PersonListRequest request,
        GetPersonsPattern pattern,
        CancellationToken cancellationToken = default)
    {
        var requestPart = pattern switch
        {
            GetPersonsPattern.Cqrs => "cqrs",
            GetPersonsPattern.Services => "services",
            _ => throw new ArgumentException($"Unknown '{nameof(pattern)}' value.", nameof(pattern))
        };

        return PostAsync<PersonListResponse>(
            $"persons/{requestPart}/list",
            request,
            cancellationToken)!;
    }
}
```

### Шаг 3: Использование в handler/query

В проекте `Template` нет типа `Result<T>` — handlers возвращают `PersonListResponse` (или наследник `Response`/`PageableResponse<>`) напрямую. HTTP-статус заполняется фреймворком из `Response.StatusCode`:

```csharp
public class GetPersonsHandler
{
    private readonly IGetterClient _getterClient;

    public GetPersonsHandler(IGetterClient getterClient)
    {
        _getterClient = getterClient;
    }

    public async Task<PersonListResponse> Handle(
        CancellationToken cancellationToken)
    {
        var dto = new PersonListRequest(new DalPattern.Default());
        var response = await _getterClient.GetPersonsAsync(
            dto,
            GetPersonsPattern.Cqrs,
            cancellationToken);

        return response;
    }
}
```

### Шаг 4: Регистрация в DI

`ApiClient` помечается атрибутом `[ApiClientRegistration]`, после чего автоматически обнаруживается и регистрируется в `Shared.Infrastructure.Core.DependencyInjection.DependencyInjector.Process` (`.\src\Shared\Core\Shared.Infrastructure.Core\DependencyInjection\DependencyInjector.cs`). Этот регистратор вызывается через `AddReferencedDependencyInjectors()` из `ImplementDependencies()` — отдельных вызовов в `Program.cs` не требуется.

Под капотом `Shared.Infrastructure.Core.ApiClient.Extensions.DependencyInjectionExtensions` (`.\src\Shared\Core\Shared.Infrastructure.Core\ApiClient\Extensions\DependencyInjectionExtensions.cs`) предоставляет три `internal static` метода:

| Метод | Назначение |
|-------|-----------|
| `AddHttpClients(IConfiguration)` | Поиск всех наследников `ApiClient` с `[ApiClientRegistration]`, для каждого — `AddClient<TOptions, TIClient, TClient>` через reflection |
| `AddDelegatingHandlers()` | Регистрация всех `DelegatingHandler` с `[ApiClientDelegatingHandleMetadata]` |
| `AddPrimaryHttpMessageHandlers()` | Регистрация всех `HttpClientHandler` с атрибутом primary-handler |

Все три метода — `internal static` и не предназначены для прямого вызова из сервисов. Для ручной регистрации единичного клиента существует публичный `AddClient<TOptions, TIClient, TClient>` в `Shared.Application.Core.ApiClient.Extensions.DependencyInjectionExtensions`.

---

## 3. HTTP-методы

### Базовые методы (возвращают `HttpResponseMessage`)

| Метод | Сигнатура | Описание |
|-------|-----------|----------|
| `GetAsync` | `GetAsync(string uri, CancellationToken)` | GET без параметров |
| `GetAsync` | `GetAsync(string uri, Dictionary<string, string> queryParams, CancellationToken)` | GET с query-параметрами |
| `PostAsync` | `PostAsync(string uri, HttpContent content, CancellationToken)` | POST с произвольным контентом |
| `PostAsync` | `PostAsync(string uri, CancellationToken)` | POST без тела |
| `PostAsJsonAsync` | `PostAsJsonAsync(string uri, object? content, CancellationToken)` | POST с JSON-телом |
| `PostFilesAsync` | `PostFilesAsync(string url, IWithFile request, CancellationToken)` | POST multipart/form-data |
| `PutAsync` | `PutAsync(string uri, object? content, CancellationToken)` | PUT с JSON-телом |
| `PatchAsync` | `PatchAsync(string uri, object? content, CancellationToken)` | PATCH с JSON-телом |
| `DeleteAsync` | `DeleteAsync(string uri, CancellationToken)` | DELETE |

### Типизированные методы (десериализуют ответ в `TResult`)

| Метод | Сигнатура | Описание |
|-------|-----------|----------|
| `GetAsync<TResult>` | `GetAsync<TResult>(string uri, Dictionary<string, string> queryParams, CancellationToken)` | GET → `TResult` (с query-параметрами) |
| `PostAsync<TResult>` | `PostAsync<TResult>(string uri, object? content, CancellationToken)` | POST → `TResult` |
| `PostAsync<TResult>` | `PostAsync<TResult>(string uri, CancellationToken)` | POST без тела → `TResult` |
| `PostFileAsync<TContent, TResult>` | `PostFileAsync<TContent, TResult>(string uri, TContent content, CancellationToken)` | POST файл → `TResult` |
| `PutAsync<TResult>` | `PutAsync<TResult>(string uri, object? content, CancellationToken)` | PUT → `TResult` |
| `PatchAsync<TResult>` | `PatchAsync<TResult>(string uri, object? content, CancellationToken)` | PATCH → `TResult` |
| `DeleteAsync<TResult>` | `DeleteAsync<TResult>(string uri, CancellationToken)` | DELETE → `TResult` |

### Автоматическая простановка StatusCode

Если `TResult` наследуется от `ResponseBase`, свойство `StatusCode` заполняется автоматически:

```csharp
// В ApiClient.Typed.cs: ResponseAsJsonAsync<TResult>
if (result is ResponseBase response)
{
    response.StatusCode = (int)httpResponse.StatusCode;
}
```

---

## 4. Validators (Безопасность)

### 4.1. RelativeUriValidator — SSRF-защита

**Assembly:** `Shared.Application.Core.dll`
**Namespace:** `Shared.Application.Core.ApiClient.Validators`

Защищает от Server-Side Request Forgery и path traversal атак:

```csharp
public sealed class RelativeUriValidator : IUriValidator
{
    public void Validate(string uri)
    {
        // 1. Запрет абсолютных URI (защита от SSRF)
        if (Uri.IsWellFormedUriString(uri, UriKind.Absolute))
            throw new SecurityException("Абсолютные URI запрещены...");

        // 2. Запрет path traversal с циклическим декодированием
        //    %2e%2e%2f → ../ → обнаруживается
        var decoded = uri;
        string prev;
        do { prev = decoded; decoded = Uri.UnescapeDataString(decoded); }
        while (decoded != prev);
        if (decoded.Contains(".."))
            throw new SecurityException("Path traversal запрещён...");

        // 3. Запрет URI, начинающихся с / или \
        if (uri.StartsWith('/') || uri.StartsWith('\\'))
            throw new SecurityException("URI должен быть относительным путём...");

        // 4. Проверка формата относительного URI
        if (!Uri.IsWellFormedUriString(uri, UriKind.Relative))
            throw new FormatException("Невалидный относительный URI...");
    }
}
```

**Что блокирует:**

| URI | Результат | Причина |
|-----|-----------|---------|
| `https://evil.com/steal` | `SecurityException` | Абсолютный URI (SSRF) |
| `../../../etc/passwd` | `SecurityException` | Path traversal |
| `%2e%2e%2f%2e%2e%2fetc/passwd` | `SecurityException` | Encoded path traversal |
| `/api/persons` | `SecurityException` | Начинается с `/` |
| `api/persons` | ✅ | Валидный относительный URI |
| `api/persons/list?page=1` | ✅ | Валидный относительный URI |

### 4.2. ProxiedResponseValidator — валидация ответов

**Assembly:** `Shared.Application.Core.dll`
**Namespace:** `Shared.Application.Core.ApiClient.Validators`

Преобразует HTTP-ошибки в `ProxiedException`:

```csharp
public sealed class ProxiedResponseValidator(ILogger<ProxiedResponseValidator> logger)
    : IResponseValidator
{
    public async Task ValidateAsync(
        HttpResponseMessage httpResponse,
        string clientName,
        string? logUri = null,
        object? logContent = null,
        CancellationToken cancellationToken = default)
    {
        if (httpResponse.IsSuccessStatusCode) return;

        // 1. Читает тело ответа как ProblemDetails
        var problemDetails = JsonHelper.TryDeserialize<ProblemDetails>(response, out var pd)
            ? pd
            : new ProblemDetails { Status = (int)httpResponse.StatusCode, ... };

        // 2. Извлекает additionalData из ProblemDetails.Extensions
        var additionalData = TakeAdditionalData(problemDetails);

        // 3. Если в errors есть 500 — модифицирует Title/Detail
        SetProblemDetailsForServerError(problemDetails, absolutePath, clientName);

        // 4. Бросает ProxiedException
        throw new ProxiedException(problemDetails, (int)httpResponse.StatusCode, additionalData);
    }
}
```

---

## 5. Delegating Handlers Pipeline

### 5.1. CorrelationIdHeaderDelegatingHandler

**Assembly:** `Shared.Infrastructure.Core.dll`
**Namespace:** `Shared.Infrastructure.Core.ApiClient.Handlers`

Автоматически добавляет `X-Correlation-Id` в исходящие запросы:

```csharp
[ApiClientDelegatingHandleMetadata(order: 100)]
public sealed class CorrelationIdHeaderDelegatingHandler(
    IHttpContextAccessor httpContextAccessor,
    ILogger<CorrelationIdHeaderDelegatingHandler> logger)
    : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Headers.Contains(Constants.CorrelationIdHeader))
            return base.SendAsync(request, cancellationToken);

        var correlationId =
            httpContextAccessor.HttpContext?.Request.GetCorrelationId() ??
            JobCorrelationContext.GetCorrelationId();

        if (correlationId.HasValue)
            request.Headers.Add(Constants.CorrelationIdHeader, correlationId.Value.ToString("D"));

        return base.SendAsync(request, cancellationToken);
    }
}
```

**Источники CorrelationId:**
1. HTTP-заголовок текущего запроса (`X-Correlation-Id`)
2. `JobCorrelationContext` (для фоновых задач)

### 5.2. ApiClientDelegatingHandleMetadataAttribute

Управляет порядком и областью применения handlers:

```csharp
[AttributeUsage(AttributeTargets.Class)]
public sealed class ApiClientDelegatingHandleMetadataAttribute(
    int order,
    params Type[] clientTypes)
    : ApiClientHandlerMetadataAttributeBase(clientTypes)
{
    public int Order { get; } = order;
}
```

**Параметры:**

| Параметр | Тип | Описание |
|----------|-----|----------|
| `order` | `int` | Приоритет в конвейере (меньше = раньше) |
| `clientTypes` | `Type[]` | К каким клиентам применять (пусто = ко всем) |

**Зарезервированные Order-значения:**

| Order | Handler | Назначение |
|-------|---------|------------|
| `100` | `CorrelationIdHeaderDelegatingHandler` | Установка correlation ID |

### 5.3. Создание собственного handler

```csharp
[ApiClientDelegatingHandleMetadata(order: 200, typeof(GetterClient))]
public sealed class RetryDelegatingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Логика retry
        return await base.SendAsync(request, cancellationToken);
    }
}
```

Handler автоматически зарегистрируется при вызове `AddDelegatingHandlers()`.

---

## 6. Configuration

### 6.1. ApiClientSettingsBase

```csharp
public abstract class ApiClientSettingsBase
{
    /// <summary>Базовый адрес сервиса.</summary>
    public virtual string BaseUrl { get; set; } = null!;

    /// <summary>Таймаут HTTP-запроса (по умолчанию 100 секунд).</summary>
    public virtual TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(100);

    /// <summary>Валидирует настройки (BaseUrl обязателен).</summary>
    public void Validate();
}
```

### 6.2. ApiClientRegistrationAttribute

Маркер для автоматической DI-регистрации:

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ApiClientRegistrationAttribute(
    Type settingsType,
    Type interfaceType) : Attribute
{
    public Type SettingsType { get; } = settingsType;
    public Type ApiClientInterfaceType { get; } = interfaceType;
}
```

### 6.3. IApiClientBuilderConfigurator

Кастомизация `IHttpClientBuilder` для отдельных клиентов или глобально:

```csharp
public interface IApiClientBuilderConfigurator
{
    /// <summary>Целевые типы (пусто = общий конфигуратор).</summary>
    IReadOnlyCollection<Type> ApiClientTypes { get; }

    /// <summary>Исключённые типы (только для общих конфигураторов).</summary>
    IReadOnlyCollection<Type> ExcludedApiClientTypes { get; }

    void Configure(IHttpClientBuilder builder);
}
```

**Два вида конфигураторов:**

| Вид | `ApiClientTypes` | Поведение |
|-----|------------------|-----------|
| **Specialized** | Не пустой | Применяется только к указанным клиентам |
| **Common** | Пустой | Применяется ко всем, кроме `ExcludedApiClientTypes` |

**Приоритет:** Specialized > Common (если найден специализированный, общий игнорируется)

**Пример специализированного конфигуратора:**

```csharp
public class GetterClientConfigurator : IApiClientBuilderConfigurator
{
    public IReadOnlyCollection<Type> ApiClientTypes => [typeof(GetterClient)];
    public IReadOnlyCollection<Type> ExcludedApiClientTypes => [];

    public void Configure(IHttpClientBuilder builder)
    {
        builder.AddPolicyHandler(GetRetryPolicy());
    }
}
```

**Пример общего конфигуратора с исключениями:**

```csharp
public class GlobalHttpClientConfigurator : IApiClientBuilderConfigurator
{
    public IReadOnlyCollection<Type> ApiClientTypes => []; // Общий
    public IReadOnlyCollection<Type> ExcludedApiClientTypes => [typeof(ExternalApiClient)];

    public void Configure(IHttpClientBuilder builder)
    {
        builder.ConfigurePrimaryHttpMessageHandler(() =>
            new HttpClientHandler { ServerCertificateCustomValidationCallback = ... });
    }
}
```

### 6.4. ApiClientBuilderConfiguratorContext

Статический контекст, который строит карту `ApiClient → IApiClientBuilderConfigurator`:

- Инициализируется один раз при старте (`InitializeApiClientBuilderConfiguratorsMap()`)
- Thread-safe (double-checked locking)
- Выбрасывает `InvalidOperationException` при дубликатах конфигураторов

---

## 7. File Uploads (IWithFile)

### 7.1. IWithFile интерфейс

```csharp
public interface IWithFile
{
    IFormFile? File { get; }
}
```

### 7.2. DTO для загрузки файла

```csharp
public record PersonPhotoUploadRequest : IWithFile
{
    public IFormFile? File { get; init; }
    public Guid PersonId { get; init; }
    public string Description { get; init; } = string.Empty;
}
```

### 7.3. Отправка файла через ApiClient

```csharp
public async Task<PersonPhotoResponse> UploadPhotoAsync(
    PersonPhotoUploadRequest request,
    CancellationToken cancellationToken)
{
    return await PostFileAsync<PersonPhotoUploadRequest, PersonPhotoResponse>(
        "persons/photo/upload",
        request,
        cancellationToken);
}
```

**Что происходит внутри `PostFilesAsync`:**

1. Создаётся `MultipartFormDataContent` с уникальным boundary
2. Файл добавляется как `StreamContent` с правильным `ContentType`
3. Все остальные свойства DTO (кроме `File`) добавляются как `StringContent`
4. Отправляется POST-запрос

---

## 8. Error Handling (ProxiedException)

### 8.1. ProxiedException

**Assembly:** `Shared.Application.Core.dll`
**Namespace:** `Shared.Application.Core.Exceptions.Models`

```csharp
public class ProxiedException : AppException
{
    /// <summary>Детали ошибки в формате RFC 7807 (ProblemDetails).</summary>
    public ProblemDetails ProblemDetails { get; }

    /// <summary>HTTP-статус код ошибки.</summary>
    public int StatusCode { get; }

    /// <summary>Типизированное получение additionalData.</summary>
    public bool TryGetAdditionalData<T>(string key, out T? value);
}
```

### 8.2. Обработка в handler/query

`ProxiedException` — это подтип `AppException`. В проекте `Template` нет типа `Result<T>` — ошибки пробрасываются как исключения и обрабатываются единым `ExceptionHandler` из `Shared.Presentation.Core`. Если требуется извлечь `AdditionalData` для побочной логики (например, логирования), `ProxiedException` ловится в handler:

```csharp
public async Task<PersonResponse> Handle(
    GetPersonQuery request,
    CancellationToken cancellationToken)
{
    try
    {
        return await _getterClient.GetPersonAsync(request.Id, cancellationToken);
    }
    catch (ProxiedException ex)
    {
        // Доступ к деталям ошибки от upstream-сервиса
        _logger.LogError(
            "Getter service error: {Status} - {Detail}",
            ex.StatusCode, ex.ProblemDetails.Detail);

        // Извлечение дополнительных данных
        if (ex.TryGetAdditionalData<ICollection<int>>("notFoundCodes", out var codes))
        {
            // Обработка специфичных данных
        }

        throw;
    }
}
```

### 8.3. AdditionalData Flow

**Как дополнительные данные передаются между сервисами:**

```
[Upstream Service]                          [Downstream Service]
─────────────────                           ──────────────────
                                           
Controller бросает                         ProxiedResponseValidator
AppException с AdditionalData:             читает тело ответа:
                                           
  throw new BusinessLogicException(          var problemDetails =
    "Person not found",                      ReadFromJsonAsync<ProblemDetails>();
    new Dictionary<string, object>
    {                                        var additionalData =
      ["personId"] = 42,                     TakeAdditionalData(problemDetails);
      ["tenantId"] = "abc"
    });                                      throw new ProxiedException(
                                               problemDetails, statusCode, additionalData);
                                            )
                                           
↓ HTTP 422 Response                         
                                           
  {                                         
    "title": "Business Logic Error",        
    "status": 422,                          
    "detail": "Person not found",           
    "additionalData": {    ← извлекается    
      "personId": 42,      и удаляется      
      "tenantId": "abc"    из Extensions    
    }                                       
  }                                         
                                           
                                            ↓
                                            Downstream handler ловит:
                                            
                                            catch (ProxiedException ex)
                                            {
                                              ex.TryGetAdditionalData<int>(
                                                "personId", out var id);
                                              // id == 42
                                            }
```

**Ключевой момент:** `ProxiedResponseValidator.TakeAdditionalData()` **удаляет** `additionalData` из `ProblemDetails.Extensions` перед выбросом исключения. Это гарантирует, что дополнительные данные доступны только бэкенд-потребителю через `TryGetAdditionalData<T>()`, но **не передаются фронтенду** в финальном ответе.

---

## 9. DI Registration Details

### 9.1. AddClient<TOptions, TIClient, TClient>

```csharp
public static IServiceCollection AddClient<TOptions, TIClient, TClient>(
    this IServiceCollection serviceCollection,
    IConfiguration configuration)
    where TOptions : ApiClientSettingsBase
    where TIClient : class
    where TClient : ApiClient, TIClient
```

**Что делает:**
1. Читает настройки из `IConfiguration` через `configuration.GetOptions<TOptions>()`
2. Валидирует `BaseUrl`
3. Регистрирует `HttpClient` с `BaseAddress` и `Timeout`
4. Авто-обнаруживает и регистрирует delegating handlers (по атрибутам)
5. Применяет `IApiClientBuilderConfigurator` если найден
6. Регистрирует `TIClient → TClient` как `Transient`

### 9.2. AddClient<TIClient, TClient>

```csharp
public static IServiceCollection AddClient<TIClient, TClient>(
    this IServiceCollection serviceCollection,
    ApiClientSettingsBase options)
```

Регистрация через объект настроек (без `IConfiguration`).

### 9.3. Автоматическая регистрация (AddHttpClients)

```csharp
internal static IServiceCollection AddHttpClients(
    this IServiceCollection serviceCollection,
    IConfiguration configuration)
```

**Алгоритм:**
1. Находит все классы-наследники `ApiClient`, помеченные `[ApiClientRegistration]`
2. Для каждого типа читает `SettingsType` и `ApiClientInterfaceType` из атрибута
3. Вызывает `AddClient<TOptions, TIClient, TClient>` через reflection

### 9.4. Handler Lifetime

```csharp
.SetHandlerLifetime(TimeSpan.FromMinutes(2))
```

Delegating handlers пересоздаются каждые 2 минуты (независимо от lifetime `HttpClient`).

---

## 10. Интеграция с CQRS/Services

### 10.1. Command/Query Handler с ApiClient

В проекте `Template` нет типа `Result`. Handlers возвращают `Response`/`PageableResponse<>` напрямую; ошибки пробрасываются как `ProxiedException` (см. 8.2):

```csharp
public class SyncPersonsHandler : IRequestHandler<SyncPersonsCommand, Response>
{
    private readonly IGetterClient _getterClient;
    private readonly ISetterClient _setterClient;

    public SyncPersonsHandler(IGetterClient getterClient, ISetterClient setterClient)
    {
        _getterClient = getterClient;
        _setterClient = setterClient;
    }

    public async Task<Response> Handle(
        SyncPersonsCommand command,
        CancellationToken cancellationToken)
    {
        var persons = await _getterClient.GetPersonsAsync(
            new PersonListRequest(new DalPattern.Default()),
            GetPersonsPattern.Cqrs,
            cancellationToken);

        foreach (var person in persons.Payload)
        {
            await _setterClient.CreatePersonAsync(
                new PersonCreateRequest { Name = person.Name, Email = person.Email },
                cancellationToken);
        }

        return new Response { StatusCode = StatusCodes.Status200OK };
    }
}
```

### 10.2. CorrelationId сквозная трассировка

```
Client → [X-Correlation-Id: abc-123] → BFF API
                                              ↓
                                    CorrelationIdHeaderDelegatingHandler
                                    (читает из HttpContext, добавляет в запрос)
                                              ↓
                                    [X-Correlation-Id: abc-123] → Getter API
```

CorrelationId сохраняется при вызове `GetPersonsAsync` — оба сервиса логируют один и тот же ID.

---

---

---

## См. также

| Документ | Описание |
|----------|----------|
| [Correlation ID](correlation-id.md) | Сквозная трассировка запросов |
| [Exception Mapping](exception-mapping.md) | Маппинг исключений в HTTP-статусы |
| [Response Types](response-types.md) | Типы ответов (ResponseBase, ErrorResponse) |
| [Controllers](controllers.md) | API Controllers и Request/Response DTOs |
| [Configuration](configuration.md) | Управление конфигурацией и .env |
