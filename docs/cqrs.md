# 📋 CQRS (Command Query Responsibility Segregation)

**Assembly:** `Shared.Application.Cqrs.Core.dll`  
**Namespace:** `Shared.Application.Cqrs.Core.Abstractions`  
**Исходники:** `src/Shared/Core/Shared.Application.Cqrs.Core/`

---

## 🚀 Quick Start

```csharp
// 1. Команда на создание
// DTO сущности "Person" переиспользуется как Request: поля Id/Name/Email/Hash.
public sealed record PersonCreateRequest : PersonDto;

public record PersonCreateCommand(PersonCreateRequest Request)
    : CreateCommand<PersonCreateRequest, PersonCreateResponse>(Request);

// 2. Query на чтение списка
public class PersonReadListQuery(PersonListRequest request)
    : ReadListQuery<PersonListRequest, PersonListFilter, PersonListResponse>(request);

// 3. Регистрация в DI
services.AddMediatR(); // Автоматически регистрирует все handlers из assemblies с префиксом "Shared"

// 4. Использование в Service/Controller
public class PersonsService(IMediator mediator) : IPersonsService
{
    public async Task<PersonListResponse> GetListAsync(PersonListRequest request, CancellationToken ct)
    {
        var query = new PersonReadListQuery(request);
        return await mediator.Send(query, ct);
    }

    public async Task<PersonCreateResponse> CreateAsync(PersonCreateRequest request, CancellationToken ct)
    {
        var command = new PersonCreateCommand(request);
        return await mediator.Send(command, ct);
    }
}
```

---

## 📐 Архитектура

CQRS-модуль реализует паттерн **Command Query Responsibility Segregation** поверх **MediatR**, предоставляя:

| Компонент | Назначение |
|-----------|-----------|
| **Commands** | Write-операции (Create, Update, Delete, Clone) |
| **Queries** | Read-операции (ReadByKey, ReadList) |
| **Handlers** | Базовые классы с готовой инфраструктурой |
| **Pipeline Behaviours** | Logging → Validation (автоматически) |
| **DI Extensions** | Авто-регистрация через `AssemblyHelper.GetAssembliesByPrefix()` |

---

## 🔌 Commands

### Базовые интерфейсы

| Интерфейс | Наследует | Описание |
|-----------|-----------|----------|
| `ICommand` | `IRequest<Unit>` | Команда без ответа |
| `ICommand<TResponse>` | `IRequest<TResponse>` | Команда с ответом |
| `ICommand<TRequest, TResponse>` | `ICommand<TResponse>` | Команда с вложенным Request-объектом |

```csharp
// Без ответа
public interface ICommand : IRequest<Unit> { }

// С ответом
public interface ICommand<TResponse> : IRequest<TResponse> { }

// С Request-объектом
public interface ICommand<TRequest, TResponse> : ICommand<TResponse>
{
    TRequest Request { get; }
}
```

### Интерфейсы Handlers

| Интерфейс | Описание |
|-----------|----------|
| `ICommandHandler<TCommand>` | Обработчик команды без ответа (`IRequestHandler<TCommand, Unit>`) |
| `ICommandHandler<TCommand, TResponse>` | Обработчик команды с ответом (`IRequestHandler<TCommand, TResponse>`) |

```csharp
public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, Unit>
    where TCommand : ICommand { }

public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse> { }
```

### Built-in Commands

| Command | Параметры | Response | Описание |
|---------|-----------|----------|----------|
| `CreateCommand<TRequest, TResponse>` | `TRequest Request` | `TResponse` | Создание сущности |
| `UpdateCommand<TRequest, TResponse>` | `object Key`, `TRequest Request` | `TResponse` | Обновление по ключу |
| `DeleteCommand` | `object Key` | `Response` | Удаление по ключу |
| `CloneCommand<TRequest, TResponse>` | `object Key`, `TRequest Request` | `TResponse` | Клонирование по ключу |

#### CreateCommand

```csharp
public abstract record CreateCommand<TRequest, TResponse>(TRequest Request) : ICommand<TResponse>;
```

**Пример:**
```csharp
// Request наследует общий PersonDto (record-to-record)
public sealed record PersonCreateRequest : PersonDto;

public record PersonCreateCommand(PersonCreateRequest Request)
    : CreateCommand<PersonCreateRequest, PersonCreateResponse>(Request);
```

#### UpdateCommand

```csharp
public abstract record UpdateCommand<TRequest, TResponse>(object Key, TRequest Request) : ICommand<TResponse>;
```

**Пример:**
```csharp
public record PersonUpdateCommand(Guid Key, PersonUpdateRequest Request)
    : UpdateCommand<PersonUpdateRequest, PersonUpdateResponse>(Key, Request);
```

#### DeleteCommand

```csharp
public abstract record DeleteCommand(object Key) : ICommand<Response>;
```

**Пример:**
```csharp
public record PersonDeleteCommand(Guid Key) : DeleteCommand(Key);
```

#### CloneCommand

```csharp
public abstract record CloneCommand<TRequest, TResponse>(object Key, TRequest Request) : ICommand<TResponse>;
```

**Пример:**
```csharp
public record PersonCloneCommand(Guid Key, PersonCloneRequest Request)
    : CloneCommand<PersonCloneRequest, PersonCreateResponse>(Key, Request);
```

### Built-in Command Handlers

| Handler | Type Parameters | Описание |
|---------|----------------|----------|
| `CreateCommandHandler<TCommand, TRequest, TEntity, TResponsePayload, TResponse>` | 5 | Создаёт entity через mapper, валидирует, сохраняет |
| `UpdateCommandHandler<TCommand, TRequest, TEntity, TPayload, TResponse>` | 5 | Находит по Key, mapит изменения, валидирует, сохраняет |
| `DeleteCommandHandler<TCommand, TEntity>` | 2 | Находит по Key, удаляет (soft delete) |
| `CloneCommandHandler<TCommand, TRequest, TEntity, TResponsePayload, TResponse>` | 5 | Находит по Key, клонирует через mapper, сохраняет |

**Пример CreateCommandHandler:**
```csharp
public class PersonCreateCommandHandler(
    ILoggerFactory loggerFactory,
    IMapper mapper,
    IUnitOfWork unitOfWork,
    IEnumerable<IValidator<Person>> validators,
    IUserProvider userProvider)
    : CreateCommandHandler<PersonCreateCommand, PersonCreateRequest, Person, PersonDto, PersonCreateResponse>(
        loggerFactory, mapper, unitOfWork, validators, userProvider)
{
    // Переопределение ProcessEntityAsync для кастомной логики
    protected override Task ProcessEntityAsync(Person entity, PersonCreateCommand command)
    {
        // Дополнительная обработка перед сохранением
        return Task.CompletedTask;
    }
}
```

---

## 🔍 Queries

### Базовые интерфейсы

| Интерфейс | Наследует | Описание |
|-----------|-----------|----------|
| `IQuery<TResponse>` | `IRequest<TResponse>` | Запрос на чтение |

```csharp
public interface IQuery<TResponse> : IRequest<TResponse> { }
```

### Интерфейс Query Handler

| Интерфейс | Описание |
|-----------|----------|
| `IQueryHandler<TQuery, TResponse>` | Обработчик запроса (`IRequestHandler<TQuery, TResponse>`) |

```csharp
public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse> { }
```

### Built-in Queries

| Query | Параметры | Response | Описание |
|-------|-----------|----------|----------|
| `ReadByKeyQuery<TResponse>` | `object Key` | `TResponse` | Чтение одной сущности по ключу |
| `ReadListQuery<TRequest, TFilter, TResponse>` | `TRequest Request` | `TResponse` | Чтение списка с пагинацией, фильтрами, сортировкой |

#### ReadByKeyQuery

```csharp
public abstract class ReadByKeyQuery<TResponse>(object key) : IQuery<TResponse>
{
    public object Key { get; } = key;
}
```

**Пример:**
```csharp
public class PersonReadByKeyQuery(Guid key) : ReadByKeyQuery<PersonResponse>(key);
```

#### ReadListQuery

```csharp
public abstract class ReadListQuery<TRequest, TFilter, TResponse>(TRequest request)
    : IQuery<TResponse>
    where TRequest : PageableRequest<TFilter>
    where TFilter : new()
{
    public int PageNumber { get; }       // Минимум = 1
    public int? PageSize { get; }
    public TRequest Request { get; }
    public TFilter Filter { get; }
}
```

**Пример:**
```csharp
public class PersonReadListQuery(PersonListRequest request)
    : ReadListQuery<PersonListRequest, PersonListFilter, PersonListResponse>(request);

// Request должен наследовать PageableRequest<TFilter>
public record PersonListRequest(DalPattern DalPattern) : PageableRequest<PersonListFilter>;

// Фильтр
public class PersonListFilter
{
    public string? Name { get; set; }
    public string? NameContains { get; set; }
    public string? Email { get; set; }
    public string? EmailContains { get; set; }
}
```

### Built-in Query Handlers

| Handler | Type Parameters | Описание |
|---------|----------------|----------|
| `ReadQueryHandler<TQuery, TEntity, TResponse>` | 3 | Читает entity по Key, mapит в Response |
| `ReadListQueryHandler<TQuery, TRequest, TFilter, TResponse, TPayload, TEntity>` | 6 | Пагинация, фильтрация, сортировка, маппинг коллекции |

**Пример ReadQueryHandler:**
```csharp
public class PersonReadQueryHandler(
    ILoggerFactory loggerFactory,
    IMapper mapper,
    IUnitOfWork unitOfWork)
    : ReadQueryHandler<PersonReadByKeyQuery, Person, PersonResponse>(
        loggerFactory, mapper, unitOfWork)
{
    protected override QueryOptions<Person> ConstructOptions(PersonReadByKeyQuery query)
    {
        var options = base.ConstructOptions(query);
        // Добавить Includes, Filters и т.д.
        return options;
    }
}
```

**Пример ReadListQueryHandler:**
```csharp
public class PersonReadListQueryHandler(
    ILoggerFactory loggerFactory,
    IUnitOfWork unitOfWork)
    : ReadListQueryHandler<PersonReadListQuery, PersonListRequest, PersonListFilter,
                           PersonListResponse, PersonListPayload, Person>(
        loggerFactory, unitOfWork)
{
    protected override QueryOptions<Person> ConstructOptions(PersonReadListQuery query)
    {
        var specification = new PersonSpecification(query.Request);
        var options = specification.BuildOptions();
        ApplySortOptions(query.Request.ConvertSortOptions(), options);
        return options;
    }
}
```

---

## 🧱 Base Classes

### RequestHandler

Базовый класс для всех обработчиков. Предоставляет logging, guards, validation.

```csharp
public abstract class RequestHandler<TRequest, TResponse>(ILoggerFactory loggerFactory)
    : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    // Защищённый логгер (создаётся через loggerFactory)
    protected readonly ILogger Logger;

    // Основной метод обработки
    public abstract Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);

    // Pre-condition проверки (переопределяется в наследниках)
    protected virtual Task GuardAsync(TRequest request, CancellationToken cancellationToken);

    // Валидация entity через коллекцию IValidator<TEntity>
    protected virtual Task ValidateAsync<TEntity>(
        TEntity entity,
        IEnumerable<IValidator<TEntity>> validators,
        CancellationToken cancellationToken = default);
}
```

| Метод/Свойство | Тип | Описание |
|----------------|-----|----------|
| `Logger` | `ILogger` | Инстанс логгера для текущего handler |
| `Handle()` | `abstract Task<TResponse>` | Основной метод обработки |
| `GuardAsync()` | `virtual Task` | Pre-condition проверки (guards) |
| `ValidateAsync<TEntity>()` | `virtual Task` | Поиск и вызов валидаторов для TEntity |

### EntityRequestHandler

Расширяет `RequestHandler` для работы с сущностями через Repository + UnitOfWork.

```csharp
public abstract class EntityRequestHandler<TRequest, TResponse, TEntity>(
    IUnitOfWork unitOfWork,
    ILoggerFactory loggerFactory)
    : RequestHandler<TRequest, TResponse>(loggerFactory)
    where TRequest : IRequest<TResponse>
    where TEntity : class, IEntity
{
    // Настройки запроса
    protected virtual bool WithTracking => false;
    protected virtual bool AsSplitQuery => false;

    // Репозиторий (через UnitOfWork)
    protected IRepository<TEntity> Repository => unitOfWork.GetRepository<TEntity>();

    // Построение QueryOptions
    protected virtual QueryOptions<TEntity> ConstructOptions(TRequest request);
    protected virtual QueryOptions<TEntity> ConstructOptions(TRequest request, bool withDeletable);

    // Хуки для кастомизации
    protected virtual Task ProcessEntityAsync(TEntity entity, TRequest request);
    protected virtual Task ProcessResponseAsync(TResponse response, TRequest request);
    protected virtual Task ProcessEntitiesAsync(ICollection<TEntity> entities, TRequest request);
}
```

| Метод/Свойство | Тип | Описание |
|----------------|-----|----------|
| `WithTracking` | `bool` | Признак отслеживания изменений (по умолчанию `false`) |
| `AsSplitQuery` | `bool` | Подгрузка связанных сущностей раздельными запросами |
| `Repository` | `IRepository<TEntity>` | Репозиторий текущей сущности |
| `ConstructOptions()` | `QueryOptions<TEntity>` | Построение параметров запроса (авто-фильтр IsDeleted) |
| `ProcessEntityAsync()` | `Task` | Хук для операций с entity перед сохранением |
| `ProcessResponseAsync()` | `Task` | Хук для пост-обработки response |
| `ProcessEntitiesAsync()` | `Task` | Хук для операций с коллекцией entities |

**Пример использования:**
```csharp
public class PersonUpdateCommandHandler(
    ILoggerFactory loggerFactory,
    IMapper mapper,
    IUnitOfWork unitOfWork,
    IEnumerable<IValidator<Person>> validators,
    IUserProvider userProvider)
    : UpdateCommandHandler<PersonUpdateCommand, PersonUpdateRequest, Person, PersonPayload, PersonUpdateResponse>(
        loggerFactory, mapper, unitOfWork, validators, userProvider)
{
    // Включение tracking для update-операции
    protected override bool WithTracking => true;

    protected override Task ProcessEntityAsync(Person entity, PersonUpdateCommand command)
    {
        // Кастомная логика перед сохранением
        Logger.LogInformation("Updating person {Id}", entity.Id);
        return Task.CompletedTask;
    }
}
```

---

## 💉 Dependency Injection

### Регистрация

```csharp
// Program.cs / Startup.cs
services.AddMediatR();
```

**Что делает `AddMediatR()`:**

1. Регистрирует MediatR с assemblies из `AssemblyHelper.GetAssembliesByPrefix()`
2. Регистрирует pipeline behaviours в порядке:
   - `LoggingPipelineBehaviour` (1-й)
   - `ValidationPipelineBehaviour` (2-й)

```csharp
public static IServiceCollection AddMediatR(this IServiceCollection services)
{
    return services
        .AddMediatR(opt => opt.RegisterServicesFromAssemblies(
            AssemblyHelper.GetAssembliesByPrefix().ToArray()))
        .AddPipelineBehaviours();
}

private static IServiceCollection AddPipelineBehaviours(this IServiceCollection services)
{
    return services
        .AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingPipelineBehaviour<,>))
        .AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehaviour<,>));
}
```

### Порядок выполнения Pipeline

```
Request → LoggingPipelineBehaviour → ValidationPipelineBehaviour → Handler → Response
            ↓                              ↓
         logger.LogTaskAsync()        Task.WhenAll(validators)
                                        ↓
                                   ValidationException (если errors)
```

---

## 🔗 Интеграция с Services

### Типичный Service с CQRS

```csharp
public class PersonsService(
    IMediator mediator,
    ILogger<PersonsService> logger) : IPersonsService
{
    // === READ ===
    public async Task<PersonResponse> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var query = new PersonReadByKeyQuery(id);
        return await mediator.Send(query, ct);
    }

    public async Task<PersonListResponse> GetListAsync(
        PersonListRequest request, CancellationToken ct)
    {
        var query = new PersonReadListQuery(request);
        return await mediator.Send(query, ct);
    }

    // === WRITE ===
    public async Task<PersonCreateResponse> CreateAsync(
        PersonCreateRequest request, CancellationToken ct)
    {
        var command = new PersonCreateCommand(request);
        return await mediator.Send(command, ct);
    }

    public async Task<Response> DeleteAsync(Guid id, CancellationToken ct)
    {
        var command = new PersonDeleteCommand(id);
        return await mediator.Send(command, ct);
    }
}
```

### Controller → Service → CQRS

```csharp
[ApiController]
[Route("api/[controller]")]
public class PersonsController(IPersonsService personsService) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult<PersonResponse>> GetById(
        Guid id, CancellationToken ct)
    {
        var response = await personsService.GetByIdAsync(id, ct);
        return Ok(response);
    }

    [HttpPost("list")]
    public async Task<ActionResult<PersonListResponse>> GetList(
        [FromBody] PersonListRequest request, CancellationToken ct)
    {
        var response = await personsService.GetListAsync(request, ct);
        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<PersonCreateResponse>> Create(
        [FromBody] PersonCreateRequest request, CancellationToken ct)
    {
        var response = await personsService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }
}
```

---

## 📊 Response Types

### CreateResponse

```csharp
public class CreateResponse<TPayload> where TPayload : class
{
    public Guid Id { get; set; }
    public TPayload? Payload { get; set; }
    public int StatusCode { get; set; }
}
```

### UpdateResponse

```csharp
public class UpdateResponse<TPayload>
{
    public object Key { get; set; }
    public TPayload? Payload { get; set; }
    public int StatusCode { get; set; }
}
```

### PageableResponse (для ReadListQuery)

```csharp
public class PageableResponse<TPayload>
{
    public TPayload Payload { get; set; }
    public int StatusCode { get; set; }
    public int TotalPages { get; set; }
    public int PageNumber { get; set; }
}
```

---

## 📝 Best Practices

### Когда использовать CQRS

| Сценарий | Recommendation |
|----------|---------------|
| CRUD-операции над сущностями | ✅ Built-in Commands/Queries |
| Сложная бизнес-логика | ✅ Custom Command + Handler |
| Чтение с фильтрацией/пагинацией | ✅ ReadListQuery + Specification |
| Простые read-операции без кэширования | ✅ ReadByKeyQuery |
| Транзакционные операции | ✅ Command + UnitOfWork |

### Правила

1. **Commands** — для write-операций (изменяют состояние)
2. **Queries** — для read-операций (не изменяют состояние)
3. **Handlers** — тонкие, делегируют логику в Domain/Services
4. **Validators** — FluentValidation, отдельный класс на каждый Request
5. **Specifications** — для сложных QueryOptions (фильтры, includes)

### Готовые CQRS-функции

Помимо базовых абстракций (ICommand, IQuery), Framework предоставляет готовые функции:

#### EntityRemoveCommand / EntityRemoveCommandHandler

Универсальная команда удаления сущности с поддержкой soft delete и аудита через `IUserProvider`:

```csharp
// Использование — не нужно писать свой Handler
var command = new EntityRemoveCommand<PersonRemoveRequest>(request);
await sender.Send(command, cancellationToken);
```

Готовый обработчик `EntityRemoveCommandHandler`:
- Извлекает сущность через `IUnitOfWork.GetRepository<TEntity>()`
- Если сущность реализует `IWithDeleted` / `IWithDeleteAction` — выполняет soft delete
- Иначе — hard delete
- Заполняет аудит-поля через `IUserProvider`

---

## См. также

| Документ | Описание |
|----------|----------|
| [Pipeline Behaviors](pipeline-behaviors.md) | Pipeline Behaviours — Logging, Validation |
| [Auto-Registration](auto-registration.md) | AssemblyHelper — авто-регистрация DI |
| [Exception Mapping](exception-mapping.md) | Exception → HTTP Response mapping |
| [Repository Pattern](repository.md) | IRepository<T> — работа с данными |
| [Unit of Work](unit-of-work.md) | IUnitOfWork — управление транзакциями |
