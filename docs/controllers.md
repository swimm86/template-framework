# Controllers

**Assembly:** `Shared.Presentation.Core.dll`  
**Namespace:** `Shared.Presentation.Core.Controllers`, `Shared.Presentation.Core.Extensions`  
**Исходники:** `src/Shared/Core/Shared.Presentation.Core/`

---

## Обзор

Presentation Layer в фреймворке Shared предоставляет инфраструктуру для ASP.NET Core контроллеров с:

- **Единым маршрутизацией** — конвенции для автоматического формирования URL
- **Process<TResponse>** — обёртка для обработки CQRS-запросов с логированием и HTTP status mapping
- **ConfigurePresentationCore()** — one-call настройка всего presentation middleware
- **FluentValidation интеграция** — авто-обнаружение валидаторов и выброс ValidationException
- **Correlation ID** — middleware для трекинга запросов across сервисов

---

## ControllerBase

Базовый класс для всех контроллеров с единой точкой обработки запросов.

```csharp
[ApiController]
[Route("api/[appName]/[controllerType]/v1/[controller]")]
public abstract class ControllerBase(ILogger logger)
    : Microsoft.AspNetCore.Mvc.ControllerBase
{
    protected Task<IActionResult> Process<TResponse>(
        Func<Task<TResponse>> processFunc,
        [CallerMemberName] string? methodName = null)
        where TResponse : Response
    {
        return logger.LogTaskAsync(
            async () =>
            {
                var result = await processFunc();
                return StatusCode(result.StatusCode, result) as IActionResult;
            },
            methodName);
    }
}
```

### Route Template

```
api/[appName]/[controllerType]/v1/[controller]
```

| Placeholder | Заполняется через | Пример |
|-------------|-------------------|--------|
| `[appName]` | `[AppName("...")]` атрибут | `template`, `bff` |
| `[controllerType]` | `[ControllerType("...")]` атрибут | `getter`, `setter` |
| `[controller]` | `ControllerNameConvention` | `persons`, `user-profiles` |

**Итоговый маршрут:**
```
api/template/getter/v1/persons
api/bff/bff/v1/orders
```

### Process<TResponse>

Метод оборачивает выполнение CQRS-запроса с:
- Автоматическим логированием через `logger.LogTaskAsync()`
- HTTP status code mapping из `Response.StatusCode`
- CallerMemberName для идентификации метода в логах

**Пример:**
```csharp
[AppName("Template")]
[ControllerType("getter")]
public class PersonsController(
    ILogger<PersonsController> logger,
    IPersonsService personsService)
    : ControllerBase(logger)
{
    [HttpGet("{id}")]
    public Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        return Process(() => personsService.GetByIdAsync(id, ct));
    }

    [HttpPost("list")]
    public Task<IActionResult> GetList([FromBody] PersonListRequest request, CancellationToken ct)
    {
        return Process(() => personsService.GetListAsync(request, ct));
    }

    [HttpPost]
    public Task<IActionResult> Create([FromBody] PersonCreateRequest request, CancellationToken ct)
    {
        return Process(() => personsService.CreateAsync(request, ct));
    }
}
```

---

## ControllerAuthBase

Контроллер с обязательной аутентификацией.

```csharp
[ApiController]
[Authorize]
public abstract class ControllerAuthBase(ILogger logger) : ControllerBase(logger)
{
}
```

Все методы такого контроллера требуют валидный authentication token.

**Пример:**
```csharp
[AppName("Template")]
[ControllerType("getter")]
public class SecurePersonsController(
    ILogger<SecurePersonsController> logger,
    IPersonsService personsService)
    : ControllerAuthBase(logger)
{
    [HttpGet("{id}")]
    public Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        return Process(() => personsService.GetByIdAsync(id, ct));
    }
}
```

---

## Routing Conventions

### ControllerNameConvention

Нормализует имена контроллеров для маршрутов:

- Удаляет суффикс `Controller`
- Преобразует PascalCase → kebab-case

```csharp
public class ControllerNameConvention : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        // PersonsController     → "persons"
        // OrdersController      → "orders"
        // UserProfilesController → "user-profiles"
    }
}
```

### ControllerTypeConvention

Автоматически обнаруживает атрибуты-наследники `ControllerRouteAttributeBase` и подставляет их значения в маршрут.

**Алгоритм:**
1. Получает все типы атрибутов из сборок через `AssemblyHelper.GetDerivedTypesFromAssemblies<ControllerRouteAttributeBase>()`
2. Для каждого контроллера ищет атрибуты (включая базовые классы)
3. Преобразует имя атрибута в ключ шаблона (`ControllerTypeAttribute` → `controllerType`)
4. Заменяет placeholder в маршруте на значение атрибута в kebab-case

```csharp
// До:
[Route("api/[appName]/[controllerType]/v1/[controller]")]

// После применения конвенции:
[Route("api/template/getter/v1/persons")]
```

---

## Routing Attributes

### ControllerRouteAttributeBase

Базовый атрибут для кастомных routing-плейсхолдеров.

```csharp
[AttributeUsage(AttributeTargets.Class)]
public abstract class ControllerRouteAttributeBase(string value) : Attribute
{
    public string Value { get; } = value;
}
```

### AppNameAttribute

Указывает имя приложения для маршрутизации.

```csharp
public class AppNameAttribute(string value) : ControllerRouteAttributeBase(value)
```

**Пример:**
```csharp
[AppName("Template")]
[ControllerType("getter")]
public abstract class GetterControllerBase { }
// Маршрут: api/template/getter/v1/{controller}
```

### ControllerTypeAttribute

Указывает тип контроллера для маршрутизации.

```csharp
public class ControllerTypeAttribute(string value) : ControllerRouteAttributeBase(value)
```

**Пример:**
```csharp
[ControllerType("getter")]
public abstract class GetterControllerBase { }
// Маршрут: api/{appName}/getter/v1/{controller}
```

---

## App Configuration

### ImplementDependencies()

One-call настройка DI для presentation layer.

```csharp
public static WebApplicationBuilder ImplementDependencies(this WebApplicationBuilder builder)
{
    builder.Configuration.InitializeConfiguration(builder.Environment);
    builder.Services
        .AddControllers(options =>
        {
            options.Conventions.Add(new ControllerTypeConvention());
            options.Conventions.Add(new ControllerNameConvention());
            options.Filters.Add<RequestLoggingFilter>();
        }).Services
        .AddReferencedDependencyInjectors();
    return builder;
}
```

**Что делает:**
1. Инициализирует `.env` конфигурацию
2. Регистрирует контроллеры с кастомными конвенциями
3. Добавляет `RequestLoggingFilter` для логирования запросов
4. Автоматически находит и выполняет все `DependencyInjectorBase` из ссылочных сборок

### UsePresentationCore()

Настройка middleware pipeline.

```csharp
public static IApplicationBuilder UsePresentationCore(this WebApplication app)
{
    LoggingServiceAccessor.Configure(app.Services);

    app.UseCorrelationId();
    if (app.Environment.IsDevelopment())
    {
        app.UseSwaggerConfigured();
    }

    app.UseExceptionHandler();
    app.UseAuthorization();
    app.MapControllers();

    return app;
}
```

**Порядок middleware:**
```
Request → CorrelationId → Swagger (dev) → ExceptionHandler → Authorization → Controllers
```

### Полный пример Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.ImplementDependencies();
builder.Services.AddFluentValidation();

var app = builder.Build();

app.UsePresentationCore();

app.Run();
```

---

## FluentValidation Integration

### AddFluentValidation()

Автоматически обнаруживает валидаторы и настраивает обработку ошибок.

```csharp
public static IServiceCollection AddFluentValidation(this IServiceCollection services)
{
    var applicationAssemblies =
        AssemblyHelper.GetAssembliesContainingDerivedGenericTypes(typeof(AbstractValidator<>));

    return services
        .AddFluentValidationAutoValidation()
        .AddValidatorsFromAssemblies(applicationAssemblies, includeInternalTypes: true)
        .Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = actionContext =>
            {
                var errors = actionContext.ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .SelectMany(e => e.Value!.Errors
                        .Select(error => new ValidationFailure(e.Key, error.ErrorMessage)))
                    .ToList();
                throw new ValidationException(errors);
            };
        });
}
```

**Что делает:**
1. Находит все сборки с `AbstractValidator<T>`
2. Регистрирует все валидаторы (включая internal)
3. Включает автоматическую валидацию
4. **Ключевое:** при ошибке валидации выбрасывает `ValidationException` вместо возврата 400 — это позволяет единому ExceptionHandler обработать ошибку консистентно

### Пример валидатора

```csharp
public class PersonCreateRequestValidator : AbstractValidator<PersonCreateRequest>
{
    public PersonCreateRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Имя обязательно")
            .MaximumLength(100).WithMessage("Имя не более 100 символов");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен")
            .EmailAddress().WithMessage("Некорректный email");
    }
}
```

---

## Correlation ID

### CorrelationIdMiddleware

Гарантирует наличие Correlation ID в каждом HTTP-запросе.

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

**Поведение:**
- Если запрос уже содержит Correlation ID header — используется он
- Если нет — генерируется новый GUID
- Correlation ID возвращается в response headers для клиентского трекинга

---

## HttpRequestExtensions

Утилиты для работы с HTTP-запросами.

```csharp
// Получение Correlation ID
var correlationId = request.GetCorrelationId();

// Добавление Correlation ID (если отсутствует)
request.TryAddCorrelationId();
```

---

---

## См. также

| Документ | Описание |
|----------|----------|
| [CQRS](cqrs.md) | Использование Process<TResponse> с MediatR |
| [Exception Mapping](exception-mapping.md) | ValidationException → HTTP Response mapping |
| [Request Logging](request-logging.md) | RequestLoggingFilter — логирование запросов |
| [Swagger](swagger.md) | Swagger конфигурация для Development |
| [Fluent Validation Integration](fluent-validation-integration.md) | Интеграция FluentValidation с ASP.NET Core |
