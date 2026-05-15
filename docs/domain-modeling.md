# Domain Modeling — Слой Domain

> **Assembly:** `Shared.Domain.Core.dll`
> **Namespace:** `Shared.Domain.Core.Base`, `Shared.Domain.Core.Exceptions.Models`, `Shared.Domain.Core.Attributes`

---

## 1. Обзор

Domain-слой — это **ядро** Clean Architecture. Он содержит бизнес-сущности, value objects, доменные события и бизнес-исключения. Не зависит от внешних библиотек, фреймворков или infrastructure-концернов.

### Принципы

| Принцип | Описание |
|---------|----------|
| **Rich Domain Model** | Сущности содержат поведение, не только данные |
| **Dependency Rule** | Domain не зависит ни от чего внешнего |
| **Immutability** | Value Objects неизменяемы |
| **Domain Events** | Коммуникация между агрегатами через события |

### Структура слоя

```
Shared.Domain.Core/
├── Base/                    ← BaseEntity, EntityWithMetadata
├── Interfaces/              ← IEntity, IWithDomainEvents, IEntityWithMetadata
├── Enums/                   ← DomainEventType
├── Event/                   ← IDomainEvent и реализации
├── Exceptions/              ← AppException, BusinessLogicException, NotFoundException
├── Attributes/              ← EntityName, EntityComparableName
└── ValueObjects/            ← Неизменяемые объекты-значения
```

---

## 2. BaseEntity

### 2.1. Иерархия интерфейсов

```
IEntity
  └── IEntity<TKey>
        └── IEntityWithMetadata<TKey>
              ├── IWithCreated (CreatedByUserId, CreatedByUserName, DateCreated)
              ├── IWithUpdated (UpdatedByUserId, DateUpdated)
              └── IWithDeleted (DeletedByUserId, DateDeleted, IsDeleted)

IWithDomainEvents
  ├── TryGetEvent()
  ├── ResetEvents()
  ├── GetAllKeys()
  ├── ProcessDomainEventAsync()
  └── ProcessDomainEventsAsync()
```

### 2.2. BaseEntity<TKey>

**Assembly:** `Shared.Domain.Core.dll`
**Namespace:** `Shared.Domain.Core.Base`

```csharp
public abstract class BaseEntity<TKey> : IEntity<TKey>, IWithDomainEvents
{
    /// <summary>Идентификатор сущности.</summary>
    public virtual TKey Id { get; set; } = default!;

    /// <summary>Имена navigation-свойств, необходимых для сохранения.</summary>
    public virtual string[] RequiredToSaveNavigationPropertiesNames => [];

    /// <summary>События, выполняемые ПЕРЕД сохранением в БД.</summary>
    protected virtual IDomainEvent[] BeforeSaveEvents => [];

    /// <summary>События, выполняемые ПОСЛЕ сохранения в БД.</summary>
    protected virtual IDomainEvent[] AfterSaveEvents => [];

    // Domain events management
    public bool TryGetEvent(DomainEventType domainEventType, Enum key, out IDomainEvent domainEvent);
    public void ResetEvents();
    public ICollection<Enum> GetAllKeys(DomainEventType domainEventType);
    public void DisableDomainEvents();
    public void DisableDomainEvents(DomainEventType domainEventType, Enum? flags = default);
    public void EnableDomainEvents();
    public void EnableDomainEvents(DomainEventType domainEventType, Enum? flags = default);
}
```

### 2.3. Domain Events Lifecycle

Доменные события разделены на два типа по моменту выполнения:

```csharp
public enum DomainEventType
{
    BeforeSave,  // Выполняются ДО SaveChanges()
    AfterSave,   // Выполняются ПОСЛЕ SaveChanges()
}
```

**Пример использования:**

```csharp
public class OrderCreatedEvent : IDomainEvent
{
    public Enum Key => OrderEvents.Created;
    public bool IsEnabled { get; private set; } = true;

    public void Enable() => IsEnabled = true;
    public void Disable() => IsEnabled = false;

    public async Task ProcessAsync(
        DomainEventType eventType,
        IServiceProvider serviceProvider,
        ICollection<IWithDomainEvents> entities,
        CancellationToken cancellationToken)
    {
        var publisher = serviceProvider.GetRequiredService<IDomainEventPublisher>();
        await publisher.PublishAsync(this, cancellationToken);
    }
}
```

```csharp
public class Order : BaseEntity<int>
{
    public string Number { get; private set; } = null!;
    public OrderStatus Status { get; private set; }

    protected override IDomainEvent[] BeforeSaveEvents =>
    [
        new OrderValidatedEvent(),  // Валидация перед сохранением
    ];

    protected override IDomainEvent[] AfterSaveEvents =>
    [
        new OrderCreatedEvent(),    // Уведомление после сохранения
        new OrderStatusChangedEvent(),
    ];

    public static Order Create(string number)
    {
        var order = new Order { Number = number, Status = OrderStatus.Created };
        // События сгенерируются автоматически при сохранении
        return order;
    }
}
```

### 2.4. Управление событиями

#### Включение/отключение по флагам

```csharp
// Отключить ВСЕ события
order.DisableDomainEvents();

// Отключить только BeforeSave-события
order.DisableDomainEvents(DomainEventType.BeforeSave);

// Отключить конкретные флаги (flag-based filtering)
order.DisableDomainEvents(DomainEventType.AfterSave, OrderEvents.StatusChanged);

// Включить обратно
order.EnableDomainEvents();
order.EnableDomainEvents(DomainEventType.AfterSave, OrderEvents.StatusChanged);
```

#### TryGetEvent — безопасное получение

```csharp
if (order.TryGetEvent(DomainEventType.AfterSave, OrderEvents.Created, out var evt))
{
    await evt.ProcessAsync(DomainEventType.AfterSave, serviceProvider, [order], ct);
}
```

#### ResetEvents — переинициализация

```csharp
// Переинициализирует события (вызывает EnableDomainEvents())
order.ResetEvents();
```

### 2.5. IWithDomainEvents — обработка событий

Интерфейс предоставляет методы для пакетной обработки:

```csharp
// Обработать одно событие
await entity.ProcessDomainEventAsync(
    DomainEventType.AfterSave,
    OrderEvents.Created,
    serviceProvider,
    entities: [order],
    cancellationToken);

// Обработать ВСЕ события указанного типа
await entity.ProcessDomainEventsAsync(
    DomainEventType.AfterSave,
    serviceProvider,
    entities: [order],
    cancellationToken);
```

---

## 3. EntityWithMetadata

### 3.1. Обзор

**Assembly:** `Shared.Domain.Core.dll`
**Namespace:** `Shared.Domain.Core.Base`

Автоматическое отслеживание аудита: кто создал, обновил, удалил сущность и когда.

```csharp
public abstract class EntityWithMetadata<TEntity, TKey>
    : BaseEntity<TKey>, IEntityWithMetadata, IWithDeleteAction<TEntity>
    where TEntity : class, IEntity<TKey>
{
    // Audit-поля
    public Guid? CreatedByUserId { get; protected set; }
    public string? CreatedByUserName { get; protected set; }
    public DateTime DateCreated { get; protected set; }
    public Guid? UpdatedByUserId { get; protected set; }
    public DateTime? DateUpdated { get; protected set; }
    public Guid? DeletedByUserId { get; protected set; }
    public DateTime? DateDeleted { get; protected set; }
    public bool IsDeleted { get; protected set; }

    // Lifecycle hooks
    public virtual void OnCreate(Guid? userId, string? userName);
    public virtual void OnUpdate(Guid? userId);
    public virtual void OnDelete(Guid? userId);

    // Soft delete
    public virtual Task DeleteAsync(IUnitOfWork unitOfWork, bool soft = true);
    public virtual Task DeleteAsync(IRepository<TEntity> repository, bool soft = true);
}
```

### 3.2. Audit-поля

| Поле | Тип | Когда заполняется |
|------|-----|-------------------|
| `CreatedByUserId` | `Guid?` | При создании (`OnCreate`) |
| `CreatedByUserName` | `string?` | При создании (`OnCreate`) |
| `DateCreated` | `DateTime` | При создании (`OnCreate`) — `DateTimeOffset.UtcNow.DateTime` |
| `UpdatedByUserId` | `Guid?` | При обновлении (`OnUpdate`) |
| `DateUpdated` | `DateTime?` | При обновлении (`OnUpdate`) — `DateTimeOffset.UtcNow.DateTime` |
| `DeletedByUserId` | `Guid?` | При удалении (`OnDelete`) |
| `DateDeleted` | `DateTime?` | При удалении (`OnDelete`) — `DateTimeOffset.UtcNow.DateTime` |
| `IsDeleted` | `bool` | При удалении (`SetIsDeleted()`) |

### 3.3. Защита от изменения автора

```csharp
public virtual void SetCreatedByUserId(Guid? createdByUserId)
{
    if (CreatedByUserId == createdByUserId) return;

    // ЗАПРЕЩЕНО изменять автора после создания
    if (CreatedByUserId.HasValue)
    {
        throw new BusinessLogicException("Запрещено изменять автора сущности.");
    }

    CreatedByUserId = createdByUserId;
}
```

### 3.4. Soft Delete

```csharp
public virtual void OnDelete(Guid? userId)
{
    SetDeletedByUserId(userId);
    SetDateDeleted(DateTimeOffset.UtcNow.DateTime);
}

public virtual Task DeleteAsync(IUnitOfWork unitOfWork, bool soft = true)
{
    SetIsDeleted();  // Помечает как удалённую
    return unitOfWork.GetRepository<TEntity>()
        .RemoveRangeAsync(x => x.Id!.Equals(Id), !soft);
}
```

При `soft = true` (по умолчанию) — сущность помечается `IsDeleted = true`, но не удаляется из БД.

### 3.5. Пример сущности с метаданными

```csharp
[EntityName("Персона")]
[EntityComparableName("ФИО")]
public class Person : EntityWithMetadata<Person, int>
{
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string? MiddleName { get; private set; }
    public DateTime BirthDate { get; private set; }

    public string FullName => $"{LastName} {FirstName} {MiddleName}".Trim();

    protected override IDomainEvent[] AfterSaveEvents =>
    [
        new PersonCreatedEvent(),
    ];

    public static Person Create(string firstName, string lastName, DateTime birthDate)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new BusinessLogicException("Имя не может быть пустым.");

        return new Person
        {
            FirstName = firstName,
            LastName = lastName,
            BirthDate = birthDate,
        };
    }

    public void UpdateName(string firstName, string lastName, string? middleName)
    {
        FirstName = firstName;
        LastName = lastName;
        MiddleName = middleName;
        // OnUpdate() вызовется автоматически из EfRepository
    }
}
```

---

## 4. Exception Hierarchy

### 4.1. Диаграмма наследования

```
Exception
  └── AppException (abstract)
        ├── IWithAdditionalData
        │     └── IReadOnlyDictionary<string, object>? AdditionalData { get; }
        │
        ├── BusinessLogicException
        │     └── HTTP 422 Unprocessable Entity
        │
        ├── NotFoundException
        │     └── HTTP 404 Not Found
        │     └── Конструкторы с MemberInfo + key(s)
        │
        └── ProxiedException (в Shared.Application.Core)
              └── HTTP-ошибки от upstream-сервисов
              └── ProblemDetails + StatusCode + TryGetAdditionalData<T>()
```

### 4.2. AppException

**Assembly:** `Shared.Domain.Core.dll`
**Namespace:** `Shared.Domain.Core.Exceptions.Models.Base`

Базовый класс для всех application-исключений:

```csharp
public abstract class AppException : Exception, IWithAdditionalData
{
    /// <summary>Дополнительная информация для потребителей API.</summary>
    public IReadOnlyDictionary<string, object>? AdditionalData { get; init; }

    protected AppException();
    protected AppException(string message, IReadOnlyDictionary<string, object>? additionalData = null);
    protected AppException(string message, Exception innerException, IReadOnlyDictionary<string, object>? additionalData = null);
}
```

**Валидация AdditionalData:**

```csharp
private static void ValidateAdditionalData(IReadOnlyDictionary<string, object>? additionalData)
{
    if (additionalData?.Any(kvp => string.IsNullOrEmpty(kvp.Key)) is true)
    {
        throw new ArgumentException("Ключи не могут быть null или пустыми", nameof(additionalData));
    }
}
```

### 4.3. BusinessLogicException

**Assembly:** `Shared.Domain.Core.dll`
**Namespace:** `Shared.Domain.Core.Exceptions.Models`

Нарушение бизнес-правил. Маппится на **HTTP 422 Unprocessable Entity**.

```csharp
public class BusinessLogicException : AppException
{
    public BusinessLogicException();
    public BusinessLogicException(string message, IReadOnlyDictionary<string, object>? additionalData = null);
    public BusinessLogicException(string message, Exception innerException, IReadOnlyDictionary<string, object>? additionalData = null);
}
```

**Когда использовать:**

| Сценарий | Пример |
|----------|--------|
| Валидация бизнес-правил | `throw new BusinessLogicException("Заказ уже оплачен.")` |
| Нарушение инвариантов | `throw new BusinessLogicException("Сумма заказа не может быть отрицательной.")` |
| Защита от изменения | `throw new BusinessLogicException("Запрещено изменять автора сущности.")` |
| Недостаточно прав | `throw new BusinessLogicException("Недостаточно прав для удаления.")` |

**Пример с AdditionalData:**

```csharp
throw new BusinessLogicException(
    "Недостаточно товара на складе.",
    new Dictionary<string, object>
    {
        ["productId"] = 42,
        ["requestedQuantity"] = 100,
        ["availableQuantity"] = 15,
    });
```

### 4.4. NotFoundException

**Assembly:** `Shared.Domain.Core.dll`
**Namespace:** `Shared.Domain.Core.Exceptions.Models`

Сущность не найдена. Маппится на **HTTP 404 Not Found**.

```csharp
public class NotFoundException : AppException
{
    // Стандартный конструктор
    public NotFoundException(string message, IReadOnlyDictionary<string, object>? additionalData = null);

    // По типу сущности + ключ (использует [EntityName])
    public NotFoundException(MemberInfo entityType, object key, IReadOnlyDictionary<string, object>? additionalData = null);

    // По типу сущности + несколько ключей
    public NotFoundException(MemberInfo entityType, object[] keys, IReadOnlyDictionary<string, object>? additionalData = null);

    // С inner exception
    public NotFoundException(string message, Exception innerException, IReadOnlyDictionary<string, object>? additionalData = null);
}
```

**Автоматическое формирование сообщения через `[EntityName]`:**

```csharp
// Без атрибута:
throw new NotFoundException(typeof(Person), 42);
// → "Сущность \"Person\" не была найдена. Ключ: 42"

// С атрибутом [EntityName("Персона")]:
throw new NotFoundException(typeof(Person), 42);
// → "Сущность \"Персона\" не была найдена. Ключ: 42"

// Несколько ключей:
throw new NotFoundException(typeof(Person), [42, 43, 44]);
// → "Сущности \"Персона\" не были найдены. Ключи: 42, 43, 44"
```

### 4.5. ClientRequestContext

**Assembly:** `Shared.Domain.Core.dll`
**Namespace:** `Shared.Domain.Core.Exceptions.Models`

Контекст запроса для диагностики:

```csharp
public class ClientRequestContext
{
    public string ClientName { get; }      // Имя клиента
    public string AbsolutePath { get; }    // Абсолютный путь URL

    public ClientRequestContext(string clientName, string absolutePath);
}
```

Используется в `ProxiedResponseValidator` для логирования ошибок при service-to-service вызовах.

---

## 5. Attributes

### 5.1. [EntityName]

**Assembly:** `Shared.Domain.Core.dll`
**Namespace:** `Shared.Domain.Core.Attributes`

Человекочитаемое название сущности для отображения в сообщениях об ошибках, UI и логах:

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class EntityNameAttribute(string name) : Attribute
{
    public string Name => name;
}
```

**Пример:**

```csharp
[EntityName("Персона")]
public class Person : EntityWithMetadata<Person, int> { ... }

[EntityName("Заказ")]
public class Order : EntityWithMetadata<Order, int> { ... }
```

**Использование в NotFoundException:**

```csharp
// Автоматически читает [EntityName] через reflection
throw new NotFoundException(typeof(Person), 42);
// Сообщение: "Сущность \"Персона\" не была найдена. Ключ: 42"
```

### 5.2. [EntityComparableName]

**Assembly:** `Shared.Domain.Core.dll`
**Namespace:** `Shared.Domain.Core.Attributes`

Название параметра сущности по умолчанию для сравнения и сортировки:

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class EntityComparableNameAttribute(string name) : Attribute
{
    public string Name => name;

    public const string DefaultComparableParameterName = "наименованием";
}
```

**Пример:**

```csharp
[EntityComparableName("ФИО")]
public class Person : EntityWithMetadata<Person, int>
{
    public string FullName => $"{LastName} {FirstName}".Trim();
}
```

Используется в фильтрации и сортировке при динамическом построении запросов:

```csharp
// При сортировке по умолчанию используется параметр из [EntityComparableName]
// → "Сортировка по ФИО"
```

---

## 6. Integration with EfRepository

### 6.1. Автоматическое заполнение audit-полей

EfRepository автоматически вызывает lifecycle-методы при сохранении:

```csharp
// При добавлении новой сущности
repository.AddAsync(person);
// → person.OnCreate(userId, userName)  — заполняет CreatedBy*, DateCreated

// При обновлении
repository.UpdateAsync(person);
// → person.OnUpdate(userId)  — заполняет UpdatedBy*, DateUpdated

// При удалении
repository.RemoveAsync(person, soft: true);
// → person.OnDelete(userId)  — заполняет DeletedBy*, DateDeleted, IsDeleted
```

### 6.2. Обработка Domain Events

EfRepository обрабатывает доменные события в два этапа:

```csharp
// 1. ДО SaveChanges()
await ProcessDomainEventsAsync(DomainEventType.BeforeSave, serviceProvider, entities);

// 2. SaveChanges()
await _context.SaveChangesAsync(cancellationToken);

// 3. ПОСЛЕ SaveChanges()
await ProcessDomainEventsAsync(DomainEventType.AfterSave, serviceProvider, entities);
```

### 6.3. RequiredToSaveNavigationPropertiesNames

Свойство указывает, какие navigation-свойства должны быть загружены перед сохранением:

```csharp
public class Order : BaseEntity<int>
{
    public OrderItem[] Items { get; private set; } = [];

    public override string[] RequiredToSaveNavigationPropertiesNames =>
    [
        nameof(Items),  // Items должны быть загружены перед сохранением
    ];
}
```

EfRepository проверяет это свойство и загружает указанные navigation-свойства если они ещё не загружены.

---

## 7. Примеры использования

### 7.1. Создание сущности с валидацией

```csharp
public class Person : EntityWithMetadata<Person, int>
{
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public DateTime BirthDate { get; private set; }

    public static Person Create(string firstName, string lastName, DateTime birthDate)
    {
        // Бизнес-валидация в фабричном методе
        if (string.IsNullOrWhiteSpace(firstName))
            throw new BusinessLogicException("Имя не может быть пустым.");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new BusinessLogicException("Фамилия не может быть пустой.");

        if (birthDate > DateTime.UtcNow)
            throw new BusinessLogicException(
                "Дата рождения не может быть в будущем.",
                new Dictionary<string, object> { ["birthDate"] = birthDate });

        return new Person
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            BirthDate = birthDate,
        };
    }
}
```

### 7.2. Domain Event в сущности

```csharp
public enum PersonEvents
{
    Created,
    NameChanged,
    Deleted,
}

public class PersonNameChangedEvent : IDomainEvent
{
    public Enum Key => PersonEvents.NameChanged;
    public bool IsEnabled { get; private set; } = true;
    public int PersonId { get; }
    public string OldName { get; }
    public string NewName { get; }

    public PersonNameChangedEvent(int personId, string oldName, string newName)
    {
        PersonId = personId;
        OldName = oldName;
        NewName = newName;
    }

    public void Enable() => IsEnabled = true;
    public void Disable() => IsEnabled = false;

    public async Task ProcessAsync(
        DomainEventType eventType,
        IServiceProvider serviceProvider,
        ICollection<IWithDomainEvents> entities,
        CancellationToken cancellationToken)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<PersonNameChangedEvent>>();
        logger.LogInformation("Person {PersonId} name changed: {Old} → {New}",
            PersonId, OldName, NewName);
    }
}
```

```csharp
public class Person : EntityWithMetadata<Person, int>
{
    private string _firstName = null!;
    private string _lastName = null!;

    protected override IDomainEvent[] AfterSaveEvents =>
    [
        new PersonCreatedEvent(),
        new PersonNameChangedEvent(0, string.Empty, string.Empty), // Placeholder
    ];

    public void ChangeName(string firstName, string lastName)
    {
        var oldName = FullName;
        _firstName = firstName;
        _lastName = lastName;

        // Обновляем событие с актуальными данными
        if (TryGetEvent(DomainEventType.AfterSave, PersonEvents.NameChanged, out var evt)
            && evt is PersonNameChangedEvent nameChangedEvent)
        {
            // Обновляем данные события
            // (в реальной реализации событие пересоздаётся)
        }
    }
}
```

### 7.3. Обработка в Command Handler

```csharp
public class CreatePersonHandler : IRequestHandler<CreatePersonCommand, Result<int>>
{
    private readonly IRepository<Person> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePersonHandler(IRepository<Person> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(
        CreatePersonCommand command,
        CancellationToken cancellationToken)
    {
        // Валидация на уровне домена
        var person = Person.Create(command.FirstName, command.LastName, command.BirthDate);

        // Audit-поля заполнятся автоматически при сохранении
        await _repository.AddAsync(person, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // AfterSave-события обработаются автоматически
        return Result.Success(person.Id);
    }
}
```

---

---

## См. также

| Документ | Описание |
|----------|----------|
| [Entity Interfaces](entity-interfaces.md) | Интерфейсы сущностей (IEntity, IEntityWithMetadata) |
| [Domain Events](domain-events.md) | Доменные события и их обработка |
| [Exception Mapping](exception-mapping.md) | Маппинг исключений в HTTP-статусы |
| [EF Core Internals](efcore-internals.md) | Внутреннее устройство EfRepository |
