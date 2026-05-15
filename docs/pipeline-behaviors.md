# 🔧 Pipeline Behaviours

**Assembly:** `Shared.Application.Cqrs.Core.dll`  
**Namespace:** `Shared.Application.Cqrs.Core.Behaviours`  
**Исходники:** `src/Shared/Core/Shared.Application.Cqrs.Core/Behaviours/`

---

## 🚀 Quick Start

Pipeline Behaviours — это middleware для MediatR, которые выполняются **до** и **после** каждого Handler.

```csharp
// Регистрация (автоматическая через AddMediatR())
services.AddMediatR();

// Pipeline выполняется в порядке регистрации:
// 1. LoggingPipelineBehaviour  → логирует выполнение
// 2. ValidationPipelineBehaviour → валидирует через FluentValidation
// 3. Handler                   → бизнес-логика

// Пример: при отправке команды
var command = new PersonCreateCommand(new PersonCreateRequest("John", "john@example.com"));
var response = await mediator.Send(command, ct);

// Console/Logs output:
// [INFO] Starting 'PersonCreateCommand' handler...
// [DEBUG] Start processing validation for PersonCreateCommand.
// [DEBUG] Validation for PersonCreateCommand succeeded.
// [INFO] Completed 'PersonCreateCommand' handler.
```

---

## 📐 Архитектура

```
┌─────────────────────────────────────────────────────────────┐
│                      IMediator.Send()                        │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│              LoggingPipelineBehaviour                        │
│  • logger.LogTaskAsync()                                     │
│  • Логирует начало и завершение выполнения                   │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│             ValidationPipelineBehaviour                      │
│  • Inject: IEnumerable<IValidator<TRequest>>                 │
│  • Task.WhenAll() — параллельная валидация                   │
│  • Deduplication ошибок по (PropertyName, ErrorMessage)      │
│  • Short-circuit если нет валидаторов                        │
│  • ValidationException при failures                          │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                        Handler                               │
│  • ICommandHandler<TCommand, TResponse>                      │
│  • IQueryHandler<TQuery, TResponse>                          │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                       Response                               │
└─────────────────────────────────────────────────────────────┘
```

---

## 📝 LoggingPipelineBehaviour

Логирование выполнения каждого запроса/команды.

### Signature

```csharp
internal sealed class LoggingPipelineBehaviour<TRequest, TResponse>(
    ILogger<LoggingPipelineBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
```

### Implementation

```csharp
public Task<TResponse> Handle(
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken _)
{
    return logger.LogTaskAsync(
        () => next(),
        processDescription: $"'{request.GetType().Name}' handler");
}
```

### Поведение

| Аспект | Описание |
|--------|----------|
| **Метод** | `logger.LogTaskAsync()` из `Shared.Common.Logging.Extensions` |
| **Что логирует** | Начало и завершение выполнения handler |
| **Description** | `'{RequestTypeName}' handler` (например: `'PersonCreateCommand' handler`) |
| **CancellationToken** | Игнорируется (`_`) — логирование не должно прерываться |
| **Exception handling** | `LogTaskAsync` логирует исключения автоматически |

### Пример логов

```
[INFO] Starting 'PersonReadListQuery' handler...
[INFO] Completed 'PersonReadListQuery' handler. Duration: 45ms

[INFO] Starting 'PersonCreateCommand' handler...
[ERROR] Failed 'PersonCreateCommand' handler. Exception: ValidationException...
```

---

## ✅ ValidationPipelineBehaviour

Валидация запросов через **FluentValidation** перед выполнением Handler.

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

    // SHORT-CIRCUIT: если нет валидаторов — сразу к handler
    if (!validators.Any())
    {
        logger.LogDebug("There is no any validator for {RequestName}.", requestName);
        return await next();
    }

    // PARALLEL: запуск всех валидаторов одновременно
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
| **Parallel Validation** | `Task.WhenAll()` — все валидаторы запускаются параллельно |
| **Deduplication** | `GroupBy(f => new { f.PropertyName, f.ErrorMessage })` — убирает дубликаты ошибок |
| **Short-Circuit** | Если нет валидаторов — сразу вызывает `next()` без overhead |
| **ValidationException** | Бросается при наличии failures (FluentValidation) |
| **Logging** | Debug для start/success/no validators, Warning для failure |

### Порядок валидации

```
1. Request приходит в PipelineBehaviour
2. DI injects все IValidator<TRequest> из контейнера
3. Если валидаторов нет → next() (handler)
4. Если есть → Task.WhenAll(validators)
5. GroupBy + deduplication ошибок
6. Если failures → ValidationException
7. Если OK → next() (handler)
```

### Пример Validator

```csharp
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
```

### Пример ошибки валидации

```
[DEBUG] Start processing validation for PersonCreateCommand.
[WARNING] Validation for PersonCreateCommand failed.

ValidationException:
  - Name: Имя обязательно.
  - Email: Некорректный формат email.
```

---

## 🔄 Registration Order & Execution Flow

### Регистрация

```csharp
// DependencyInjectionExtensions.cs
private static IServiceCollection AddPipelineBehaviours(this IServiceCollection services)
{
    return services
        .AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingPipelineBehaviour<,>))     // 1-й
        .AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehaviour<,>)); // 2-й
}
```

### Execution Flow

```
┌─────────────────────────────────────────────────────────────────┐
│  Request: PersonCreateCommand                                    │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│  1. LoggingPipelineBehaviour                                     │
│     → logger.LogTaskAsync("PersonCreateCommand handler")         │
│     → Записывает: "Starting 'PersonCreateCommand' handler..."    │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│  2. ValidationPipelineBehaviour                                  │
│     → Inject: IEnumerable<IValidator<PersonCreateCommand>>       │
│     → Task.WhenAll(validators)                                   │
│     → Если errors → ValidationException (handler не вызывается)  │
│     → Если OK → next()                                           │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│  3. PersonCreateCommandHandler                                   │
│     → GuardAsync()                                               │
│     → CreateAsync()                                              │
│     → Return PersonCreateResponse                                │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│  Response: PersonCreateResponse                                  │
│     → LoggingPipelineBehaviour записывает:                       │
│       "Completed 'PersonCreateCommand' handler"                  │
└─────────────────────────────────────────────────────────────────┘
```

### Важные правила

1. **Порядок регистрации = порядок выполнения**
   - 1-й зарегистрированный = 1-й выполняется
   - Logging всегда первый → логирует весь pipeline
   - Validation всегда второй → валидирует до handler

2. **Short-circuit**
   - ValidationPipelineBehaviour может не вызвать `next()` при errors
   - Handler не выполнится если валидация не прошла

3. **Exception propagation**
   - ValidationException пробрасывается через LoggingPipelineBehaviour
   - LoggingPipelineBehaviour логирует исключения через `LogTaskAsync()`

---

## 🔨 Кастомизация

### Создание Custom Behaviour

```csharp
public class AuthorizationPipelineBehaviour<TRequest, TResponse>(
    IAuthorizationService authService,
    ILogger<AuthorizationPipelineBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest).Name;

        // Проверка авторизации
        var isAuthorized = await authService.AuthorizeAsync(request, cancellationToken);
        if (!isAuthorized)
        {
            logger.LogWarning("Unauthorized access attempt for {RequestType}", requestType);
            throw new UnauthorizedAccessException($"Access denied for {requestType}");
        }

        logger.LogDebug("Authorization passed for {RequestType}", requestType);
        return await next();
    }
}
```

### Регистрация Custom Behaviour

```csharp
// ДО Logging (самый первый)
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuthorizationPipelineBehaviour<,>));

// ПОСЛЕ Validation (последний перед handler)
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(CachingPipelineBehaviour<,>));
```

### Рекомендуемый порядок

```csharp
services
    .AddScoped(typeof(IPipelineBehavior<,>), typeof(AuthorizationPipelineBehaviour<,>))  // 1. Auth
    .AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingPipelineBehaviour<,>))        // 2. Logging
    .AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehaviour<,>))     // 3. Validation
    .AddScoped(typeof(IPipelineBehavior<,>), typeof(CachingPipelineBehaviour<,>));       // 4. Caching
```

---

## 🔗 Интеграция с CQRS Handlers

### Автоматическая интеграция

Pipeline Behaviours работают **автоматически** для всех `IRequest<TResponse>`:

```csharp
// Все эти типы проходят через pipeline:
ICommand                → Logging → Validation → Handler
ICommand<TResponse>     → Logging → Validation → Handler
IQuery<TResponse>       → Logging → Validation → Handler
```

### Пример полного цикла

```csharp
// 1. Command
public record PersonCreateCommand(PersonCreateRequest Request)
    : CreateCommand<PersonCreateRequest, PersonCreateResponse>(Request);

// 2. Validator (для Request внутри Command)
// ВАЖНО: ValidationPipelineBehaviour валидирует TRequest (PersonCreateCommand),
// а не PersonCreateRequest. Для валидации вложенного Request:
public class PersonCreateCommandValidator : AbstractValidator<PersonCreateCommand>
{
    public PersonCreateCommandValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty();
        RuleFor(x => x.Request.Email).EmailAddress();
    }
}

// 3. Handler
public class PersonCreateCommandHandler(...)
    : CreateCommandHandler<PersonCreateCommand, PersonCreateRequest, Person, ...>(...)
{
    // Handler вызовется ТОЛЬКО если ValidationPipelineBehaviour не бросил ValidationException
}

// 4. Execution
var command = new PersonCreateCommand(new PersonCreateRequest("", "invalid"));
var response = await mediator.Send(command, ct);
// → ValidationPipelineBehaviour бросит ValidationException
// → Handler НЕ вызовется
// → LoggingPipelineBehaviour залоггирует ошибку
```

### Валидация в RequestHandler.ValidateAsync()

Помимо Pipeline Behaviour, базовый `RequestHandler` предоставляет `ValidateAsync<TEntity>()`:

```csharp
public abstract class RequestHandler<TRequest, TResponse>(ILoggerFactory loggerFactory)
{
    protected virtual async Task ValidateAsync<TEntity>(
        TEntity entity,
        IEnumerable<IValidator<TEntity>> validators,
        CancellationToken cancellationToken = default)
    {
        // Последовательная валидация entity (не Request!)
        var failures = new List<ValidationFailure>();
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(
                new ValidationContext<TEntity>(entity), cancellationToken);
            failures.AddRange(result.Errors.Where(e => e is not null));
        }

        if (failures.Count != 0)
            throw new ValidationException(failures);
    }
}
```

**Разница:**

| | ValidationPipelineBehaviour | RequestHandler.ValidateAsync() |
|--|---------------------------|-------------------------------|
| **Что валидирует** | `TRequest` (Command/Query) | `TEntity` (Domain Entity) |
| **Когда** | До Handler | Внутри Handler |
| **Как** | Параллельно (Task.WhenAll) | Последовательно (foreach) |
| **Deduplication** | ✅ Да | ❌ Нет |
| **Использование** | Автоматически | Вызывается вручную в Handler |

---

## 📊 Troubleshooting

### Валидация не срабатывает

**Проблема:** ValidationPipelineBehaviour не валидирует Request.

**Причина:** Нет зарегистрированного `IValidator<TRequest>` для конкретного типа.

**Решение:**
```csharp
// Убедитесь что Validator зарегистрирован
public class PersonCreateCommandValidator : AbstractValidator<PersonCreateCommand> { }

// Или для вложенного Request
public class PersonCreateRequestValidator : AbstractValidator<PersonCreateRequest> { }
```

### Дубликаты ошибок валидации

**Проблема:** Одна и та же ошибка появляется несколько раз.

**Причина:** Несколько валидаторов для одного типа возвращают одинаковые ошибки.

**Решение:** ValidationPipelineBehaviour уже делает deduplication через `GroupBy`. Если дубликаты есть — проверьте что у вас нет дублирующихся Validators.

### Performance при множестве валидаторов

**Проблема:** Медленная валидация при большом количестве validators.

**Решение:** ValidationPipelineBehaviour уже использует `Task.WhenAll()` для параллельного выполнения. Убедитесь что сами валидаторы не содержат блокирующих операций.

---

## 📝 Best Practices

1. **Один Validator на Request** — избегайте множественных валидаторов для одного типа
2. **Валидируйте Request, не Entity** — Pipeline Behaviour для Request, ValidateAsync() для Entity
3. **Не логируйте в Validators** — логирование уже есть в Pipeline Behaviours
4. **Используйте CancellationToken** — передавайте в `ValidateAsync()` для отмены
5. **Кастомные Behaviours — в начало** — Authorization перед Logging и Validation

---

## См. также

| Документ | Описание |
|----------|----------|
| [CQRS](cqrs.md) | Commands, Queries, Handlers |
| [Logging](logging.md) | LogTask и [LogMethod] — структурированное логирование |
| [Auto-Registration](auto-registration.md) | AssemblyHelper — авто-регистрация DI |
| [Exception Mapping](exception-mapping.md) | Exception → HTTP Response mapping
