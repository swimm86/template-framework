# Swagger / OpenAPI Integration

**Сборка:** `Shared.Presentation.Core.dll`  
**Namespace:** `Shared.Presentation.Core.Swagger`  
**Исходники:** `src/Shared/Core/Shared.Presentation.Core/Swagger/`
**Пакет:** `Swashbuckle.AspNetCore`

---

## 🚀 Quick Start

В `Template` Swagger регистрируется и подключается автоматически — `Program.cs` сводится к двум вызовам. Реальные `Program.cs` сервисов Bff, Getter, Setter выглядят так (`src/Services/Getter/Template.Getter.Api/Program.cs`):

```csharp
using Shared.Presentation.Core.Extensions;
using Template.Presentation.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.ImplementDependencies();

var app = builder.Build();
app.UseCommonPresentation();

app.Run();
```

Что происходит «под капотом»:

1. `ImplementDependencies()` (`Shared.Presentation.Core.Extensions.WebApplicationBuilderExtensions`):
   - Инициализирует `.env`-конфигурацию
   - Регистрирует контроллеры с конвенциями маршрутизации и `RequestLoggingFilter`
   - Вызывает `AddReferencedDependencyInjectors()`, который находит ВСЕ наследники `DependencyInjectorBase` из ссылочных сборок и последовательно выполняет их `Inject`. Среди них:
     - `Shared.Application.Cqrs.Core.DependencyInjection` → `AddMediatR()`
     - `Shared.Application.Core.DependencyInjection` → регистрация валидаторов, репозиториев, HTTP-аксессора и др.
     - `Shared.Infrastructure.Core.DependencyInjection` → `AddDelegatingHandlers()`, `AddPrimaryHttpMessageHandlers()`, `AddHttpClients(configuration)` (включая `ApiClientBuilderConfiguratorContext.InitializeApiClientBuilderConfiguratorsMap()`)
     - `Shared.Presentation.Core.DependencyInjection` → `AddEndpointsApiExplorer()`, `AddSwagger()`, `AddFluentValidation()`, `AddExceptionHandling()`
     - `Template.Presentation.DependencyInjection` (из `Template.Presentation`) → `ConfigureSwaggerAuth()` + CORS

2. `UseCommonPresentation()` (`Template.Presentation.Extensions.ApplicationBuilderExtensions`):
   - Вызывает `UsePresentationCore()` (`Shared.Presentation.Core.Extensions.ApplicationBuilderExtensions`):
     - `UseCorrelationId()` (внутренний extension из `Shared.Presentation.Core.CorrelationId.Extensions`)
     - В Development — `UseSwaggerConfigured()` (внутренний extension из `Shared.Presentation.Core.Swagger.Extensions`)
     - `UseExceptionHandler()` (из `Shared.Presentation.Core.Exceptions`)
     - `UseAuthorization()`, `MapControllers()`
   - Добавляет `UseCors(Constants.CorsDefaultPolicyName)`

Swagger UI доступен по адресу: `https://localhost:<port>/swagger` (только в Development).

---

## 📐 Архитектура

Модуль Swagger предоставляет автоматическую настройку OpenAPI-документации для ASP.NET Core API:

| Компонент | Назначение |
|-----------|-----------|
| `AddSwagger()` | Регистрация генератора Swagger + XML-документация + Schema Filters |
| `UseSwaggerConfigured()` | Middleware Swagger + Swagger UI с настройками |
| `RequiredByClrNullabilitySchemaFilter` | Синхронизация `required` с CLR nullability |
| `EnumTypesSchemaFilter` | Обогащение enum-схем значениями и XML-описаниями |

---

## 🔧 Schema Filters

### RequiredByClrNullabilitySchemaFilter

**Файл:** `Swagger/SchemaFilters/RequiredByClrNullabilitySchemaFilter.cs`

Синхронизирует OpenAPI-массив `required` с CLR nullability (NRT-метаданные). Гарантирует, что в Swagger-спецификации обязательные поля соответствуют реальной nullability типов.

#### Что учитывает

| Сценарий | Поведение |
|----------|-----------|
| Non-nullable value types (`int`, `Guid`, `bool`) | **required** |
| Nullable value types (`int?`, `Guid?`) | **optional** |
| Non-nullable reference types (`string`) | **required** (по NRT-метаданным) |
| Nullable reference types (`string?`) | **optional** |
| Свойства с `[JsonPropertyName("custom_name")]` | Корректное маппинг JSON-имени |
| camelCase-имена | Автоматическое преобразование |

#### Implementation

```csharp
public sealed class RequiredByClrNullabilitySchemaFilter : ISchemaFilter
{
    private static readonly NullabilityInfoContext NullabilityContext = new();

    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        // 1. Находим все public properties типа
        var typeProperties = context.Type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead)
            .ToArray();

        // 2. Для каждого schema property определяем required по nullability
        foreach (var schemaPropertyName in schema.Properties.Keys)
        {
            var typeProperty = FindTypeProperty(typeProperties, schemaPropertyName);
            if (typeProperty != null && IsRequired(typeProperty))
            {
                requiredByNullability.Add(schemaPropertyName);
            }
        }

        // 3. Синхронизируем: удаляем "висячие", добавляем missing
        SynchronizeRequired(schema.Required, schemaPropertyNames, requiredByNullability);
    }
}
```

#### Алгоритм IsRequired

```csharp
private static bool IsRequired(PropertyInfo property)
{
    var type = property.PropertyType;

    // Value type required, кроме Nullable<T>
    if (type.IsValueType)
    {
        return Nullable.GetUnderlyingType(type) == null;
    }

    // Reference types — NRT-метаданные компилятора
    var nullabilityInfo = NullabilityContext.Create(property);
    return nullabilityInfo.ReadState == NullabilityState.NotNull;
}
```

#### Маппинг JSON-имён

`FindTypeProperty` поддерживает три стратегии поиска:

1. **Прямое совпадение** — `property.Name == schemaPropertyName`
2. **JsonPropertyNameAttribute** — `[JsonPropertyName("customName")]`
3. **camelCase** — `JsonNamingPolicy.CamelCase.ConvertName(property.Name)`

#### Пример

```csharp
public record CreatePersonRequest(
    string Name,          // required (non-nullable reference type)
    string Email,         // required (non-nullable reference type)
    int Age,              // required (non-nullable value type)
    Guid? ManagerId,      // optional (nullable value type)
    string? Notes);       // optional (nullable reference type)
```

**OpenAPI Schema (автоматически):**

```json
{
  "required": ["name", "email", "age"],
  "properties": {
    "name": { "type": "string" },
    "email": { "type": "string" },
    "age": { "type": "integer", "format": "int32" },
    "managerId": { "type": "string", "format": "uuid", "nullable": true },
    "notes": { "type": "string", "nullable": true }
  }
}
```

---

### EnumTypesSchemaFilter

**Файл:** `Swagger/SchemaFilters/EnumTypesSchemaFilter.cs`

Обогащает описание enum-схем списком значений и их XML-описаниями из XML-документации сборок.

#### Constructor

```csharp
public class EnumTypesSchemaFilter(params string[] xmlPaths)
```

| Параметр | Описание |
|----------|----------|
| `xmlPaths` | Абсолютные пути к XML-файлам документации с XML-комментариями |

#### Поведение

Фильтр **не меняет** контракт схемы (`type`/`enum`/`required`), а только обогащает поле `description` человекочитаемым списком.

#### Формат описания

```html
<p>Members:</p>
<ul>
  <li><i>0</i> - Draft</li>
  <li><i>1</i> - Published (Опубликован)</li>
  <li><i>2</i> - Archived (В архиве)</li>
</ul>
```

#### Implementation

```csharp
public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
{
    if (schema.Enum is not { Count: > 0 } || context.Type is not { IsEnum: true })
        return;

    schema.Description += "<p>Members:</p><ul>";
    schema.Enum
        .Where(x => x.GetValueKind() == JsonValueKind.Number)
        .Select(x => x.GetValue<int>())
        .ForEach(x =>
        {
            var valueName = Enum.GetName(context.Type, x);
            var fullTypeName = $"F:{context.Type.FullName}.{valueName}";
            var description = _xmlComments
                .Descendants("member")
                .FirstOrDefault(m =>
                    m.Attribute("name")?.Value.Equals(fullTypeName, StringComparison.OrdinalIgnoreCase) ?? false)?
                .Descendants("summary")
                .FirstOrDefault()?.Value
                .Trim();

            schema.Description +=
                $"<li><i>{x}</i> - {valueName}" +
                $"{(string.IsNullOrWhiteSpace(description) ? string.Empty : $" ({description})")}</li>";
        });
    schema.Description += "</ul>";
}
```

#### Пример

```csharp
/// <summary>
/// Статус заказа.
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Черновик.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Подтверждён.
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// Отменён.
    /// </summary>
    Cancelled = 2,
}
```

**Swagger UI description (автоматически):**

```
Статус заказа.

Members:
  • 0 - Draft (Черновик.)
  • 1 - Confirmed (Подтверждён.)
  • 2 - Cancelled (Отменён.)
```

---

## 📦 Setup

### AddSwagger()

**Файл:** `Swagger/Extensions/ServiceCollectionExtensions.cs`

```csharp
public static IServiceCollection AddSwagger(this IServiceCollection services)
{
    return services
        .AddSwaggerGen(ConfigureSwaggerGenOptions)
        .Configure<ForwardedHeadersOptions>(options =>
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
}
```

> `AddSwagger()` регистрирует не только Swagger-генератор, но и `ForwardedHeadersOptions` для проксирования заголовков `X-Forwarded-For` / `X-Forwarded-Proto`. Эта связка живёт в одном методе намеренно — отдельного `AddForwardedHeaders()`-расширения в проекте нет.

#### Что делает

| Шаг | Действие |
|-----|----------|
| 1 | `AddSwaggerGen()` — регистрация генератора Swagger |
| 2 | Авто-поиск XML-документации всех загруженных assemblies |
| 3 | `SupportNonNullableReferenceTypes()` — поддержка NRT |
| 4 | `SchemaFilter<RequiredByClrNullabilitySchemaFilter>()` — nullability sync |
| 5 | `SchemaFilter<EnumTypesSchemaFilter>(xmlPaths)` — enum descriptions |
| 6 | `ForwardedHeadersOptions` — X-Forwarded-For / X-Forwarded-Proto |

#### Авто-поиск XML-документации

```csharp
var documentationsPaths = AppDomain.CurrentDomain.GetAssemblies()
    .Select(x => Path.Combine(AppContext.BaseDirectory, $"{x.GetName().Name}.xml"))
    .Where(Path.Exists)
    .ToArray();

documentationsPaths.ForEach(xmlFile => options.IncludeXmlComments(xmlFile, true));
```

Параметр `true` в `IncludeXmlComments` включает XML-комментарии для членов типов (properties, methods, enum values).

---

### UseSwaggerConfigured()

**Файл:** `Swagger/Extensions/ApplicationBuilderExtensions.cs`

Класс расширений объявлен как `internal static class`, поэтому `UseSwaggerConfigured` нельзя вызвать напрямую из сервиса. Метод выполняется автоматически из `UsePresentationCore()` в Development-окружении:

```csharp
internal static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseSwaggerConfigured(
        this IApplicationBuilder app,
        Action<SwaggerUIOptions>? setupUiAction = null)
    {
        app.UseSwagger();
        app.UseSwaggerUI(opts =>
        {
            opts.DocExpansion(DocExpansion.None);
            opts.ConfigObject.AdditionalItems.Add("tagsSorter", "alpha");
            setupUiAction?.Invoke(opts);
        });

        return app;
    }
}
```

#### Настройки UI по умолчанию

| Настройка | Значение | Описание |
|-----------|----------|----------|
| `DocExpansion` | `None` | Все endpoint'ы свёрнуты по умолчанию |
| `tagsSorter` | `alpha` | Контроллеры отсортированы по алфавиту |

Кастомизация `SwaggerUIOptions` возможна только при прямом вызове метода, что в текущей архитектуре не используется (нет публичной точки входа).

---

## 🔗 Интеграция с CQRS/Services

### Реальный Program.cs

Все регистрации (`AddMediatR`, `AddFluentValidation`, `AddSwagger`, `AddExceptionHandling`) выполняются автоматически через `DependencyInjectorBase`-классы из Shared-сборок. `Program.cs` сводится к двум вызовам. Реальный пример из `src/Services/Bff/Template.Bff.Api/Program.cs`:

```csharp
using Shared.Presentation.Core.Extensions;
using Template.Presentation.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.ImplementDependencies();

var app = builder.Build();
app.UseCommonPresentation();

app.Run();
```

`UseCommonPresentation()` (`src/Services/Common/Template.Presentation/Extensions/ApplicationBuilderExtensions.cs`) оборачивает `UsePresentationCore()` и добавляет `UseCors(...)`.

### Настройка авторизации в Swagger

В сервисах, где нужен Bearer JWT в Swagger UI, отдельный `ConfigureSwaggerAuth()` подключается через `Template.Presentation.DependencyInjection.DependencyInjector` (`src/Services/Common/Template.Presentation/DependencyInjection/DependencyInjector.cs`):

```csharp
public class DependencyInjector : DependencyInjectorBase
{
    protected override IServiceCollection Process(IServiceCollection services)
    {
        var allowedOrigins = configuration.GetValue<string>("AllowedOrigins");
        return services
            .ConfigureSwaggerAuth()                       // Bearer JWT
            .AddCors(options => { /* ... */ });
    }
}
```

`ConfigureSwaggerAuth` (`src/Services/Common/Template.Presentation/Swagger/Extensions/DependencyInjectionExtensions.cs`) добавляет `SecurityDefinition` "Bearer" с типом `Http` и схемой `Bearer`, а также требование безопасности, ссылающееся на это определение.

### Request DTOs в Swagger

Реальный `PersonsController` из `src/Services/Getter/Template.Getter.Api/Controllers/PersonsController.cs`:

```csharp
public sealed class PersonsController(
    IPersonsService personsService,
    ISender sender,
    ILogger<PersonsController> logger)
    : GetterControllerBase(logger)
{
    /// <summary>
    /// Возвращает коллекцию сущностей "Персона" через слой приложения (без CQRS).
    /// </summary>
    /// <param name="request">Тело запроса.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Коллекция сущностей "Персона".</returns>
    [HttpPost("services/list")]
    public Task<IActionResult> GetPersonsByServicesAsync(
        [FromBody] PersonListRequest request,
        CancellationToken cancellationToken = default) =>
        Process(() => personsService.GetPersonsAsync(request, cancellationToken));

    /// <summary>
    /// Возвращает коллекцию сущностей "Персона" через CQRS (MediatR).
    /// </summary>
    /// <param name="request">Тело запроса.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Коллекция сущностей "Персона".</returns>
    [HttpPost("cqrs/list")]
    public Task<IActionResult> GetPersonsByCqrsAsync(
        [FromBody] PersonListRequest request,
        CancellationToken cancellationToken = default)
    {
        return Process(
            () => sender.Send(new PersonListQuery(request), cancellationToken));
    }
}
```

**Swagger автоматически покажет:**

- `PersonListRequest` с корректными `required` полями (благодаря `RequiredByClrNullabilitySchemaFilter`)
- XML-комментарии к endpoint'у (description, parameters, response) — для этого `GenerateDocumentationFile=true` должен быть включён в `.csproj`
- 4xx/5xx схемы ответов через единый `ExceptionHandler` (если в проекте есть `ErrorResponse`)

---

## 📝 Best Practices

1. **XML-документация обязательна** — включите `<GenerateDocumentationFile>true</GenerateDocumentationFile>` в `.csproj`
2. **`<Nullable>enable</Nullable>`** — RequiredByClrNullabilitySchemaFilter работает только с включёнными NRT
3. **Только Development** — `UseSwaggerConfigured()` только в `IsDevelopment()`
4. **Enum XML-комментарии** — добавляйте `<summary>` к каждому enum value для автоматического отображения в Swagger UI
5. **ProducesResponseType** — явно указывайте response types для корректной OpenAPI-спецификации

---

---

## См. также

| Документ | Описание |
|----------|----------|
| [Controllers](controllers.md) | API Controllers — Request/Response DTOs |
| [Response Types](response-types.md) | Response types — CreateResponse, UpdateResponse, PageableResponse |
