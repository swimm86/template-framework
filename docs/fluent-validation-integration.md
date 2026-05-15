# FluentValidation Integration

**Сборка:** `Shared.Presentation.Core.dll`, `Shared.Application.Cqrs.Core.dll`  
**Namespace:** `Shared.Presentation.Core.Extensions`, `Shared.Application.Cqrs.Core.Behaviours`  
**Исходники:** `src/Shared/Core/Shared.Presentation.Core/Extensions/`, `src/Shared/Core/Shared.Application.Cqrs.Core/Behaviours/`

---

## 🚀 Quick Start

```csharp
// Program.cs — регистрация (автоматически находит все валидаторы)
builder.Services.AddFluentValidation();
builder.Services.AddMediatR();  // Регистрирует ValidationPipelineBehaviour

// Validator — автоматически обнаруживается
public class PersonCreateRequestValidator : AbstractValidator<PersonCreateRequest>
{
    public PersonCreateRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Имя обязательно.")
            .MaximumLength(100).WithMessage("Имя не более 100 символов.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен.")
            .EmailAddress().WithMessage("Некорректный формат email.");
    }
}

// Command/Query — валидация происходит автоматически
var command = new PersonCreateCommand(new PersonCreateRequest("", "invalid"));
var response = await mediator.Send(command, ct);
// → ValidationException → 400 Bad Request
```

---

## 📐 Архитектура

Валидация работает на **двух уровнях**:

```
┌─────────────────────────────────────────────────────────────┐
│                    HTTP Request                              │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  Presentation Layer: AddFluentValidation()                   │
│  • Auto-discovery validators из assemblies                   │
│  • InvalidModelStateResponseFactory → ValidationException    │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  MediatR Pipeline: ValidationPipelineBehaviour               │
│  • Inject: IEnumerable<IValidator<TRequest>>                 │
│  • Task.WhenAll() — параллельная валидация                   │
│  • Deduplication ошибок                                      │
│  • ValidationException при failures                          │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  Exception Mapper: ValidationExceptionMapper                 │
│  • ValidationException → 400 Bad Request                     │
│  • ErrorResponse с коллекцией ProblemDetails                 │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  HTTP Response: 400 Bad Request                              │
│  {                                                           │
│    "statusCode": 400,                                        │
│    "title": "Ошибка валидации",                              │
│    "errors": [                                               │
│      { "detail": "Имя обязательно.", "instance": "Name" },   │
│      { "detail": "Email обязателен.", "instance": "Email" }  │
│    ]                                                         │
│  }                                                           │
└─────────────────────────────────────────────────────────────┘
```

---

## 🔧 FluentValidation Registration

**Файл:** `Shared.Presentation.Core/Extensions/FluentValidationExtensions.cs`

### AddFluentValidation()

```csharp
public static IServiceCollection AddFluentValidation(this IServiceCollection services)
{
    // 1. Auto-discovery assemblies с валидаторами
    var applicationAssemblies =
        AssemblyHelper.GetAssembliesContainingDerivedGenericTypes(
            typeof(AbstractValidator<>));

    return services
        // 2. Автоматическая валидация ModelState
        .AddFluentValidationAutoValidation()
        // 3. Регистрация валидаторов из всех assemblies
        .AddValidatorsFromAssemblies(applicationAssemblies, includeInternalTypes: true)
        // 4. Кастомная обработка ошибок валидации
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

### Что делает

| Шаг | Действие |
|-----|----------|
| 1 | `AssemblyHelper.GetAssembliesContainingDerivedGenericTypes(typeof(AbstractValidator<>))` — находит все сборки, содержащие типы, наследующие `AbstractValidator<T>` |
| 2 | `AddFluentValidationAutoValidation()` — включает автоматическую валидацию ModelState для controller-параметров |
| 3 | `AddValidatorsFromAssemblies(..., includeInternalTypes: true)` — регистрирует все валидаторы (включая `internal`) из найденных assemblies |
| 4 | `InvalidModelStateResponseFactory` — переопределяет обработку ошибок ModelState: вместо `400 Bad Request` с `ValidationProblemDetails` бросается `ValidationException` |

### Auto-discovery

```csharp
var applicationAssemblies =
    AssemblyHelper.GetAssembliesContainingDerivedGenericTypes(typeof(AbstractValidator<>));
```

Находит все загруженные assemblies, в которых есть типы, наследующие `AbstractValidator<T>`. Это означает:

- Валидаторы из **любой** сборки проекта автоматически регистрируются
- Не нужно вручную указывать assemblies
- Работает с `Shared.*`, `Services.*`, и любыми другими сборками с префиксом

### InvalidModelStateResponseFactory

Когда ASP.NET Core обнаруживает ошибки валидации ModelState (например, `[Required]` атрибут не прошёл), вместо стандартного ответа вызывается кастомный factory:

```csharp
options.InvalidModelStateResponseFactory = actionContext =>
{
    // 1. Извлекаем все ошибки из ModelState
    var errors = actionContext.ModelState
        .Where(e => e.Value?.Errors.Count > 0)
        .SelectMany(e => e.Value!.Errors
            .Select(error => new ValidationFailure(e.Key, error.ErrorMessage)))
        .ToList();

    // 2. Бросаем ValidationException — будет обработан ExceptionMapper
    throw new ValidationException(errors);
};
```

**Зачем:**统一 обработка ошибок валидации через `ValidationExceptionMapper` → `400 Bad Request` с `ErrorResponse`.

---

## ✅ ValidationPipelineBehaviour

**Файл:** `Shared.Application.Cqrs.Core/Behaviours/ValidationPipelineBehaviour.cs`

Pipeline Behaviour для MediatR, который валидирует **все** запросы/команды перед выполнением Handler.

### Signature

```csharp
internal sealed class ValidationPipelineBehaviour<TRequest, TResponse>(
    ILogger<ValidationPipelineBehaviour<TRequest, TResponse>> logger,
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
```

### Implementation

```csharp
public async Task<TResponse> Handle(
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken)
{
    var requestName = typeof(TRequest).Name;

    logger.LogDebug("Start processing validation for {RequestName}.", requestName);

    // SHORT-CIRCUIT: нет валидаторов → сразу к handler
    if (!validators.Any())
    {
        logger.LogDebug("There is no any validator for {RequestName}.", requestName);
        return await next();
    }

    // PARALLEL: все валидаторы запускаются одновременно
    var validationContext = new ValidationContext<TRequest>(request);
    var validationResults = await Task.WhenAll(
        validators.Select(v => v.ValidateAsync(validationContext, cancellationToken)));

    // DEDUPLICATION: группировка по (PropertyName, ErrorMessage)
    var failures = validationResults
        .SelectMany(result => result.Errors)
        .Where(failure => failure is not null)
        .GroupBy(f => new { f.PropertyName, f.ErrorMessage })
        .Select(g => g.First())
        .ToList();

    // SUCCESS
    if (failures.Count == 0)
    {
        logger.LogDebug("Validation for {RequestName} succeeded.", requestName);
        return await next();
    }

    // FAILURE
    logger.LogWarning("Validation for {RequestName} failed.", requestName);
    throw new ValidationException(failures);
}
```

### Ключевые особенности

| Фича | Описание |
|------|----------|
| **Parallel Validation** | `Task.WhenAll()` — все валидаторы для `TRequest` запускаются параллельно |
| **Deduplication** | `GroupBy(f => new { f.PropertyName, f.ErrorMessage })` — убирает дубликаты ошибок |
| **Short-Circuit** | Если нет валидаторов для `TRequest` — сразу вызывает `next()` без overhead |
| **ValidationException** | Бросается при наличии failures; содержит все `ValidationFailure` |
| **Logging** | `LogDebug` для start/success/no validators, `LogWarning` для failure |

### Порядок валидации

```
1. Request приходит в ValidationPipelineBehaviour
2. DI injects все IValidator<TRequest> из контейнера
3. Если валидаторов нет → next() (handler)
4. Если есть → Task.WhenAll(validators) — параллельно
5. GroupBy + deduplication ошибок
6. Если failures → ValidationException
7. Если OK → next() (handler)
```

### Deduplication пример

```csharp
// Два валидатора возвращают одинаковую ошибку:
// Validator 1: [Name: "Имя обязательно."]
// Validator 2: [Name: "Имя обязательно."]

// После GroupBy + Select(g => g.First()):
// [Name: "Имя обязательно."]  ← только одна ошибка
```

---

## 🔗 Как работают вместе

### Полный поток: Request → 400 Response

```
┌──────────────────────────────────────────────────────────────┐
│  1. HTTP POST /api/persons                                    │
│     Body: { "name": "", "email": "invalid" }                  │
└──────────────────────┬───────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│  2. Controller: PersonsController.Create(request)             │
│     → PersonsService.CreateAsync(request)                     │
│     → mediator.Send(new PersonCreateCommand(request))         │
└──────────────────────┬───────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│  3. LoggingPipelineBehaviour                                  │
│     → logger.LogTaskAsync("PersonCreateCommand handler")      │
└──────────────────────┬───────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│  4. ValidationPipelineBehaviour                               │
│     → Inject: IValidator<PersonCreateCommand>                 │
│     → Task.WhenAll(validators)                                │
│     → failures: [                                             │
│         { PropertyName: "Request.Name",                       │
│           ErrorMessage: "Имя обязательно." },                 │
│         { PropertyName: "Request.Email",                      │
│           ErrorMessage: "Некорректный формат email." }        │
│       ]                                                       │
│     → throw ValidationException(failures)                     │
└──────────────────────┬───────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│  5. ExceptionHandler (IExceptionHandler)                      │
│     → ExceptionMapperResolver.Map(exception)                  │
│     → ValidationExceptionMapper → ErrorResponse:              │
│         {                                                     │
│           "statusCode": 400,                                  │
│           "title": "Ошибка валидации",                        │
│           "errors": [...]                                     │
│         }                                                     │
└──────────────────────┬───────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│  6. HTTP 400 Bad Request                                      │
│     Content-Type: application/problem+json                    │
│     Body: ErrorResponse                                      │
└──────────────────────────────────────────────────────────────┘
```

### Пример Validator для Command

```csharp
// ВАЖНО: ValidationPipelineBehaviour валидирует TRequest (Command/Query),
// а не вложенный Request-объект.

public class PersonCreateCommandValidator : AbstractValidator<PersonCreateCommand>
{
    public PersonCreateCommandValidator()
    {
        // Валидация вложенного Request
        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Имя обязательно.")
            .MaximumLength(100).WithMessage("Имя не более 100 символов.");

        RuleFor(x => x.Request.Email)
            .NotEmpty().WithMessage("Email обязателен.")
            .EmailAddress().WithMessage("Некорректный формат email.");
    }
}
```

### Альтернатива: Validator для вложенного Request

Если валидатор зарегистрирован для `PersonCreateRequest` (не для Command), он **не** будет вызван `ValidationPipelineBehaviour` автоматически. Для этого нужно:

```csharp
// Вариант 1: SetValidator внутри Command-валидатора
public class PersonCreateCommandValidator : AbstractValidator<PersonCreateCommand>
{
    public PersonCreateCommandValidator(PersonCreateRequestValidator requestValidator)
    {
        RuleFor(x => x.Request).SetValidator(requestValidator);
    }
}

// Вариант 2: Валидация в Handler через RequestHandler.ValidateAsync()
public class PersonCreateCommandHandler : CreateCommandHandler<...>
{
    protected override async Task GuardAsync(PersonCreateCommand command, CancellationToken ct)
    {
        await ValidateAsync(command.Request, _requestValidators, ct);
    }
}
```

---

## 📊 Troubleshooting

### Валидация не срабатывает

**Проблема:** `ValidationPipelineBehaviour` не валидирует Request.

**Причина:** Нет зарегистрированного `IValidator<TRequest>` для конкретного типа.

**Решение:**

```csharp
// Убедитесь что Validator зарегистрирован для правильного типа
public class PersonCreateCommandValidator : AbstractValidator<PersonCreateCommand> { }

// Или используйте SetValidator для вложенного Request
RuleFor(x => x.Request).SetValidator(new PersonCreateRequestValidator());
```

### ModelState валидация vs Pipeline валидация

| | ModelState Validation | ValidationPipelineBehaviour |
|--|----------------------|----------------------------|
| **Что валидирует** | Параметры controller-методов (DTO) | MediatR Request (Command/Query) |
| **Когда** | До входа в controller | До входа в Handler |
| **Как** | ASP.NET Core model binding | `Task.WhenAll(validators)` |
| **Результат** | `ValidationException` → 400 | `ValidationException` → 400 |

**Оба пути** приводят к `ValidationException`, который обрабатывается `ValidationExceptionMapper` → `400 Bad Request`.

### Дубликаты ошибок

**Проблема:** Одна и та же ошибка появляется несколько раз.

**Причина:** Несколько валидаторов для одного типа возвращают одинаковые ошибки.

**Решение:** `ValidationPipelineBehaviour` уже делает deduplication через `GroupBy`. Если дубликаты есть — проверьте что нет дублирующихся Validators для одного типа.

---

## 📝 Best Practices

1. **Один Validator на Request** — избегайте множественных валидаторов для одного типа
2. **Валидируйте Command/Query** — `ValidationPipelineBehaviour` работает с `TRequest`, не с вложенными объектами
3. **Используйте SetValidator** — для переиспользования валидаторов вложенных Request
4. **Не логируйте в Validators** — логирование уже есть в Pipeline Behaviours
5. **CancellationToken** — передавайте в `ValidateAsync()` для graceful cancellation
6. **Internal validators** — `includeInternalTypes: true` позволяет использовать `internal` валидаторы
7. **Async валидация** — используйте `MustAsync()` для async-проверок (например, проверка уникальности в БД)

---

---

## См. также

| Документ | Описание |
|----------|----------|
| [CQRS](cqrs.md) | Commands, Queries, Handlers |
| [Pipeline Behaviors](pipeline-behaviors.md) | Pipeline Behaviours — Logging, Validation |
| [Exception Mapping](exception-mapping.md) | Exception → HTTP Response mapping |
| [Controllers](controllers.md) | API Controllers — Request/Response DTOs |
