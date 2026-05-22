# Lifecycle Actions

**Namespace:** `Shared.Domain.Core.LifecycleAction`  
**Assembly:** `Shared.Domain.Core`

---

## Обзор

Lifecycle Actions — механизм trigger'ов side-эффектов на уровне Domain слоя. Сущности, реализующие `IWithLifecycleActions`, регистрируют действия, которые обрабатываются Unit of Work в процессе `SaveChanges` — до или после фактического сохранения в БД.

**Зачем это нужно:**
- Декомпозиция бизнес-логики без нарушения SRP
- Trigger'ование side-эффектов (отправка уведомлений, аудит, каскадные операции)
- Изоляция Domain слоя от Infrastructure concerns
- Контроль lifecycle: `BeforeSave` (валидация, обогащение данных) vs `AfterSave` (публикация, нотификация)

---

## Типы действий

### IEntityLifecycleAction

Базовый интерфейс всех действий перехвата:

```csharp
public interface IEntityLifecycleAction
{
    Enum Key { get; }

    Task ExecuteAsync(
        LifecycleHookType hookType,
        IServiceProvider serviceProvider,
        ICollection<IWithLifecycleActions> entities,
        CancellationToken cancellationToken);

    void Enable();
    void Disable();
}
```

| Член | Описание |
|------|----------|
| `Key` | Уникальный идентификатор действия (enum) |
| `ExecuteAsync` | Выполняет логику действия |
| `Enable()` | Включает действие для повторного срабатывания |
| `Disable()` | Отключает действие (автоматически после обработки) |

### CustomLifecycleAction

Lambda-based действие — вся логика передаётся через `Func`:

```csharp
public class CustomLifecycleAction(
    Enum key,
    Func<IServiceProvider, ICollection<IWithLifecycleActions>, CancellationToken, Task> action)
    : EntityLifecycleActionBase(key)
{
    protected override Task ExecuteActionAsync(
        IServiceProvider serviceProvider,
        ICollection<IWithLifecycleActions> entities,
        CancellationToken cancellationToken) =>
        action(serviceProvider, entities, cancellationToken);
}
```

**Когда использовать:** одноразовые действия, inline-логика, динамическая регистрация.

### TypeLifecycleAction

Type-based действие — аналогично `CustomLifecycleAction`, но с явным полем `_action`:

```csharp
public class TypeLifecycleAction : EntityLifecycleActionBase
{
    private readonly Func<IServiceProvider, ICollection<IWithLifecycleActions>, CancellationToken, Task> _action;

    public TypeLifecycleAction(
        Enum key,
        Func<IServiceProvider, ICollection<IWithLifecycleActions>, CancellationToken, Task> action)
        : base(key)
    {
        _action = action;
    }

    protected override Task ExecuteActionAsync(
        IServiceProvider serviceProvider,
        ICollection<IWithLifecycleActions> entities,
        CancellationToken cancellationToken)
        => _action(serviceProvider, entities, cancellationToken);
}
```

**Когда использовать:** действия, привязанные к конкретному типу сущностей, где нужна передача коллекции entities.

### EntityLifecycleAction

Упрощённое действие — не работает с коллекцией entities, не вызывает `DisableEntitiesActions`:

```csharp
public class EntityLifecycleAction : EntityLifecycleActionBase
{
    private readonly Func<IServiceProvider, CancellationToken, Task> _action;

    public EntityLifecycleAction(
        Enum key,
        Func<IServiceProvider, CancellationToken, Task> action)
        : base(key)
    {
        _action = action;
    }

    protected override Task ExecuteActionAsync(
        IServiceProvider serviceProvider,
        ICollection<IWithLifecycleActions> entities,
        CancellationToken cancellationToken)
        => _action(serviceProvider, cancellationToken);

    /// <summary>
    /// Пустая реализация — действия других сущностей не отключаются.
    /// </summary>
    protected override void DisableEntitiesActions(
        LifecycleHookType hookType,
        ICollection<IWithLifecycleActions> entities)
    {
    }
}
```

**Когда использовать:** глобальные действия, не требующие доступа к конкретным entities.

---

## Создание кастомных действий

### EntityLifecycleActionBase — абстрактная основа

Все действия наследуются от `EntityLifecycleActionBase`:

```csharp
public abstract class EntityLifecycleActionBase(Enum key) : IEntityLifecycleAction
{
    private bool _enabled = true;

    public Enum Key { get; } = key;

    public async Task ExecuteAsync(
        LifecycleHookType hookType,
        IServiceProvider serviceProvider,
        ICollection<IWithLifecycleActions> entities,
        CancellationToken cancellationToken)
    {
        if (_enabled)
            await ExecuteActionAsync(serviceProvider, entities, cancellationToken);

        Disable();
        DisableEntitiesActions(hookType, entities);
    }

    public void Enable() => _enabled = true;
    public void Disable() => _enabled = false;

    protected abstract Task ExecuteActionAsync(
        IServiceProvider serviceProvider,
        ICollection<IWithLifecycleActions> entities,
        CancellationToken cancellationToken);

    protected virtual void DisableEntitiesActions(
        LifecycleHookType hookType,
        ICollection<IWithLifecycleActions> entities) =>
        entities.ForEach(x =>
        {
            if (x.TryGetAction(hookType, Key, out var lifecycleAction))
                lifecycleAction.Disable();
        });
}
```

**Ключевое поведение:**
1. `ExecuteAsync` проверяет `_enabled`, вызывает `ExecuteActionAsync`, затем `Disable()` + `DisableEntitiesActions()`
2. `DisableEntitiesActions` автоматически отключает одноимённые действия у всех переданных entities — предотвращает повторную обработку
3. Override `DisableEntitiesActions` для изменения этого поведения (как в `EntityLifecycleAction`)

### Пример: кастомное действие

```csharp
// Enum для ключей действий
public enum OrderActions
{
    OrderCreated,
    OrderStatusChanged,
    OrderShipped,
}

// Кастомное действие
public class OrderCreatedAction : EntityLifecycleActionBase
{
    public OrderCreatedAction() : base(OrderActions.OrderCreated) { }

    protected override async Task ExecuteActionAsync(
        IServiceProvider serviceProvider,
        ICollection<IWithLifecycleActions> entities,
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

## Action Settings — контроль срабатывания

Иерархия настроек позволяет тонко управлять тем, какие действия срабатывают:

```
LifecycleActionSettings
└── EntityTypeActionSettings (по типу сущности)
    └── ActionKeySettings (по LifecycleHookType: BeforeSave/AfterSave)
        └── Enum flags (конкретные action keys)
```

### LifecycleActionSettings

Корневая настройка — управляет действиями по типу сущности:

```csharp
public record LifecycleActionSettings(bool Enabled = true)
    : ActionSettingsWithInternalSettingsBase<
        Type,
        EntityTypeActionSettings,
        LifecycleHookType,
        Dictionary<LifecycleHookType, ActionKeySettings>>(Enabled)
{
    protected override EntityTypeActionSettings CreateExceptItem(bool enabled) => new(enabled);

    public bool AnyElementEnabled(Type typeKey, LifecycleHookType hookTypeKey, Enum actionKey);
    public void Switch(Type typeKey, LifecycleHookType hookTypeKey, Enum actionKey, bool enabled);
}
```

### ActionSettingBase<TItems, TKey>

Базовый класс настроек с паттерном "enabled + exceptions":

```csharp
public abstract record ActionSettingBase<TItems, TKey>
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

### ActionKeySettings

Настройка по `LifecycleHookType` с flag-based filtering:

```csharp
public record ActionKeySettings(bool Enabled = true)
    : ActionSettingBase<Enum?, Enum>(Enabled)
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

### EntityTypeActionSettings

Настройка по типу сущности:

```csharp
public record EntityTypeActionSettings(bool Enabled = true)
    : ActionSettingsWithInternalSettingsBase<LifecycleHookType, ActionKeySettings, Enum, Enum?>(Enabled)
{
    protected override ActionKeySettings CreateExceptItem(bool enabled) => new(enabled);
}
```

### Пример использования в Unit of Work

```csharp
// Отключить все действия
unitOfWork.DisableLifecycleActions();

// Отключить действия для конкретного типа
unitOfWork.DisableLifecycleActions<Order>();

// Отключить BeforeSave-действия для Order
unitOfWork.DisableLifecycleActions<Order>(LifecycleHookType.BeforeSave);

// Отключить конкретный action key
unitOfWork.DisableLifecycleActions<Order>(LifecycleHookType.AfterSave, OrderActions.OrderCreated);

// Включить обратно
unitOfWork.EnableLifecycleActions();
unitOfWork.EnableLifecycleActions<Order>(LifecycleHookType.AfterSave, OrderActions.OrderCreated);
```

---

## Интеграция с Unit of Work

### IWithLifecycleActions

Интерфейс, который сущность должна реализовать для поддержки lifecycle actions:

```csharp
public interface IWithLifecycleActions
{
    string[] RequiredToSaveNavigationPropertiesNames { get; }

    bool TryGetAction(LifecycleHookType hookType, Enum key, out IEntityLifecycleAction lifecycleAction);

    void ResetActions();

    ICollection<Enum> GetAllKeys(LifecycleHookType hookType);

    Task ProcessLifecycleActionAsync(
        LifecycleHookType hookType,
        Enum key,
        IServiceProvider serviceProvider,
        ICollection<IWithLifecycleActions>? entities = null,
        CancellationToken cancellationToken = default);

    Task ProcessLifecycleActionsAsync(
        LifecycleHookType hookType,
        IServiceProvider serviceProvider,
        ICollection<IWithLifecycleActions>? entities = null,
        CancellationToken cancellationToken = default);
}
```

| Член | Описание |
|------|----------|
| `RequiredToSaveNavigationPropertiesNames` | Имена navigation properties, которые нужно загрузить перед обработкой действий |
| `TryGetAction` | Извлечение действия по типу и ключу |
| `ResetActions` | Сброс очередей действий к обязательным |
| `GetAllKeys` | Все ключи действий заданного типа |
| `ProcessLifecycleActionAsync` | Обработка одного действия |
| `ProcessLifecycleActionsAsync` | Обработка всех действий заданного типа |

### LifecycleHookType

```csharp
public enum LifecycleHookType
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
    bool resetLifecycleActionSettingsAfterSave = true)
{
    var entryTypeGroups = EntriesWithLifecycleActions
        .GroupBy(e => e.Entity.GetType())
        .ToArray();

    try
    {
        // 1. BeforeSave действия
        await ProcessLifecycleActionsAsync(entryTypeGroups, LifecycleHookType.BeforeSave, cancellationToken);

        // 2. Pre-save хуки (IBeforeSaveChangesService)
        await ProcessBeforeSaveChangesActionsAsync(cancellationToken);

        // 3. Фактическое сохранение
        var result = await DbContext.SaveChangesAsync(cancellationToken);

        if (commitTransaction)
            await CommitTransactionAsync(cancellationToken);

        // 4. AfterSave действия
        await ProcessLifecycleActionsAsync(entryTypeGroups, LifecycleHookType.AfterSave, cancellationToken);

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
        // 5. Сброс действий и настроек
        if (_lifecycleActionSettings.AnyEnabled)
            entryTypeGroups.SelectMany(x => x).ForEach(e => e.Entity.ResetActions());

        if (resetLifecycleActionSettingsAfterSave)
            ResetLifecycleActionSettings();

        if (commitTransaction)
            await ResetTransactionAsync(cancellationToken);
    }
}
```

**Порядок выполнения:**

```
BeforeSave Actions → IBeforeSaveChangesService → SaveChangesAsync → Commit → AfterSave Actions → Reset
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

### Когда использовать Lifecycle Actions

| Сценарий | Recommendation |
|----------|---------------|
| Отправка email после создания заказа | ✅ `AfterSave` action |
| Валидация перед сохранением | ✅ `BeforeSave` action |
| Аудит изменений | ✅ `BeforeSave` action |
| Каскадное обновление связанных сущностей | ✅ `BeforeSave` action |
| Публикация интеграционных событий | ✅ `AfterSave` action |
| Простая CRUD-операция без side-эффектов | ❌ Не нужно |
| Синхронный вызов сервиса из handler'а | ❌ Лучше через MediatR |

### Правила

1. **BeforeSave** — для изменений, которые должны попасть в ту же транзакцию (валидация, обогащение, аудит)
2. **AfterSave** — для действий после коммита (нотификации, интеграционные события, кэш-инвалидация)
3. **Enable/Disable** — действия автоматически отключаются после обработки; `Enable()` нужен для повторного срабатывания
4. **Settings** — используйте `DisableLifecycleActions<T>()` для отключения действий в специфичных use cases (batch-операции, миграции)
5. **RequiredToSaveNavigationPropertiesNames** — указывайте navigation properties, необходимые для обработки действий, чтобы избежать N+1

### Анти-паттерны

| ❌ Анти-паттерн | ✅ Решение |
|----------------|-----------|
| Бизнес-логика в действиях, которая должна быть в Entity | Перенести логику в Entity method, действие — только для side-эффектов |
| `AfterSave` действие, которое модифицирует данные | Использовать `BeforeSave` |
| Действие с `.Result` / `.Wait()` | Всегда `async/await` с `CancellationToken` |
| Игнорирование `RequiredToSaveNavigationPropertiesNames` | Указать явно — EfUnitOfWork загрузит efficiently |

---

---

## См. также

| Документ | Описание |
|----------|----------|
| [Domain Modeling](domain-modeling.md) | Проектирование Domain слоя |
| [Unit of Work](unit-of-work.md) | Unit of Work и транзакции |
| [Entity Interfaces](entity-interfaces.md) | Интерфейсы сущностей (IEntity, IWithCreated, IWithDeleted) |
