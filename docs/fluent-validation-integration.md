# FluentValidation Integration

**Сборка:** `Shared.Presentation.Core.dll`, `Shared.Application.Cqrs.Core.dll`  
**Namespace:** `Shared.Presentation.Core.Extensions`, `Shared.Application.Cqrs.Core.Behaviours`  
**Исходники:** `src/Shared/Core/Shared.Presentation.Core/Extensions/FluentValidationExtensions.cs`, `src/Shared/Core/Shared.Application.Cqrs.Core/Behaviours/`

---

## 🚀 Quick Start

```csharp
// Program.cs — регистрация (автоматически находит все валидаторы)
builder.Services.AddFluentValidation();
builder.Services.AddMediatR();  // Регистрирует ValidationPipelineBehaviour

// Validator для сущности — автоматически обнаруживается
public sealed class PersonValidator : AbstractValidator<Domain.Entities.Person>
{
    public PersonValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Имя обязательно.")
            .MaximumLength(100).WithMessage("Имя не более 100 символов.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен.")
            .EmailAddress().WithMessage("Некорректный формат email.");
    }
}

// Команда вызывается через MediatR
var command = new PersonCreateCommand(new PersonCreateRequest { Name = "John", Email = "john@example.com" });
var response = await sender.Send(command, ct);
// → 201 Created (PersonCreateResponse)
```

> **Обратите внимание:** в проекте валидаторы регистрируются для `TEntity` (а не для `TRequest`), потому что базовый `RequestHandler.ValidateAsync<TEntity>` вызывается внутри `CreateCommandHandler` / `UpdateCommandHandler` после маппинга `TRequest → TEntity`.

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
│  • Auto-discovery валидаторов из assemblies                   │
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
│  Command Handler: RequestHandler.ValidateAsync<TEntity>()    │
│  • Последовательная валидация сущности                       │
│  • Вызывается после маппинга TRequest → TEntity              │
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
│    "errors": [...]                                           │
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

- Валидаторы из **любой** сборки проекта автоматически регистрируются.
- Не нужно вручную указывать assemblies.
- Префикс сборок определяется через `AssemblyHelper.GetAssembliesByPrefix()` (первая часть имени entry assembly до точки).

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

**Зачем:** единая обработка ошибок валидации через `ValidationExceptionMapper` → `400 Bad Request` с `ErrorResponse`.

---

## ✅ ValidationPipelineBehaviour

**Файл:** `Shared.Application.Cqrs.Core/Behaviours/ValidationPipelineBehaviour.cs`

Pipeline Behaviour для MediatR, который валидирует **все** запросы/команды перед выполнением Handler. Валидирует **тип `TRequest`** (т.е. саму `IRequest<TResponse>`-реализацию), поэтому валидатор должен быть зарегистрирован для типа команды/запроса.

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

## 🔗 Двухуровневая валидация: Pipeline + Handler

### Уровень 1: ValidationPipelineBehaviour (TRequest)

Валидирует **тип команды/запроса** (`TRequest` в `IPipelineBehavior<TRequest, TResponse>`). Срабатывает **до входа в Handler**.

Пример валидатора для команды:

```csharp
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

### Уровень 2: RequestHandler.ValidateAsync<TEntity> (TEntity)

Валидирует **уже замапленный тип сущности** (`TEntity`) внутри Handler. Вызывается **после маппинга** `TRequest → TEntity`.

Из `RequestHandler.cs`:

```csharp
protected virtual async Task ValidateAsync<TEntity>(
    TEntity entity,
    IEnumerable<IValidator<TEntity>> validators,
    CancellationToken cancellationToken = default)
{
    var validatorsArray = validators as IValidator<TEntity>[] ?? validators.ToArray();
    if (validatorsArray.Length == 0)
    {
        return;
    }

    var validationContext = new ValidationContext<TEntity>(entity);
    var failures = new List<ValidationFailure>();

    foreach (var validator in validatorsArray)
    {
        var result = await validator.ValidateAsync(validationContext, cancellationToken);
        failures.AddRange(result.Errors.Where(error => error is not null));
    }

    if (failures.Count != 0)
    {
        throw new ValidationException(failures);
    }
}
```

В `CreateCommandHandler.CreateAsync` (и `UpdateCommandHandler.UpdateAsync`) валидация вызывается так:

```csharp
var entity = mapper.Map<TRequest, TEntity>(command.Request);
await ProcessEntityAsync(entity, command, cancellationToken);
await ValidateAsync(entity, validators, cancellationToken);  // ← валидация TEntity
```

Валидатор для сущности (из проекта):

```csharp
public sealed class PersonValidator : AbstractValidator<Domain.Entities.Person>
{
    public PersonValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name не может быть пустым.")
            .Must(name => name.Length > 0 && char.IsUpper(name[0]))
            .WithMessage("Name должен начинаться с заглавной буквы.")
            .Must(name => !name.Any(char.IsDigit))
            .WithMessage("Name не должен содержать цифр.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email не может быть пустым.")
            .EmailAddress().WithMessage("Некорректный формат Email.");
    }
}
```

### Сравнение уровней

| | ValidationPipelineBehaviour | RequestHandler.ValidateAsync() |
|--|---------------------------|-------------------------------|
| **Что валидирует** | `TRequest` (Command/Query) | `TEntity` (Domain Entity) |
| **Когда** | До Handler | Внутри Handler, после маппинга |
| **Как** | Параллельно (`Task.WhenAll`) | Последовательно (`foreach`) |
| **Deduplication** | ✅ Да | ❌ Нет |
| **Источник валидаторов** | DI-контейнер (`IEnumerable<IValidator<TRequest>>`) | Параметр primary ctor handler'а (`IEnumerable<IValidator<TEntity>>`) |
| **Использование** | Автоматически | Вызывается вручную в `CreateCommandHandler.CreateAsync` / `UpdateCommandHandler.UpdateAsync` |

---

## 🔄 Полный поток: Request → Response / 400

```
┌──────────────────────────────────────────────────────────────┐
│  1. HTTP POST /api/persons/create                            │
│     Body: { "name": "John", "email": "john@example.com" }    │
└──────────────────────┬───────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│  2. Controller: PersonsController.Create(request)            │
│     → sender.Send(new PersonCreateCommand(request))           │
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
│     → Если есть failures → ValidationException                │
│     → Если OK → next() (handler)                              │
└──────────────────────┬───────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│  5. PersonCreateCommandHandler                                │
│     → GuardAsync()                                            │
│     → CreateAsync()                                           │
│        → mapper.Map<PersonCreateRequest, Person>(...)         │
│        → ProcessEntityAsync(entity, command)                  │
│        → ValidateAsync(entity, validators) ← TEntity          │
│        → Repository.AddAsync(...)                             │
│        → unitOfWork.SaveChangesAsync()                        │
│     → Return PersonCreateResponse                             │
└──────────────────────┬───────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│  6. HTTP 201 Created                                          │
│     Body: PersonCreateResponse                                │
└──────────────────────────────────────────────────────────────┘
```

При ошибке валидации `ValidationException` обрабатывается `IExceptionHandler` → `ValidationExceptionMapper` → `ErrorResponse` с `StatusCode = 400`.

---

## 📊 Troubleshooting

### Валидация не срабатывает

**Проблема:** `ValidationPipelineBehaviour` не валидирует Request.

**Причина:** Нет зарегистрированного `IValidator<TRequest>` для конкретного типа команды/запроса.

**Решение:**

```csharp
// Зарегистрируйте валидатор для правильного типа
public class PersonCreateCommandValidator : AbstractValidator<PersonCreateCommand> { }

// Валидатор для вложенного Request — не сработает в Pipeline автоматически.
// Используйте SetValidator внутри Command-валидатора:
public class PersonCreateCommandValidator : AbstractValidator<PersonCreateCommand>
{
    public PersonCreateCommandValidator(PersonCreateRequestValidator requestValidator)
    {
        RuleFor(x => x.Request).SetValidator(requestValidator);
    }
}
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

**Решение:** `ValidationPipelineBehaviour` уже делает deduplication через `GroupBy`. Если дубликаты есть — проверьте, что нет дублирующихся Validators для одного типа.

---

## 📝 Best Practices

1. **Валидация сущности — основной путь.** В проекте валидаторы регистрируются для `TEntity` и вызываются через `ValidateAsync<TEntity>` в `RequestHandler`. Это позволяет валидировать данные **после** маппинга, включая вычисляемые поля.
2. **Валидация команды — для сквозных правил** (`x.Request.Foo.Bar != null`). Используйте `ValidationPipelineBehaviour` + `IValidator<TCommand>`.
3. **Используйте `SetValidator`** — для переиспользования валидаторов вложенных Request в Command-валидаторах.
4. **Не логируйте в Validators** — логирование уже есть в Pipeline Behaviours.
5. **CancellationToken** — передавайте в `ValidateAsync()` для graceful cancellation.
6. **Internal validators** — `includeInternalTypes: true` позволяет использовать `internal` валидаторы.
7. **Async валидация** — используйте `MustAsync()` для async-проверок (например, проверка уникальности в БД).

---

## См. также

| Документ | Описание |
|----------|----------|
| [CQRS](cqrs.md) | Commands, Queries, Handlers |
| [Pipeline Behaviors](pipeline-behaviors.md) | `LoggingPipelineBehaviour` + `ValidationPipelineBehaviour` |
| [Response Types](response-types.md) | `ErrorResponse` для `400 Bad Request` |
| [Exception Mapping](exception-mapping.md) | `ValidationException` → `ErrorResponse` |
| [Controllers](controllers.md) | API Controllers — Request/Response DTOs |
