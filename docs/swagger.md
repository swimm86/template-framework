# Swagger / OpenAPI Integration

**Сборка:** `Shared.Presentation.Core.dll`  
**Namespace:** `Shared.Presentation.Core.Swagger`  
**Исходники:** `src/Shared/Core/Shared.Presentation.Core/Swagger/`

---

## 🚀 Quick Start

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Регистрация Swagger-сервисов
builder.Services.AddSwagger();

var app = builder.Build();

// Активация Swagger UI (только Development)
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerConfigured();
}

app.Run();
```

Swagger UI доступен по адресу: `https://localhost:<port>/swagger`

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
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                | ForwardedHeaders.XForwardedProto);
}
```

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

```csharp
public static IApplicationBuilder UseSwaggerConfigured(
    this IApplicationBuilder app,
    Action<SwaggerUIOptions>? setupUiAction = null)
{
    app.UseSwagger();
    app.UseSwaggerUI(opts =>
    {
        opts.DocExpansion(DocExpansion.None);          // Все endpoint'ы свёрнуты
        opts.ConfigObject.AdditionalItems.Add("tagsSorter", "alpha");  // Сортировка по алфавиту
        setupUiAction?.Invoke(opts);                   // Кастомная настройка
    });
    return app;
}
```

#### Настройки UI по умолчанию

| Настройка | Значение | Описание |
|-----------|----------|----------|
| `DocExpansion` | `None` | Все endpoint'ы свёрнуты по умолчанию |
| `tagsSorter` | `alpha` | Контроллеры отсортированы по алфавиту |

#### Кастомизация UI

```csharp
app.UseSwaggerConfigured(opts =>
{
    opts.RoutePrefix = "api-docs";           // Другой URL
    opts.DocumentTitle = "My API Docs";      // Заголовок страницы
    opts.SwaggerEndpoint("/swagger/v1/swagger.json", "My API v1");
});
```

---

## 🔗 Интеграция с CQRS/Services

### Типичный Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// CQRS + MediatR
builder.Services.AddMediatR();

// FluentValidation
builder.Services.AddFluentValidation();

// Exception Handling
builder.Services.AddExceptionHandling();

// Swagger
builder.Services.AddSwagger();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerConfigured();
}

app.UseExceptionHandler();
app.MapControllers();
app.Run();
```

### Request DTOs в Swagger

```csharp
[ApiController]
[Route("api/[controller]")]
public class PersonsController(IPersonsService personsService) : ControllerBase
{
    /// <summary>
    /// Создаёт новую персону.
    /// </summary>
    /// <param name="request">Данные для создания.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Созданная персона.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(PersonCreateResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PersonCreateResponse>> Create(
        [FromBody] PersonCreateRequest request,
        CancellationToken ct)
    {
        var response = await personsService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }
}
```

**Swagger автоматически покажет:**

- `PersonCreateRequest` с корректными `required` полями (благодаря `RequiredByClrNullabilitySchemaFilter`)
- XML-комментарии к endpoint'у (description, parameters, response)
- `ErrorResponse` schema для 400 Bad Request

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
