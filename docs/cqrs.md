# 📋 CQRS (Command Query Responsibility Segregation)

**Assembly:** `Shared.Application.Cqrs.Core.dll`  
**Namespace:** `Shared.Application.Cqrs.Core.Abstractions`  
**Исходники:** `src/Shared/Core/Shared.Application.Cqrs.Core/`

---

## 🚀 Quick Start

```csharp
// 1. Команда создания — наследник CreateCommand<TRequest, TResponse>
public sealed record PersonCreateRequest : PersonCreateDto;

public sealed record PersonCreateCommand(PersonCreateRequest Request)
    : CreateCommand<PersonCreateRequest, PersonCreateResponse>(Request);

// 2. Запрос списка — наследник ReadListQuery<TRequest, TFilter, TResponse>
public sealed record PersonReadListQuery(PersonListRequest request)
    : ReadListQuery<PersonListRequest, PersonListFilter, PersonListResponse>(request);

// 3. Регистрация в DI выполняется через DependencyInjector.Process()
//    (Shared.Application.Cqrs.Core.DependencyInjection.DependencyInjector),
//    который вызывает AddMediatR() из пакета Shared.Application.Cqrs.Core.
//    Метод сканирует сборки по префиксу entry assembly
//    (AssemblyHelper.GetAssembliesByPrefix) и регистрирует MediatR
//    и pipeline-поведения (Logging + Validation).

// 4. Использование в Controller/Service
public sealed class PersonsController(ISender sender) : ControllerBase
{
    [HttpPost("create")]
    public Task<IActionResult> Create(
        [FromBody] PersonCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        return Process(
            () => sender.Send(new PersonCreateCommand(request), cancellationToken));
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
| **Handlers** | Базовые классы с готовой инфраструктурой (Repository, UnitOfWork, Mapper, Validators) |
| **Pipeline Behaviours** | Logging → Validation (регистрируются автоматически) |
| **DI Extension** | `AddMediatR()` в `Shared.Application.Cqrs.Core.Extensions.DependencyInjectionExtensions` |

---

## 🔌 Commands

### Базовые интерфейсы

| Интерфейс | Наследует | Описание |
|-----------|-----------|----------|
| `ICommand` | `IRequest<Unit>` | Команда без ответа |
| `ICommand<TResponse>` | `IRequest<TResponse>` | Команда с ответом |
| `ICommand<TRequest, TResponse>` | `ICommand<TResponse>` | Команда с вложенным Request-объектом |

```csharp
public interface ICommand : IRequest<Unit> { }

public interface ICommand<out TResponse> : IRequest<TResponse> { }

public interface ICommand<out TRequest, out TResponse> : ICommand<TResponse>
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
public interface ICommandHandler<in TCommand>
    : IRequestHandler<TCommand, Unit>
    where TCommand : ICommand;

public interface ICommandHandler<in TCommand, TResponse>
    : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>;
```

### Built-in Commands

| Command | Параметры primary ctor | Response | Описание |
|---------|------------------------|----------|----------|
| `CreateCommand<TRequest, TResponse>` | `TRequest Request` | `TResponse` | Создание сущности |
| `UpdateCommand<TRequest, TResponse>` | `object Key`, `TRequest Request` | `TResponse` | Обновление по ключу |
| `DeleteCommand` | `object Key` | `Response` | Удаление по ключу |
| `CloneCommand<TRequest, TResponse>` | `object Key`, `TRequest Request` | `TResponse` | Клонирование по ключу |

#### CreateCommand

```csharp
public abstract record CreateCommand<TRequest, TResponse>(TRequest Request)
    : ICommand<TResponse>;
```

**Пример (из проекта):**

```csharp
// Request наследует PersonCreateDto (только Name/Email)
public sealed record PersonCreateRequest : PersonCreateDto;

public sealed record PersonCreateCommand(PersonCreateRequest Request)
    : CreateCommand<PersonCreateRequest, PersonCreateResponse>(Request);
```

#### UpdateCommand

```csharp
public abstract record UpdateCommand<TRequest, TResponse>(
    object Key,
    TRequest Request)
    : ICommand<TResponse>;
```

#### DeleteCommand

```csharp
public abstract record DeleteCommand(object Key) : ICommand<Response>;
```

#### CloneCommand

```csharp
public abstract record CloneCommand<TRequest, TResponse>(object Key, TRequest Request)
    : ICommand<TResponse>;
```

### Built-in Command Handlers

| Handler | Type Parameters | Описание |
|---------|----------------|----------|
| `CreateCommandHandler<TCommand, TRequest, TEntity, TResponsePayload, TResponse>` | 5 | Создаёт entity через mapper, валидирует, сохраняет |
| `UpdateCommandHandler<TCommand, TRequest, TEntity, TPayload, TResponse>` | 5 | Находит по Key, mapит изменения, валидирует, сохраняет |
| `DeleteCommandHandler<TCommand, TEntity>` | 3 (включая `Response`) | Находит по Key, удаляет (soft delete) |
| `CloneCommandHandler<TCommand, TRequest, TEntity, TResponsePayload, TResponse>` | 5 | Находит по Key, клонирует через mapper, сохраняет |

> **Обратите внимание на `TResponsePayload` в `CreateCommandHandler` и `TPayload` в `UpdateCommandHandler`.** Это разные generic-параметры, несмотря на схожую роль.

**Пример `CreateCommandHandler` (из проекта):**

```csharp
public sealed class PersonCreateCommandHandler(
    ILoggerFactory loggerFactory,
    IMapper mapper,
    IUnitOfWork unitOfWork,
    IEnumerable<IValidator<Domain.Entities.Person>> validators,
    IUserProvider userProvider)
    : CreateCommandHandler<
        PersonCreateCommand,
        PersonCreateRequest,
        Domain.Entities.Person,
        PersonDto,
        PersonCreateResponse>(
        loggerFactory,
        mapper,
        unitOfWork,
        validators,
        userProvider);
```

Базовый `CreateCommandHandler` сам формирует ответ через `CreateResponseDto`:

```csharp
protected virtual TResponse CreateResponseDto(TEntity entity) =>
    new()
    {
        Id = entity.Id,
        Payload = mapper.Map<TEntity, TResponsePayload>(entity),
        StatusCode = StatusCodes.Status201Created,
    };
```

---

## 🔍 Queries

### Базовые интерфейсы

| Интерфейс | Наследует | Описание |
|-----------|-----------|----------|
| `IQuery<TResponse>` | `IRequest<TResponse>` | Запрос на чтение |

```csharp
public interface IQuery<out TResponse> : IRequest<TResponse> { }
```

### Интерфейс Query Handler

| Интерфейс | Описание |
|-----------|----------|
| `IQueryHandler<TQuery, TResponse>` | Обработчик запроса (`IRequestHandler<TQuery, TResponse>`) |

```csharp
public interface IQueryHandler<in TQuery, TResponse>
    : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>;
```

### Built-in Queries

| Query | Параметры primary ctor | Response | Описание |
|-------|------------------------|----------|----------|
| `ReadByKeyQuery<TResponse>` | `object key` | `TResponse` | Чтение одной сущности по ключу |
| `ReadListQuery<TRequest, TFilter, TResponse>` | `TRequest request` | `TResponse` | Чтение списка с пагинацией, фильтрами, сортировкой |

#### ReadByKeyQuery

```csharp
public abstract class ReadByKeyQuery<TResponse>(object key) : IQuery<TResponse>
{
    public object Key { get; } = key;
}
```

#### ReadListQuery

```csharp
public abstract class ReadListQuery<TRequest, TFilter, TResponse>(TRequest request)
    : IQuery<TResponse>
    where TRequest : PageableRequest<TFilter>
    where TFilter : new()
{
    public int PageNumber { get; } = request.PageNumber < 1 ? 1 : request.PageNumber;
    public int? PageSize { get; } = request.PageSize;
    public TRequest Request => request;
    public TFilter Filter { get; } = request.Filter ?? new TFilter();
}
```

**Пример (из Getter-сервиса):**

```csharp
public sealed record PersonReadListQuery(PersonListRequest request)
    : ReadListQuery<PersonListRequest, PersonListFilter, PersonListResponse>(request);

public record PersonListRequest(DalPattern DalPattern) : PageableRequest<PersonListFilter>;

public record PersonListFilter
{
    public string? Name { get; init; }
    public string? NameContains { get; init; }
    public string? Email { get; init; }
    public string? EmailContains { get; init; }
}
```

### Built-in Query Handlers

| Handler | Type Parameters | Описание |
|---------|----------------|----------|
| `ReadQueryHandler<TQuery, TEntity, TResponse>` | 3 | Читает entity по Key, mapит в Response |
| `ReadListQueryHandler<TQuery, TRequest, TFilter, TResponse, TPayload, TEntity>` | 6 | Пагинация, фильтрация, сортировка, маппинг коллекции |

> В `ReadListQueryHandler` дополнительно автоматически применяется фильтр по `Ids`, если `TFilter : ListFilterBase` и `Ids.Any()`.

---

## 🧱 Base Classes

### RequestHandler

Базовый класс для всех обработчиков. Предоставляет logging, guards, validation для сущности.

```csharp
public abstract class RequestHandler<TRequest, TResponse>(ILoggerFactory loggerFactory)
    : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    protected readonly ILogger Logger =
        loggerFactory.CreateLogger<RequestHandler<TRequest, TResponse>>();

    public abstract Task<TResponse> Handle(TRequest query, CancellationToken cancellationToken);

    protected virtual Task GuardAsync(TRequest request, CancellationToken cancellationToken);

    protected virtual async Task ValidateAsync<TEntity>(
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
| `ValidateAsync<TEntity>()` | `virtual Task` | Последовательно вызывает все `IValidator<TEntity>`; бросает `ValidationException` при failures |

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
    protected virtual bool WithTracking => false;
    protected virtual bool AsSplitQuery => false;

    protected IRepository<TEntity> Repository => unitOfWork.GetRepository<TEntity>();

    protected virtual QueryOptions<TEntity> ConstructOptions(TRequest request);
    protected virtual QueryOptions<TEntity> ConstructOptions(TRequest request, bool withDeletable);
    protected virtual Task ProcessEntityAsync(TEntity entity, TRequest request, CancellationToken cancellationToken);
    protected virtual Task ProcessResponseAsync(TResponse response, TRequest request, CancellationToken cancellationToken);
    protected virtual Task ProcessEntitiesAsync(ICollection<TEntity> entities, TRequest request, CancellationToken cancellationToken);
}
```

| Метод/Свойство | Тип | Описание |
|----------------|-----|----------|
| `WithTracking` | `bool` | Признак отслеживания изменений (по умолчанию `false`) |
| `AsSplitQuery` | `bool` | Загрузка связанных сущностей раздельными запросами |
| `Repository` | `IRepository<TEntity>` | Репозиторий текущей сущности |
| `ConstructOptions()` | `QueryOptions<TEntity>` | Построение параметров запроса (авто-фильтр `IsDeleted` для `IWithDeleted`) |
| `ProcessEntityAsync()` | `Task` | Хук для операций с entity перед сохранением |
| `ProcessResponseAsync()` | `Task` | Хук для пост-обработки response |
| `ProcessEntitiesAsync()` | `Task` | Хук для операций с коллекцией entities |

**Пример использования:**

```csharp
public sealed class PersonUpdateCommandHandler(
    ILoggerFactory loggerFactory,
    IMapper mapper,
    IUnitOfWork unitOfWork,
    IEnumerable<IValidator<Domain.Entities.Person>> validators,
    IUserProvider? userProvider)
    : UpdateCommandHandler<
        PersonUpdateCommand,
        PersonUpdateRequest,
        Domain.Entities.Person,
        PersonDto,
        PersonUpdateResponse>(
        loggerFactory, mapper, unitOfWork, validators, userProvider)
{
    // Включение tracking для update-операции
    protected override bool WithTracking => true;

    protected override Task ProcessEntityAsync(
        Domain.Entities.Person entity,
        PersonUpdateCommand command,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation("Updating person {Id}", entity.Id);
        return Task.CompletedTask;
    }
}
```

---

## 💉 Dependency Injection

### Регистрация

`AddMediatR()` **не вызывается напрямую из `Program.cs`**. В слое `Shared.Application.Cqrs.Core` есть наследник `DependencyInjectorBase`, который инкапсулирует регистрацию:

```csharp
// src/Shared/Core/Shared.Application.Cqrs.Core/DependencyInjection/DependencyInjector.cs
public class DependencyInjector(ILoggerFactory loggerFactory)
    : DependencyInjectorBase(loggerFactory)
{
    protected override IServiceCollection Process(IServiceCollection serviceCollection) =>
        serviceCollection.AddMediatR();
}
```

`AddMediatR()` из `Shared.Application.Cqrs.Core.Extensions.DependencyInjectionExtensions`:

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

> **Префикс сборок определяется динамически** через `AssemblyHelper.GetAssembliesByPrefix()` — берётся первая часть имени entry assembly до точки (например, для `Template.Setter.Api` префикс — `Template`).

**Что делает `AddMediatR()`:**

1. Регистрирует MediatR с assemblies из `AssemblyHelper.GetAssembliesByPrefix()`.
2. Регистрирует pipeline-поведения в порядке:
   - `LoggingPipelineBehaviour` (1-й)
   - `ValidationPipelineBehaviour` (2-й)

### Порядок выполнения Pipeline

```
Request → LoggingPipelineBehaviour → ValidationPipelineBehaviour → Handler → Response
             ↓                              ↓
          LogTaskAsync()             Task.WhenAll(validators) → ValidationException
```

Подробнее — см. [Pipeline Behaviours](pipeline-behaviors.md).

---

## 📊 Built-in Response Types

CQRS-модуль предоставляет собственные response-типы для команд:

### CreateResponse

```csharp
public record CreateResponse<TDto> : Response<TDto>
{
    public CreateResponse() { }

    public CreateResponse(object Id, TDto Payload, int StatusCode = StatusCodes.Status201Created)
        : base(Payload, StatusCode)
    {
        this.Id = Id;
    }

    public object Id { get; init; }
}
```

**Пример (из проекта):**

```csharp
public sealed record PersonCreateResponse : CreateResponse<PersonDto>;
```

### UpdateResponse

```csharp
public record UpdateResponse<TDto> : Response<TDto>
{
    public object Key { get; init; }
}
```

### PageableResponse (в `Shared.Application.Core.Dto.Responses`)

```csharp
public record PageableResponse<T> : Response<T>
{
    public PageableResponse() { }

    public PageableResponse(int totalPages, int pageNumber, T? payload, int statusCode = StatusCodes.Status200OK)
        : base(payload, statusCode)
    {
        TotalPages = totalPages;
        PageNumber = pageNumber;
    }

    public int TotalPages { get; init; }
    public int PageNumber { get; init; }
}
```

> Все response-типы — `record` с `init`-свойствами (не `set`), что соответствует immutable-контракту. Полная иерархия описана в [Response Types](response-types.md).

---

## 🧩 EntityRemoveCommand / EntityRemoveCommandHandler

Готовая команда для удаления любой сущности по идентификатору — поддерживает soft delete и аудит.

```csharp
public record EntityRemoveCommand<TEntity>(EntityRemoveRequest Request)
    : DeleteCommand(Request.Id)
    where TEntity : class, IEntity<Guid>;
```

```csharp
public class EntityRemoveCommandHandler<TEntity>(
    IUnitOfWork unitOfWork,
    ILoggerFactory loggerFactory,
    IUserProvider? userProvider)
    : DeleteCommandHandler<EntityRemoveCommand<TEntity>, TEntity>(
        unitOfWork, loggerFactory, userProvider)
    where TEntity : class, IEntity<Guid>;
```

**Использование:**

```csharp
// Не нужно писать свой Handler
var command = new EntityRemoveCommand<Person>(new EntityRemoveRequest { Id = personId });
await sender.Send(command, cancellationToken);
```

`DeleteCommandHandler` сам:
- Извлекает сущность через `IUnitOfWork.GetRepository<TEntity>()` (`GetByIdOrThrowAsync`).
- Если сущность реализует `IWithDeleted` / `IWithDeleteAction` — выполняет soft delete.
- Иначе — hard delete через `Repository.RemoveAsync`.
- Заполняет аудит-поля через `IUserProvider`.

---

## 📝 Best Practices

| Сценарий | Recommendation |
|----------|---------------|
| CRUD-операции над сущностями | ✅ Built-in Commands/Queries |
| Сложная бизнес-логика | ✅ Custom Command + Handler |
| Чтение с фильтрацией/пагинацией | ✅ `ReadListQuery` + Specification |
| Простые read-операции | ✅ `ReadByKeyQuery` |
| Удаление по id с аудитом | ✅ `EntityRemoveCommand<TEntity>` |
| Транзакционные операции | ✅ Command + UnitOfWork |

### Правила

1. **Commands** — для write-операций (изменяют состояние).
2. **Queries** — для read-операций (не изменяют состояние).
3. **Handlers** — тонкие, делегируют логику в Domain/Services.
4. **Validators** — FluentValidation, отдельный класс на `TEntity` для `ValidateAsync()` в `RequestHandler` (см. [FluentValidation Integration](fluent-validation-integration.md)).
5. **Specifications** — для сложных `QueryOptions` (фильтры, includes).

---

## См. также

| Документ | Описание |
|----------|----------|
| [Pipeline Behaviors](pipeline-behaviors.md) | Pipeline Behaviours — Logging, Validation |
| [Auto-Registration](auto-registration.md) | `RegisterDerivedTypeDependencies` и `DependencyInjectorBase` |
| [Response Types](response-types.md) | `ResponseBase`, `Response<T>`, `PageableResponse`, `ErrorResponse` |
| [FluentValidation Integration](fluent-validation-integration.md) | Двухуровневая валидация (ModelState + Pipeline) |
| [Exception Mapping](exception-mapping.md) | Exception → HTTP Response mapping |
| [Repository Pattern](repository.md) | `IRepository<T>` — работа с данными |
| [Unit of Work](unit-of-work.md) | `IUnitOfWork` — управление транзакциями |
