# Entity Interfaces

**Assembly:** `Shared.Domain.Core.dll`  
**Namespace:** `Shared.Domain.Core.Interfaces`  
**Исходники:** `src/Shared/Core/Shared.Domain.Core/Interfaces/`

---

## Обзор

Entity Interfaces в фреймворке Shared определяют контракты для сущностей доменного слоя. Они обеспечивают:

- **Единообразие** — все сущности следуют единому контракту
- **Audit Trail** — автоматическое отслеживание создания, обновления, удаления
- **Soft Delete** — поддержка мягкого удаления без потери данных
- **Lifecycle Actions** — встроенная поддержка действий перехвата
- **Auto-population** — EfRepository автоматически заполняет аудиторские поля через `IUserProvider`

Эти интерфейсы не содержат реализации — они лишь определяют контракт. Реализация остаётся на стороне сущности.

---

## Базовые интерфейсы сущностей

### IEntity

Базовый контракт для любой сущности с идентификатором.

```csharp
public interface IEntity
{
    object Id { get; }
}
```

### IEntity<T>

Типизированная версия с генерик-параметром для ключа.

```csharp
public interface IEntity<out T> : IEntity
{
    object IEntity.Id => Id;
    new T Id { get; }
}
```

**Пример:**
```csharp
public class Person : IEntity<Guid>
{
    public Guid Id { get; set; }
}
```

---

## Audit Interfaces

Интерфейсы для отслеживания жизненного цикла сущности. EfRepository автоматически заполняет эти поля при операциях `AddAsync` и `RemoveAsync` (soft-delete), а также при изменении tracked-сущности с последующим `SaveChangesAsync` (через `IBeforeSaveChangesService`), используя `IUserProvider` для получения контекста текущего пользователя. Метода `IRepository.UpdateAsync` не существует — обновление идёт через `ChangeTracker` либо через DB-level `ExecuteUpdateRangeAsync`.

| Интерфейс | Свойство | Методы | Авто-заполняется при |
|-----------|----------|--------|---------------------|
| `IWithDateCreated` | `DateTime DateCreated` | `SetDateCreated(DateTime)` | `AddAsync` |
| `IWithCreated` | `Guid? CreatedByUserId`<br>`string? CreatedByUserName` | `SetCreatedByUserId(Guid?)`<br>`SetCreatedByUserName(string?)`<br>`OnCreate()` | `AddAsync` |
| `IWithDateUpdated` | `DateTime? DateUpdated` | `SetDateUpdated(DateTime?)` | `SaveChangesAsync` (tracked, через `IBeforeSaveChangesService`) |
| `IWithUpdated` | `Guid? UpdatedByUserId` | `SetUpdatedByUserId(Guid?)`<br>`OnUpdate()` | `SaveChangesAsync` (tracked, через `IBeforeSaveChangesService`) |
| `IWithDateDeleted` | `DateTime? DateDeleted` | `SetDateDeleted(DateTime?)` | `RemoveAsync` (soft) |
| `IWithDeleted` | `Guid? DeletedByUserId`<br>`bool IsDeleted` | `SetDeletedByUserId(Guid?)`<br>`OnDelete()`<br>`SetIsDeleted()` | `RemoveAsync` (soft) |

### Иерархия наследования

```
IWithDateCreated
    ↑
IWithCreated

IWithDateUpdated
    ↑
IWithUpdated

IWithDateDeleted
    ↑
IWithDeleted
```

### Пример реализации

```csharp
public class Order : IEntity<Guid>, IWithCreated, IWithUpdated, IWithDeleted
{
    public Guid Id { get; set; }

    // IWithDateCreated
    public DateTime DateCreated { get; private set; }
    public void SetDateCreated(DateTime dateCreated) => DateCreated = dateCreated;

    // IWithCreated
    public Guid? CreatedByUserId { get; private set; }
    public string? CreatedByUserName { get; private set; }
    public void SetCreatedByUserId(Guid? userId) => CreatedByUserId = userId;
    public void SetCreatedByUserName(string userName) => CreatedByUserName = userName;
    public void OnCreate(Guid? userId, string? userName)
    {
        SetDateCreated(DateTime.UtcNow);
        SetCreatedByUserId(userId);
        SetCreatedByUserName(userName);
    }

    // IWithDateUpdated
    public DateTime? DateUpdated { get; private set; }
    public void SetDateUpdated(DateTime? dateUpdated) => DateUpdated = dateUpdated;

    // IWithUpdated
    public Guid? UpdatedByUserId { get; private set; }
    public void SetUpdatedByUserId(Guid? userId) => UpdatedByUserId = userId;
    public void OnUpdate(Guid? userId)
    {
        SetDateUpdated(DateTime.UtcNow);
        SetUpdatedByUserId(userId);
    }

    // IWithDateDeleted
    public DateTime? DateDeleted { get; private set; }
    public void SetDateDeleted(DateTime? dateDeleted) => DateDeleted = dateDeleted;

    // IWithDeleted
    public Guid? DeletedByUserId { get; private set; }
    public bool IsDeleted { get; private set; }
    public void SetDeletedByUserId(Guid? userId) => DeletedByUserId = userId;
    public void OnDelete(Guid? userId)
    {
        SetDateDeleted(DateTime.UtcNow);
        SetDeletedByUserId(userId);
        SetIsDeleted();
    }
    public void SetIsDeleted() => IsDeleted = true;
}
```

### Auto-population в EfRepository

EfRepository автоматически вызывает методы `OnCreate`, `OnUpdate`, `OnDelete` при операциях с сущностью:

```csharp
// Внутри EfRepository.AddAsync():
if (entity is IWithCreated withCreated)
{
    withCreated.OnCreate(_userProvider.UserId, _userProvider.UserFullName);
}

// В IBeforeSaveChangesService (вызывается из SaveChangesAsync для tracked-сущности):
if (entity is IWithUpdated withUpdated)
{
    withUpdated.OnUpdate(_userProvider.UserId);
}

// Внутри EfRepository.RemoveAsync() (soft delete):
if (entity is IWithDeleted withDeleted && !hard)
{
    withDeleted.OnDelete(_userProvider.UserId);
}
```

---

## Composite Metadata Interfaces

### IEntityWithMetadata

Композитный интерфейс, объединяющий все audit-интерфейсы.

```csharp
public interface IEntityWithMetadata : IEntity, IWithCreated, IWithUpdated, IWithDeleted;

public interface IEntityWithMetadata<out T> : IEntity<T>, IEntityWithMetadata
    where T : struct;
```

**Пример:**
```csharp
public class Document : IEntityWithMetadata<Guid>
{
    public Guid Id { get; set; }
    public DateTime DateCreated { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public string? CreatedByUserName { get; private set; }
    public DateTime? DateUpdated { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public DateTime? DateDeleted { get; private set; }
    public Guid? DeletedByUserId { get; private set; }

    // ... реализация методов Set*/On*
}
```

### IEntityWithUserData

Интерфейс для хранения имён пользователей (без ID) — используется в DTO для отображения.

```csharp
public interface IEntityWithUserData
{
    string? CreatedByUserName { get; }
    string? UpdatedByUserName { get; }
    void SetCreatedByUserName(string name);
    void SetUpdatedByUserName(string name);
}
```

---

## Soft Delete

### IWithDeleteAction<TEntity>

Интерфейс для сущностей, поддерживающих удаление (soft/hard) через UnitOfWork или Repository.

```csharp
public interface IWithDeleteAction<TEntity>
    where TEntity : class, IEntity
{
    Task DeleteAsync(IUnitOfWork unitOfWork, bool soft = true);
    Task DeleteAsync(IRepository<TEntity> repository, bool soft = true);
}
```

**Пример использования:**
```csharp
public class Order : IEntity<Guid>, IWithDeleteAction<Order>
{
    public Guid Id { get; set; }
    public bool IsDeleted { get; private set; }

    public async Task DeleteAsync(IUnitOfWork unitOfWork, bool soft = true)
    {
        var repository = unitOfWork.GetRepository<Order>();
        var entity = await repository.GetAsync(Id);
        if (entity != null)
        {
            await repository.RemoveAsync(entity, hard: !soft);
        }
    }

    public async Task DeleteAsync(IRepository<Order> repository, bool soft = true)
    {
        var entity = await repository.GetAsync(Id);
        if (entity != null)
        {
            await repository.RemoveAsync(entity, hard: !soft);
        }
    }
}
```

**Вызов:**
```csharp
// Soft delete
await order.DeleteAsync(unitOfWork, soft: true);

// Hard delete
await order.DeleteAsync(repository, soft: false);
```

---

## Additional Data

### IWithAdditionalData

Интерфейс для передачи произвольных данных через HTTP-ответы, доступных только бэкенд-потребителям (не передаются фронтенду).

```csharp
public interface IWithAdditionalData
{
    IReadOnlyDictionary<string, object>? AdditionalData { get; }
}
```

**Пример использования:**
```csharp
// Интерфейс IWithAdditionalData предоставляет только get; set добавляется в DTO явно.
public class OrderResponse : IWithAdditionalData
{
    public Guid Id { get; set; }
    public string Status { get; set; }

    // Дополнительные данные для внутренних потребителей API
    // Реализация интерфейса — get; собственный setter нужен для инициализации через object initializer.
    public IReadOnlyDictionary<string, object>? AdditionalData { get; set; } = null!;
}

// В handler:
var response = new OrderResponse
{
    Id = order.Id,
    Status = order.Status,
    AdditionalData = new Dictionary<string, object>
    {
        ["InternalProcessingTime"] = processingTimeMs,
        ["CacheHit"] = false
    }
};
```

---

## Сводная таблица интерфейсов

| Интерфейс | Назначение | Ключевые свойства |
|-----------|-----------|-------------------|
| `IEntity` | Базовый контракт сущности | `object Id` |
| `IEntity<T>` | Типизированный ключ | `T Id` |
| `IWithDateCreated` | Дата создания | `DateTime DateCreated` |
| `IWithCreated` | Автор создания | `Guid? CreatedByUserId`, `string? CreatedByUserName` |
| `IWithDateUpdated` | Дата обновления | `DateTime? DateUpdated` |
| `IWithUpdated` | Автор обновления | `Guid? UpdatedByUserId` |
| `IWithDateDeleted` | Дата удаления | `DateTime? DateDeleted` |
| `IWithDeleted` | Автор удаления + флаг | `Guid? DeletedByUserId`, `bool IsDeleted` |
| `IWithDeleteAction<T>` | Действие удаления | `DeleteAsync()` |
| `IWithAdditionalData` | Дополнительные данные | `IReadOnlyDictionary<string, object>? AdditionalData` |
| `IEntityWithMetadata` | Композит audit | Наследует `IEntity` + `IWithCreated` + `IWithUpdated` + `IWithDeleted` |
| `IEntityWithUserData` | Имена пользователей | `CreatedByUserName`, `UpdatedByUserName` |

---

---

## См. также

| Документ | Описание |
|----------|----------|
| [Domain Modeling](domain-modeling.md) | Рекомендации по проектированию доменной модели |
| [EF Core Internals](efcore-internals.md) | Внутренние механизмы EfRepository, IUserProvider |
| [Repository Pattern](repository.md) | IRepository<T> — работа с данными |
