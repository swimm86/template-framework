# Domain Events

**Namespace:** `Shared.Domain.Core.Event`  
**Assembly:** `Shared.Domain.Core`

---

## Обзор

Domain Events — механизм trigger'ов side-эффектов на уровне Domain слоя. Сущности, реализующие `IWithDomainEvents`, регистрируют события, которые обрабатываются Unit of Work в процессе `SaveChanges` — до или после фактического сохранения в БД.

**Зачем это нужно:**
- Декомпозиция бизнес-логики без нарушения SRP
- Trigger'ование side-эффектов (отправка уведомлений, аудит, каскадные операции)
- Изоляция Domain слоя от Infrastructure concerns
- Контроль lifecycle: `BeforeSave` (валидация, обогащение данных) vs `AfterSave` (публикация, нотификация)

---

## Типы событий

### IDomainEvent

Базовый интерфейс всех доменных событий:

```csharp
public interface IDomainEvent
{
    Enum Key { get; }

    Task ProcessAsync(
        DomainEventType eventType,
        IServiceProvider serviceProvider,
        ICollection<IWithDomainEvents> entities,
        CancellationToken cancellationToken);

    void Enable();
    void Disable();
}
```

| Член | Описание |
|------|----------|
| `Key` | Уникальный идентификатор события (enum) |
| `ProcessAsync` | Выполняет логику события |
| `Enable()` | Включает событие для повторного срабатывания |
| `Disable()` | Отключает событие (автоматически после обработки) |

### CustomDomainEvent

Lambda-based событие — вся логика передаётся через `Func`:

```csharp
public class CustomDomainEvent(
    Enum key,
    Func<IServiceProvider, ICollection<IWithDomainEvents>, CancellationToken, Task> action)
    : EntityEventBase(key)
{
    protected override Task ProcessActionAsync(
        IServiceProvider serviceProvider,
        ICollection<IWithDomainEvents> entities,
        CancellationToken cancellationToken) =>
        action(serviceProvider, entities, cancellationToken);
}
```

**Когда использовать:** одноразовые события, inline-логика, динамическая регистрация.

### TypeDomainEvent

Type-based событие — аналогично `CustomDomainEvent`, но с явным полем `_action`:

```csharp
public class TypeDomainEvent : EntityEventBase
{
    private readonly Func<IServiceProvider, ICollection<IWithDomainEvents>, CancellationToken, Task> _action;

    public TypeDomainEvent(
        Enum key,
        Func<IServiceProvider, ICollection<IWithDomainEvents>, CancellationToken, Task> action)
        : base(key)
    {
        _action = action;
    }

    protected override Task ProcessActionAsync(
        IServiceProvider serviceProvider,
        ICollection<IWithDomainEvents> entities,
        CancellationToken cancellationToken)
        => _action(serviceProvider, entities, cancellationToken);
}
```

**Когда использовать:** события, привязанные к конкретному типу сущностей, где нужна передача коллекции entities.

### EntityDomainEvent

Упрощённое событие — не работает с коллекцией entities, не вызывает `DisableEntitiesEvents`:

```csharp
public class EntityDomainEvent : EntityEventBase
{
    private readonly Func<IServiceProvider, CancellationToken, Task> _action;

    public EntityDomainEvent(
        Enum key,
        Func<IServiceProvider, CancellationToken, Task> action)
        : base(key)
    {
        _action = action;
    }

    protected override Task ProcessActionAsync(
        IServiceProvider serviceProvider,
        ICollection<IWithDomainEvents> entities,
        CancellationToken cancellationToken)
        => _action(serviceProvider, cancellationToken);

    protected override void DisableEntitiesEvents(
        DomainEventType eventType,
        ICollection<IWithDomainEvents> entities)
    {
        // Пустая реализация — события других сущностей не отключаются
    }
}
```

**Когда использовать:** глобальные события, не требующие доступа к конкретным entities.

---

## Создание кастомных событий

### EntityEventBase — абстрактная основа

Все события наследуются от `EntityEventBase`:

```csharp
public abstract class EntityEventBase(Enum key) : IDomainEvent
{
    private bool _enabled = true;

    public Enum Key { get; } = key;

    public async Task ProcessAsync(
        DomainEventType eventType,
        IServiceProvider serviceProvider,
        ICollection<IWithDomainEvents> entities,
        CancellationToken cancellationToken)
    {
        if (_enabled)
            await ProcessActionAsync(serviceProvider, entities, cancellationToken);

        Disable();
        DisableEntitiesEvents(eventType, entities);
    }

    public void Enable() => _enabled = true;
    public void Disable() => _enabled = false;

    protected abstract Task ProcessActionAsync(
        IServiceProvider serviceProvider,
        ICollection<IWithDomainEvents> entities,
        CancellationToken cancellationToken);

    protected virtual void DisableEntitiesEvents(
        DomainEventType eventType,
        ICollection<IWithDomainEvents> entities) =>
        entities.ForEach(x =>
        {
            if (x.TryGetEvent(eventType, Key, out var domainEvent))
                domainEvent.Disable();
        });
}
```

**Ключевое поведение:**
1. `ProcessAsync` проверяет `_enabled`, вызывает `ProcessActionAsync`, затем `Disable()` + `DisableEntitiesEvents()`
2. `DisableEntitiesEvents` автоматически отключает одноимённые события у всех переданных entities — предотвращает повторную обработку
3. Override `DisableEntitiesEvents` для изменения этого поведения (как в `EntityDomainEvent`)

### Пример: кастомное событие

```csharp
// Enum для ключей событий
public enum OrderEvents
{
    OrderCreated,
    OrderStatusChanged,
    OrderShipped,
}

// Кастомное событие
public class OrderCreatedEvent : EntityEventBase
{
    public OrderCreatedEvent() : base(OrderEvents.OrderCreated) { }

    protected override async Task ProcessActionAsync(
        IServiceProvider serviceProvider,
        ICollection<IWithDomainEvents> entities,
        CancellationToken cancellationToken)
    {
        var emailService = serviceProvider.GetRequiredService<IEmailService>();

        foreach (var order in entities.OfType<Order>())
        {
            await emailService.SendOrderConfirmationAsync(order, cancellationToken);
        }
    }
}
```

---

## Event Settings — контроль срабатывания

Иерархия настроек позволяет тонко управлять тем, какие события срабатывают:

```
DomainEventSettings
└── TypeEventSettings (по типу сущности)
    └── EventTypeEventSettings (по DomainEventType: BeforeSave/AfterSave)
        └── Enum flags (конкретные event keys)
```

### DomainEventSettings

Корневая настройка — управляет событиями по типу сущности:

```csharp
public record DomainEventSettings(bool Enabled = true)
    : EventSettingsWithInternalSettingsBase<
        Type,
        TypeEventSettings,
        DomainEventType,
        Dictionary<DomainEventType, EventTypeEventSettings>>(Enabled)
{
    protected override TypeEventSettings CreateExceptItem(bool enabled) => new(enabled);

    public bool AnyElementEnabled(Type typeKey, DomainEventType eventTypeKey, Enum eventKey);
    public void Switch(Type typeKey, DomainEventType eventTypeKey, Enum eventKey, bool enabled);
}
```

### EventSettingBase<TItems, TKey>

Базовый класс настроек с паттерном "enabled + exceptions":

```csharp
public abstract record EventSettingBase<TItems, TKey>
{
    protected bool Enabled { get; private set; }
    protected abstract bool HasExceptItems { get; }
    protected TItems ExceptItems { get; set; }

    public bool AnyEnabled => Enabled || HasExceptItems;
    public bool AnyDisabled => !Enabled || HasExceptItems;

    public void Switch(bool enabled);
    public abstract void Switch(TKey itemKey, bool enabled);
    public abstract bool AnyElementEnabled(TKey itemKey);
}
```

**Логика:** если `Enabled = true`, все элементы включены кроме тех, что в `ExceptItems`. Если `Enabled = false`, все выключены кроме `ExceptItems`.

### EventTypeEventSettings

Настройка по `DomainEventType` с flag-based filtering:

```csharp
public record EventTypeEventSettings(bool Enabled = true)
    : EventSettingBase<Enum?, Enum>(Enabled)
{
    protected override Enum? GetDefaultItems => default;
    protected sealed override bool HasExceptItems => ExceptItems != null;

    public override bool AnyElementEnabled(Enum itemKey)
    {
        var hasFlag = ExceptItems?.HasFlag(itemKey) ?? false;
        return Enabled ? !hasFlag : hasFlag;
    }

    public override void Switch(Enum itemKey, bool enabled)
    {
        ExceptItems = Enabled == enabled
            ? ExceptItems?.Without(itemKey)
            : (ExceptItems?.With(itemKey) ?? itemKey);
    }
}
```

**Flag-based логика:** `ExceptItems` хранит enum flags. При `Enabled = true` элементы с флагом в `ExceptItems` отключены.

### TypeEventSettings

Настройка по типу сущности:

```csharp
public record TypeEventSettings(bool Enabled = true)
    : EventSettingsWithInternalSettingsBase<DomainEventType, EventTypeEventSettings, Enum, Enum?>(Enabled)
{
    protected override EventTypeEventSettings CreateExceptItem(bool enabled) => new(enabled);
}
```

### Пример использования в Unit of Work

```csharp
// Отключить все события
unitOfWork.DisableEvents();

// Отключить события для конкретного типа
unitOfWork.DisableEvents<Order>();

// Отключить BeforeSave-события для Order
unitOfWork.DisableEvents<Order>(DomainEventType.BeforeSave);

// Отключить конкретный event key
unitOfWork.DisableEvents<Order>(DomainEventType.AfterSave, OrderEvents.OrderCreated);

// Включить обратно
unitOfWork.EnableEvents();
unitOfWork.EnableEvents<Order>(DomainEventType.AfterSave, OrderEvents.OrderCreated);
```

---

## Интеграция с Unit of Work

### IWithDomainEvents

Интерфейс, который сущность должна реализовать для поддержки domain events:

```csharp
public interface IWithDomainEvents
{
    string[] RequiredToSaveNavigationPropertiesNames { get; }

    bool TryGetEvent(DomainEventType domainEventType, Enum key, out IDomainEvent domainEvent);

    void ResetEvents();

    ICollection<Enum> GetAllKeys(DomainEventType domainEventType);

    Task ProcessDomainEventAsync(
        DomainEventType eventType,
        Enum key,
        IServiceProvider serviceProvider,
        ICollection<IWithDomainEvents>? entities = default,
        CancellationToken cancellationToken = default);

    Task ProcessDomainEventsAsync(
        DomainEventType eventType,
        IServiceProvider serviceProvider,
        ICollection<IWithDomainEvents>? entities = default,
        CancellationToken cancellationToken = default);
}
```

| Член | Описание |
|------|----------|
| `RequiredToSaveNavigationPropertiesNames` | Имена navigation properties, которые нужно загрузить перед обработкой событий |
| `TryGetEvent` | Извлечение события по типу и ключу |
| `ResetEvents` | Сброс очередей событий к обязательным |
| `GetAllKeys` | Все ключи событий заданного типа |
| `ProcessDomainEventAsync` | Обработка одного события |
| `ProcessDomainEventsAsync` | Обработка всех событий заданного типа |

### DomainEventType

```csharp
public enum DomainEventType
{
    BeforeSave,  // До вызова SaveChangesAsync
    AfterSave,   // После вызова SaveChangesAsync
}
```

### Lifecycle в EfUnitOfWork

```csharp
public async Task<int> SaveChangesAsync(
    CancellationToken cancellationToken,
    bool commitTransaction = true,
    bool resetEventSettingsAfterSave = true)
{
    var entryTypeGroups = EntriesWithDomainEvents
        .GroupBy(e => e.Entity.GetType())
        .ToArray();

    try
    {
        // 1. BeforeSave события
        await ProcessDomainEventsAsync(entryTypeGroups, DomainEventType.BeforeSave, cancellationToken);

        // 2. Pre-save хуки (IBeforeSaveChangesService)
        await ProcessBeforeSaveChangesActionsAsync(cancellationToken);

        // 3. Фактическое сохранение
        var result = await DbContext.SaveChangesAsync(cancellationToken);

        if (commitTransaction)
            await CommitTransactionAsync(cancellationToken);

        // 4. AfterSave события
        await ProcessDomainEventsAsync(entryTypeGroups, DomainEventType.AfterSave, cancellationToken);

        return result;
    }
    catch
    {
        if (commitTransaction)
            await RollbackTransactionAsync(cancellationToken);
        throw;
    }
    finally
    {
        // 5. Сброс событий и настроек
        if (_domainEventSettings.AnyEnabled)
            entryTypeGroups.SelectMany(x => x).ForEach(e => e.Entity.ResetEvents());

        if (resetEventSettingsAfterSave)
            ResetEventSettings();

        if (commitTransaction)
            await ResetTransactionAsync(cancellationToken);
    }
}
```

**Порядок выполнения:**

```
BeforeSave Events → IBeforeSaveChangesService → SaveChangesAsync → Commit → AfterSave Events → Reset
```

### Автозагрузка Navigation Properties

EfUnitOfWork автоматически загружает navigation properties, указанные в `RequiredToSaveNavigationPropertiesNames`, если они ещё не загружены:

```csharp
private async Task IncludeRequiredNavigationPropertiesAsync(...)
{
    var navigationGroups = typeEntryGroup
        .SelectMany(entry => entry.Navigations
            .Where(nav =>
                entry.Entity.RequiredToSaveNavigationPropertiesNames.Contains(nav.Metadata.Name)
                && !nav.IsLoaded))
        .GroupBy(nav => nav.Metadata.Name)
        .ToArray();

    // Один запрос на каждое navigation property для всего типа
    await navigationGroups.ForeachAsync(async navigationGroup =>
    {
        var navigation = navigationGroup.First();
        await IncludeNavigationPropertyCollectionByTypeAsync(...);
        navigationGroup.ForEach(nav => nav.IsLoaded = true);
    }, cancellationToken);
}
```

---

## Best Practices

### Когда использовать Domain Events

| Сценарий | Recommendation |
|----------|---------------|
| Отправка email после создания заказа | ✅ `AfterSave` event |
| Валидация перед сохранением | ✅ `BeforeSave` event |
| Аудит изменений | ✅ `BeforeSave` event |
| Каскадное обновление связанных сущностей | ✅ `BeforeSave` event |
| Публикация интеграционных событий | ✅ `AfterSave` event |
| Простая CRUD-операция без side-эффектов | ❌ Не нужно |
| Синхронный вызов сервиса из handler'а | ❌ Лучше через MediatR |

### Правила

1. **BeforeSave** — для изменений, которые должны попасть в ту же транзакцию (валидация, обогащение, аудит)
2. **AfterSave** — для действий после коммита (нотификации, интеграционные события, кэш-инвалидация)
3. **Enable/Disable** — события автоматически отключаются после обработки; `Enable()` нужен для повторного срабатывания
4. **Settings** — используйте `DisableEvents<T>()` для отключения событий в специфичных use cases (batch-операции, миграции)
5. **RequiredToSaveNavigationPropertiesNames** — указывайте navigation properties, необходимые для обработки событий, чтобы избежать N+1

### Анти-паттерны

| ❌ Анти-паттерн | ✅ Решение |
|----------------|-----------|
| Бизнес-логика в событиях, которая должна быть в Entity | Перенести логику в Entity method, событие — только для side-эффектов |
| `AfterSave` событие, которое модифицирует данные | Использовать `BeforeSave` |
| Событие с `.Result` / `.Wait()` | Всегда `async/await` с `CancellationToken` |
| Игнорирование `RequiredToSaveNavigationPropertiesNames` | Указать явно — EfUnitOfWork загрузит efficiently |

---

---

## См. также

| Документ | Описание |
|----------|----------|
| [Domain Modeling](domain-modeling.md) | Проектирование Domain слоя |
| [Unit of Work](unit-of-work.md) | Unit of Work и транзакции |
| [Entity Interfaces](entity-interfaces.md) | Интерфейсы сущностей (IEntity, IWithCreated, IWithDeleted) |
