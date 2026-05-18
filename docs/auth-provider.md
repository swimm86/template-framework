# Auth Provider — IUserProvider

**Assembly:** `Shared.Application.Core.dll`  
**Namespace:** `Shared.Application.Core.Auth`  
**Исходники:** `src/Shared/Core/Shared.Application.Core/Auth/IUserProvider.cs`

---

## Обзор

`IUserProvider` — интерфейс провайдера информации о текущем пользователе. Фреймворк использует его для **автоматического заполнения аудиторских полей** (`CreatedByUserId`, `UpdatedByUserId`, `DeletedByUserId`) при операциях создания, обновления и удаления сущностей.

Интерфейс является точкой расширения: каждый сервис реализует его самостоятельно, извлекая данные пользователя из контекста аутентификации (JWT-claims, HttpContext, заголовки и т.д.).

---

## Интерфейс

```csharp
namespace Shared.Application.Core.Auth;

/// <summary>
/// Интерфейс провайдера пользователя.
/// </summary>
public interface IUserProvider
{
    /// <summary>
    /// Возвращает идентификатор пользователя.
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// Возвращает полное имя пользователя.
    /// </summary>
    string UserFullName { get; }
}
```

| Свойство | Тип | Описание |
|----------|-----|----------|
| `UserId` | `Guid` | Идентификатор текущего пользователя. Используется для заполнения `*ByUserId` полей сущностей |
| `UserFullName` | `string` | Полное имя текущего пользователя. Используется для заполнения `CreatedByUserName` |

---

## Авто-заполнение аудиторских полей

EfRepository и базовые CQRS-обработчики автоматически передают `IUserProvider.UserId` и `IUserProvider.UserFullName` в методы репозитория при выполнении операций с сущностями.

### Механизм в EfRepository

| Операция | Метод | Интерфейс сущности | Заполняемые поля |
|----------|------|--------------------|-------------------|
| Создание | `AddAsync` | `IWithCreated` | `CreatedByUserId`, `CreatedByUserName`, `DateCreated` |
| Обновление | `UpdateCommandHandler` | `IWithUpdated` | `UpdatedByUserId`, `DateUpdated` |
| Удаление (soft) | `RemoveAsync` | `IWithDeleted` | `DeletedByUserId`, `DateDeleted`, `IsDeleted` |

### Создание — EfRepository.AddAsync

```csharp
public async Task<TEntity> AddAsync(
    TEntity entity, Guid? userId, string? userName, CancellationToken cancellationToken = default)
{
    await DbSet.AddAsync(entity, cancellationToken);

    if (entity is IWithCreated entityWithCreated)
    {
        entityWithCreated.OnCreate(userId, userName);
    }

    return entity;
}
```

### Создание — CreateCommandHandler

`CreateCommandHandler` получает `IUserProvider` через DI и передаёт его свойства в `Repository.AddAsync`:

```csharp
public abstract class CreateCommandHandler<...>(
    ILoggerFactory loggerFactory,
    IMapper mapper,
    IUnitOfWork unitOfWork,
    IEnumerable<IValidator<TEntity>> validators,
    IUserProvider userProvider)     // ← внедрение через DI
    : EntityRequestHandler<...>
{
    protected virtual async Task<TResponse> CreateAsync(...)
    {
        var entity = mapper.Map<TRequest, TEntity>(command.Request);
        await ProcessEntityAsync(entity, command);
        await ValidateAsync(entity, validators, cancellationToken);
        var newEntity = await Repository
            .AddAsync(entity, userProvider.UserId, userProvider.UserFullName, cancellationToken);
        //                             ^^^^^^^^^^^^^^^^  ^^^^^^^^^^^^^^^^^^^^^^
        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
        return CreateResponseDto(newEntity);
    }
}
```

### Обновление — UpdateCommandHandler

```csharp
protected virtual async Task<TResponse> UpdateAsync(...)
{
    mapper.Map(command.Request, entity);
    await ProcessEntityAsync(entity, command);
    await ValidateAsync(entity, validators, cancellationToken);

    if (entity is IWithUpdated entityWithUpdated)
    {
        entityWithUpdated.SetUpdatedByUserId(userProvider.UserId);
    }
    // ...
}
```

### Удаление — DeleteCommandHandler

```csharp
protected virtual async Task<Response> DeleteAsync(TEntity entity, TCommand command)
{
    await Repository.RemoveAsync(entity, userId: userProvider.UserId);
    //                                       ^^^^^^^^^^^^^^^^^^^^
    await unitOfWork.SaveChangesAsync(default);
    return new Response { StatusCode = StatusCodes.Status200OK };
}
```

### Удаление — EfRepository.RemoveAsync (soft delete)

```csharp
public Task RemoveAsync(TEntity entity, Guid? userId, bool hard = false, ...)
{
    if (!hard && entity is IWithDeleted deletable)
    {
        deletable.SetIsDeleted();
        deletable.OnDelete(userId);
    }
    else
    {
        DbSet.Remove(entity);
    }

    return Task.CompletedTask;
}
```

---

## Реализация IUserProvider

### HTTP-контекст провайдер

Типичная реализация извлекает данные пользователя из `HttpContext` (JWT-claims, заголовки и т.д.):

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Shared.Application.Core.Auth;

namespace Template.Presentation.Auth;

/// <summary>
/// Провайдер текущего пользователя на основе HTTP-контекста.
/// Извлекает идентификатор и имя из JWT-claims.
/// </summary>
public class HttpUserProvider : IUserProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpUserProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public Guid UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(userIdClaim, out var userId)
                ? userId
                : Guid.Empty;
        }
    }

    /// <inheritdoc />
    public string UserFullName
    {
        get
        {
            return _httpContextAccessor.HttpContext?.User
                .FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        }
    }
}
```

### Провайдер для фоновых задач

Для background-задач (MassTransit consumers, Quartz jobs), где нет HTTP-контекста, используется реализация с предустановленным идентификатором:

```csharp
using Shared.Application.Core.Auth;

namespace Template.Infrastructure.Auth;

/// <summary>
/// Провайдер пользователя для фоновых задач.
/// Возвращает предустановленный идентификатор системного пользователя.
/// </summary>
public class SystemUserProvider : IUserProvider
{
    public static readonly Guid SystemUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public Guid UserId => SystemUserId;
    public string UserFullName => "System";
}
```

---

## Регистрация в DI

### В service-слое (через DependencyInjector)

```csharp
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.Auth;
using Template.Presentation.Auth;

namespace Template.Presentation.DependencyInjection;

public class DependencyInjector : DependencyInjectorBase
{
    protected override IServiceCollection Process(IServiceCollection services)
    {
        return services
            .AddScoped<IUserProvider, HttpUserProvider>();
    }
}
```

### Ручная регистрация в Program.cs

```csharp
builder.Services.AddScoped<IUserProvider, HttpUserProvider>();
```

### Условная регистрация

```csharp
// HTTP-запросы — HttpUserProvider
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserProvider, HttpUserProvider>();

// Фоновые задачи — SystemUserProvider (MassTransit consumers)
builder.Services.AddSingleton<IUserProvider>(sp =>
{
    // Для фоновых задач определяем контекст через scoped factory
    var httpContext = sp.GetService<IHttpContextAccessor>()?.HttpContext;
    return httpContext != null
        ? (IUserProvider)new HttpUserProvider(
            sp.GetRequiredService<IHttpContextAccessor>())
        : new SystemUserProvider();
});
```

---

## Обработка null-значений

Когда контекст пользователя недоступен (анонимный доступ, фоновые задачи, тесты), `IUserProvider` возвращает значения по умолчанию:

| Ситуация | `UserId` | `UserFullName` | Поведение |
|----------|----------|----------------|-----------|
| Анонимный запрос | `Guid.Empty` | `string.Empty` | `CreatedByUserId = null`, `CreatedByUserName = null` |
| Фоновая задача | `SystemUserId` | `"System"` | Аудиторские поля заполняются системным пользователем |
| Тест | `Guid.Empty` | `string.Empty` | Аудиторские поля — `null` |

Методы `OnCreate`, `OnUpdate`, `OnDelete` в интерфейсах сущностей принимают `Guid?` — nullable-параметры. При передаче `Guid.Empty` значение будет сохранено; при передаче `null` — останется `null`.

```csharp
// IWithCreated.OnCreate принимает nullable-параметры:
void OnCreate(Guid? userId, string? userName);

// В EfRepository.AddAsync:
// userId: userProvider.UserId  → Guid (не null, но может быть Guid.Empty)
// userName: userProvider.UserFullName → string (может быть null/empty)
```

---

## Интеграция с Entity Interfaces

Аудиторские интерфейсы сущностей определены в `Shared.Domain.Core.Interfaces` и подробно описаны в [entity-interfaces.md](entity-interfaces.md). Ниже — сводка взаимодействия с `IUserProvider`:

| Интерфейс | Свойство | Заполняется через | Когда |
|-----------|----------|-------------------|-------|
| `IWithCreated` | `CreatedByUserId` | `userProvider.UserId` | `EfRepository.AddAsync` |
| `IWithCreated` | `CreatedByUserName` | `userProvider.UserFullName` | `EfRepository.AddAsync` |
| `IWithUpdated` | `UpdatedByUserId` | `userProvider.UserId` | `UpdateCommandHandler` |
| `IWithDeleted` | `DeletedByUserId` | `userProvider.UserId` | `EfRepository.RemoveAsync` (soft) |

### Пример сущности с полной аудиторской информацией

```csharp
public class Order : IEntity<Guid>, IWithCreated, IWithUpdated, IWithDeleted
{
    public Guid Id { get; set; }

    // IWithDateCreated (унаследовано от IWithCreated)
    public DateTime DateCreated { get; private set; }

    // IWithCreated
    public Guid? CreatedByUserId { get; private set; }
    public string? CreatedByUserName { get; private set; }

    // IWithDateUpdated (унаследовано от IWithUpdated)
    public DateTime? DateUpdated { get; private set; }

    // IWithUpdated
    public Guid? UpdatedByUserId { get; private set; }

    // IWithDateDeleted (унаследовано от IWithDeleted)
    public DateTime? DateDeleted { get; private set; }

    // IWithDeleted
    public Guid? DeletedByUserId { get; private set; }
    public bool IsDeleted { get; private set; }

    public void OnCreate(Guid? userId, string? userName)
    {
        SetDateCreated(DateTime.UtcNow);
        SetCreatedByUserId(userId);
        SetCreatedByUserName(userName);
    }

    public void OnUpdate(Guid? userId)
    {
        SetDateUpdated(DateTime.UtcNow);
        SetUpdatedByUserId(userId);
    }

    public void OnDelete(Guid? userId)
    {
        SetDateDeleted(DateTime.UtcNow);
        SetDeletedByUserId(userId);
        SetIsDeleted();
    }

    // ... setter-методы
}
```

---

## Тестирование без аутентификации

В юнит-тестах можно использовать mock-реализацию `IUserProvider`:

```csharp
using Moq;
using Shared.Application.Core.Auth;

public class TestUserProvider : IUserProvider
{
    public Guid UserId { get; set; } = Guid.Parse("11111111-2222-3333-4444-555555555555");
    public string UserFullName { get; set; } = "Test User";
}

// Или через Moq:
var mockUserProvider = new Mock<IUserProvider>();
mockUserProvider.Setup(x => x.UserId).Returns(Guid.Parse("11111111-2222-3333-4444-555555555555"));
mockUserProvider.Setup(x => x.UserFullName).Returns("Test User");
```

### Интеграционные тесты с WebApplicationFactory

```csharp
builder.Services.AddScoped<IUserProvider>(_ => new TestUserProvider
{
    UserId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
    UserFullName = "Integration Test User"
});
```

---

## См. также

| Документ | Описание |
|----------|----------|
| [Entity Interfaces](entity-interfaces.md) | Интерфейсы `IWithCreated`, `IWithUpdated`, `IWithDeleted` и их реализация |
| [EF Core Internals](efcore-internals.md) | Внутренние механизмы EfRepository и DbContext |
| [Controllers](controllers.md) | Контроллеры — точка входа HTTP-запросов |
| [Correlation ID](correlation-id.md) | Идентификация корреляции запросов |
| [Domain Modeling](domain-modeling.md) | Моделирование домена: сущности, value objects, перечисления |