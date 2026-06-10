# Lifecycle Actions

**Namespace:** `Shared.Application.Core.LifecycleAction`, `Shared.Application.Core.LifecycleAction.Interfaces`, `Shared.Domain.Core.Enums`  
**Assemblies:** `Shared.Application.Core.dll`, `Shared.Domain.Core.dll`

---

## Обзор

Lifecycle Actions — механизм trigger'ов side-эффектов на уровне Application слоя. Действия регистрируются как отдельные `ILifecycleActionHandler<TEntity>`-классы, обнаруживаются через `AddLifecycleActions()` и исполняются `LifecycleActionOrchestrator` в процессе `SaveChangesAsync` — до или после фактического сохранения в БД.

Архитектура построена вокруг **одного обработчика на одну комбинацию `(EntityType, LifecyclePhase, Key)`** и **оркестратора**, который собирает все зарегистрированные обработчики, фильтрует сущности и диспетчеризует вызовы.

**Зачем это нужно:**
- Декомпозиция бизнес-логики без нарушения SRP
- Trigger'ование side-эффектов (отправка уведомлений, аудит, каскадные операции)
- Контроль lifecycle: `BeforeSave` (валидация, обогащение данных) vs `AfterSave` (публикация, нотификация)
- Гибкое управление активностью действий (глобально / по фазе / по ключу / по конкретной сущности)

---

## Базовые интерфейсы

### `ILifecycleActionHandler`

Контракт обработчика действий перехвата. Реализуется напрямую или через generic-вариант `ILifecycleActionHandler<TEntity>`.

```csharp
public interface ILifecycleActionHandler
{
    Type EntityType { get; }
    LifecyclePhase Phase { get; }
    string Key { get; }
    int Order { get; }
    string[] RequiredNavigationProperties => [];

    Task ExecuteAsync(
        IEnumerable<IEntity> entities,
        CancellationToken cancellationToken);
}

public interface ILifecycleActionHandler<TEntity>
    : ILifecycleActionHandler
    where TEntity : class, IEntity
{
    Type ILifecycleActionHandler.EntityType => typeof(TEntity);

    Task ExecuteAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken);
}
```

| Член | Описание |
|------|----------|
| `EntityType` | Тип сущности, к которой применяется обработчик |
| `Phase` | Фаза жизненного цикла (`BeforeSave` / `AfterSave`) |
| `Key` | Уникальный строковый идентификатор действия в рамках `(EntityType, Phase)` |
| `Order` | Порядок выполнения относительно других обработчиков той же фазы |
| `RequiredNavigationProperties` | Имена navigation-свойств, которые `EfUnitOfWork` загрузит перед вызовом |
| `ExecuteAsync` | Метод выполнения действия |

### `LifecycleActionHandlerBase<TEntity>`

Абстрактная основа для большинства обработчиков — реализует `ILifecycleActionHandler<TEntity>` и фильтрует `IEnumerable<IEntity>` через `OfType<TEntity>()`:

```csharp
public abstract class LifecycleActionHandlerBase<TEntity>
    : ILifecycleActionHandler<TEntity>
    where TEntity : class, IEntity
{
    public abstract LifecyclePhase Phase { get; }
    public abstract string Key { get; }
    public abstract int Order { get; }
    public virtual string[] RequiredNavigationProperties => [];

    public Task ExecuteAsync(
        IEnumerable<IEntity> entities,
        CancellationToken cancellationToken)
    {
        return ((ILifecycleActionHandler<TEntity>)this).ExecuteAsync(
            entities.OfType<TEntity>(),
            cancellationToken);
    }

    public async Task ExecuteAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken)
    {
        if (entities.Any())
        {
            await ExecuteActionAsync(entities, cancellationToken);
        }
    }

    protected abstract Task ExecuteActionAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken);
}
```

**Ключевое поведение:**
1. Метод `ExecuteAsync` фильтрует входные сущности по типу `TEntity` (через `.OfType<TEntity>()`).
2. Если подходящих сущностей нет — `ExecuteActionAsync` не вызывается.
3. Override `RequiredNavigationProperties` для перечисления navigation-свойств, которые нужно загрузить перед выполнением (иначе `EfUnitOfWork` не сможет корректно их прочитать).

### `ILifecycleActionOrchestrator`

Управляет действиями перехвата жизненного цикла. Является фасадом над `ILifecycleEntityRegistry` (карта отслеживаемых сущностей) и `ILifecycleActionGate` (настройки активности действий).

```csharp
public interface ILifecycleActionOrchestrator
{
    void AddEntities(IEnumerable<IEntity> entities);
    void RemoveEntities(IEnumerable<IEntity> entities);

    string[] GetRequiredProperties(Type entityType);
    bool IsActionEnabled(IEntity entity, string key, LifecyclePhase phase);

    void EnableActions();
    void DisableActions();
    void EnableActions(IReadOnlyList<string> keys);
    void DisableActions(IReadOnlyList<string> keys);
    void EnableActionForEntity(string key, IEntity entity);
    void DisableActionForEntity(string key, IEntity entity);
    void EnableActionsForEntities(IReadOnlyList<string> keys, IReadOnlyList<IEntity> entities);
    void DisableActionsForEntities(IReadOnlyList<string> keys, IReadOnlyList<IEntity> entities);

    void EnablePhase(LifecyclePhase phase);
    void DisablePhase(LifecyclePhase phase);
    void EnablePhaseForEntity(LifecyclePhase phase, IEntity entity);
    void DisablePhaseForEntity(LifecyclePhase phase, IEntity entity);

    Task DispatchAsync(LifecyclePhase phase, CancellationToken cancellationToken);

    void ResetAllActions();
}
```

| Метод | Назначение |
|-------|-----------|
| `AddEntities` / `RemoveEntities` | Учёт сущностей в карте отслеживаемых (вызывается из `EfUnitOfWork` по событиям `ChangeTracker`) |
| `GetRequiredProperties` | Возвращает объединение `RequiredNavigationProperties` всех обработчиков указанного типа |
| `IsActionEnabled` | Проверяет, разрешено ли выполнение действия для конкретной сущности |
| `EnableActions` / `DisableActions` | Включение/отключение всех действий; либо только указанных ключей (глобально) |
| `EnableActionForEntity` / `DisableActionForEntity` | Короткая форма per-entity управления одним ключом — самый частый сценарий |
| `EnableActionsForEntities` / `DisableActionsForEntities` | Per-entity управление для нескольких ключей и сущностей |
| `EnablePhase` / `DisablePhase` | Включение/отключение действий определённой фазы (глобально) |
| `EnablePhaseForEntity` / `DisablePhaseForEntity` | Per-entity отключение целой фазы |
| `DispatchAsync` | Диспетчеризация всех разрешённых обработчиков указанной фазы в порядке `Order` |
| `ResetAllActions` | Сброс всех overrides (глобальный/по ключам/по фазам/по сущностям) в исходное состояние |

**Приоритет проверок в `IsActionEnabled` (от высшего к низшему):**

1. Отключённый ключ для конкретной сущности
2. Отключённая фаза для конкретной сущности
3. Глобально отключённый ключ
4. Глобально отключённая фаза
5. Глобальный флаг (`_globalEnabled`)

> **Замечание о семантике per-entity:** `EnableActionForEntity` / `EnableActionsForEntities` снимают только ранее установленный per-entity disable (через `DisableActionForEntity` / `DisableActionsForEntities`). Они **не** отменяют глобальный disable для одной сущности. Если действие глобально отключено через `DisableActions(["k"])`, вернуть его только для одной сущности текущий API не позволяет — это намеренное упрощение модели хранения состояния. Метода `EnableForEntity` без префикса `Action` не существует — корректное имя: `EnableActionForEntity(key, entity)`.

### `LifecyclePhase`

```csharp
public enum LifecyclePhase
{
    BeforeSave,  // До вызова SaveChangesAsync
    AfterSave,   // После вызова SaveChangesAsync
}
```

---

## Регистрация в DI

Все компоненты подключаются одним вызовом `AddLifecycleActions()` в `DependencyInjector` (`Shared.Application.Core/DependencyInjection/DependencyInjector.cs:51`):

```csharp
public static IServiceCollection AddLifecycleActions(this IServiceCollection serviceCollection)
{
    return serviceCollection
        .AddLifecycleHandlers()
        .AddLifecycleOrchestrator();
}
```

| Метод | Что делает |
|-------|-----------|
| `AddLifecycleHandlers` | Сканирует все загруженные сборки, регистрирует все `ILifecycleActionHandler` как Scoped-сервисы (через `RegisterDerivedTypeDependencies`) |
| `AddLifecycleOrchestrator` | Регистрирует `ILifecycleActionOrchestrator` (Scoped) → `LifecycleActionOrchestrator` |

Обработчики автоматически обнаруживаются и подхватываются DI при добавлении ссылки на сборку, где они определены. Никаких ручных регистраций не требуется.

---

## Создание обработчика

### Шаг 1: унаследовать `LifecycleActionHandlerBase<TEntity>`

```csharp
using Shared.Application.Core.LifecycleAction;
using Shared.Domain.Core.Enums;

namespace MyService.Application.LifecycleActions;

/// <summary>
/// Пересчитывает хэш сущности "Person" перед сохранением.
/// </summary>
public class PersonHashLifecycleHandler
    : LifecycleActionHandlerBase<Person>
{
    /// <inheritdoc />
    public override LifecyclePhase Phase => LifecyclePhase.BeforeSave;

    /// <inheritdoc />
    public override string Key => "CalculatePersonHashBeforeSavingChanges";

    /// <inheritdoc />
    public override int Order => 0;

    /// <inheritdoc />
    protected override Task ExecuteActionAsync(
        IEnumerable<Person> entities,
        CancellationToken cancellationToken)
    {
        foreach (var person in entities)
        {
            person.UpdateHash();
        }
        return Task.CompletedTask;
    }
}
```

Обработчик регистрируется автоматически: после добавления проекта в solution и регистрации `AddLifecycleActions()` в `DependencyInjector` он будет найден через reflection.

### Шаг 2 (опционально): объявить navigation properties

Если для выполнения действия нужны navigation-свойства сущности, которые могут быть не загружены в `ChangeTracker`:

```csharp
public override string[] RequiredNavigationProperties =>
[
    nameof(Order.Items),
    nameof(Order.Customer),
];
```

`EfUnitOfWork.SaveChangesAsync` вызовет `orchestrator.GetRequiredProperties(entityType)`, объединит результаты по всем обработчикам и выполнит `Include` одним запросом на каждое navigation property для всего типа (см. `EfUnitOfWork.IncludeRequiredNavigationPropertiesAsync`).

### Шаг 3 (опционально): DI-доступ к сервисам

`ILifecycleActionOrchestrator` уже Scoped, а сам обработчик — Scoped-сервис, поэтому любые зависимости регистрируются стандартно через конструктор:

```csharp
public class SendOrderConfirmationHandler(
    IEmailService emailService,
    ILogger<SendOrderConfirmationHandler> logger)
    : LifecycleActionHandlerBase<Order>
{
    public override LifecyclePhase Phase => LifecyclePhase.AfterSave;
    public override string Key => "SendOrderConfirmation";
    public override int Order => 0;

    protected override async Task ExecuteActionAsync(
        IEnumerable<Order> entities,
        CancellationToken cancellationToken)
    {
        foreach (var order in entities)
        {
            try
            {
                await emailService.SendOrderConfirmationAsync(order, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send confirmation for order {OrderId}", order.Id);
            }
        }
    }
}
```

---

## Архитектура: разделение Registry и Gate

`ILifecycleActionOrchestrator` — это фасад. Реальная работа разделена между двумя внутренними сервисами, которые регистрируются в DI как **scoped** и могут быть инжектированы независимо для тестирования и расширения:

| Сервис | Ответственность | Состояние |
|--------|----------------|-----------|
| `ILifecycleEntityRegistry` | Карта отслеживаемых сущностей: кто попадает в диспетчеризацию | `Dictionary<EntityKey, IEntity>` |
| `ILifecycleActionGate` | Настройки активности: разрешено или отключено | 4 словаря + `bool _globalEnabled` |

**`EntityKey`** — `readonly record struct (Type, object Id)`. Используется как ключ обеих коллекций вместо ссылочной идентичности `IEntity`. Это гарантирует, что одна и та же доменная сущность остаётся идентифицируемой при подмене инстанса (Attach/Detach, разные сессии, прокси EF).

```csharp
public readonly record struct EntityKey(Type Type, object Id)
{
    public static EntityKey Of(IEntity entity) => new(entity.GetType(), entity.Id);
}
```

### Почему разделение

- **Single Responsibility**: registry и gate — это две разные причины для изменения (трекинг vs. конфигурация активности).
- **Тестируемость**: каждый компонент покрывается собственными unit-тестами без необходимости поднимать orchestrator.
- **Расширяемость**: можно реализовать кастомный gate (например, читающий настройки из БД) без изменения реестра, и наоборот.

### Регистрация в DI

```csharp
services.AddLifecycleActions();
// внутри:
//   .AddScoped<ILifecycleEntityRegistry, LifecycleEntityRegistry>()
//   .AddScoped<ILifecycleActionGate, LifecycleActionGate>()
//   .AddScoped<ILifecycleActionOrchestrator, LifecycleActionOrchestrator>()
```

В production обычно используется фасад `ILifecycleActionOrchestrator`. Прямая инжекция `ILifecycleEntityRegistry` / `ILifecycleActionGate` оправдана в инфраструктурных адаптерах и в тестах.

---

## Управление активностью действий

`ILifecycleActionOrchestrator` позволяет точечно отключать действия, не убирая обработчик из DI.

### Через `IUnitOfWork` в рамках операции

Метод `SaveChangesAsync` принимает флаг `resetLifecycleActionSettingsAfterSave = true` — после сохранения `ResetAllActions()` вызывается автоматически (см. `EfUnitOfWork.cs:143-147`). Любые `Enable/Disable`, выставленные внутри скоупа UoW, сбрасываются после `SaveChangesAsync`.

### Через прямой вызов `ILifecycleActionOrchestrator`

```csharp
public class MyService
{
    private readonly ILifecycleActionOrchestrator _orchestrator;

    public async Task BulkImportAsync(IEnumerable<Person> people)
    {
        // Полностью отключить все действия перехвата
        _orchestrator.DisableActions();

        try
        {
            // ... добавить people в репозиторий ...
            await _unitOfWork.SaveChangesAsync();
        }
        finally
        {
            _orchestrator.ResetAllActions();
        }
    }
}
```

### Отключение по ключу

```csharp
// Отключить только действие с указанным ключом (для всех отслеживаемых сущностей)
_orchestrator.DisableActions(new[] { "CalculatePersonHashBeforeSavingChanges" });

// Включить обратно
_orchestrator.EnableActions(new[] { "CalculatePersonHashBeforeSavingChanges" });
```

### Отключение по фазе

```csharp
// Отключить все AfterSave-действия (перед массовым импортом)
_orchestrator.DisablePhase(LifecyclePhase.AfterSave);

_orchestrator.EnablePhase(LifecyclePhase.AfterSave);
```

### Отключение для конкретной сущности

```csharp
// Короткая форма — один ключ + одна сущность
var person = await personRepo.GetAsync(personId);
_orchestrator.DisableActionForEntity("SendOrderConfirmation", person);

// Включить обратно
_orchestrator.EnableActionForEntity("SendOrderConfirmation", person);

// Множественная форма — несколько ключей и/или сущностей
_orchestrator.DisableActionsForEntities(
    new[] { "SendOrderConfirmation", "SendPushNotification" },
    new[] { person });
```

### Отключение фазы для конкретной сущности

```csharp
_orchestrator.DisablePhaseForEntity(LifecyclePhase.AfterSave, person);
_orchestrator.EnablePhaseForEntity(LifecyclePhase.AfterSave, person);
```

> **Важно:** все `Enable/Disable` по умолчанию **сбрасываются** после `SaveChangesAsync` (благодаря параметру `resetLifecycleActionSettingsAfterSave: true`). Чтобы overrides сохранялись между сохранениями, передавайте `resetLifecycleActionSettingsAfterSave: false` в `SaveChangesAsync`.

---

## Интеграция с `EfUnitOfWork`

`EfUnitOfWork<TDbContext>` (см. `Shared.Infrastructure.Dal.EFCore/EfUnitOfWork.cs`) автоматически интегрирован с `ILifecycleActionOrchestrator`:

1. **При трекинге** сущности (`OnEntityTracked`): `orchestrator.AddEntities([entity])` — сущность попадает в карту.
2. **При детаче** (`OnEntityStateChanged` для `EntityState.Detached`): `orchestrator.RemoveEntities([entity])` — сущность удаляется из карты.
3. **В `SaveChangesAsync`**:
   - `IncludeRequiredNavigationPropertiesAsync` — загружает navigation properties, указанные в `RequiredNavigationProperties` обработчиков.
   - `DispatchAsync(LifecyclePhase.BeforeSave, ...)` — все `BeforeSave`-обработчики.
   - `ProcessBeforeSaveChangesActionsAsync` — pre-save хуки через `IBeforeSaveChangesService` (опционально).
   - `DbContext.SaveChangesAsync` — фактическое сохранение.
   - `CommitTransactionAsync` — коммит транзакции (если включена).
   - `DispatchAsync(LifecyclePhase.AfterSave, ...)` — все `AfterSave`-обработчики.
4. **В `finally`**: `orchestrator.ResetAllActions()` (если `resetLifecycleActionSettingsAfterSave = true`) и сброс транзакции.

**Порядок выполнения в `SaveChangesAsync`:**

```
Include Navigation Properties
  ↓
BeforeSave Actions  (через ILifecycleActionOrchestrator.DispatchAsync)
  ↓
IBeforeSaveChangesService.ProcessAsync
  ↓
DbContext.SaveChangesAsync
  ↓
Commit Transaction (если включена)
  ↓
AfterSave Actions  (через ILifecycleActionOrchestrator.DispatchAsync)
  ↓
ResetAllActions (если resetLifecycleActionSettingsAfterSave = true)
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
| Синхронный вызов сервиса из handler'а | ❌ Лучше через MediatR pipeline |

### Правила

1. **BeforeSave** — для изменений, которые должны попасть в ту же транзакцию (валидация, обогащение, аудит).
2. **AfterSave** — для действий после коммита (нотификации, интеграционные события, кэш-инвалидация).
3. **Key** — стабильная строка; одна комбинация `(EntityType, Phase, Key)` должна встречаться ровно один раз в DI-контейнере.
4. **Order** — используется только для упорядочивания обработчиков одной фазы; меньшие значения выполняются раньше.
5. **RequiredNavigationProperties** — указывайте явно, чтобы `EfUnitOfWork` загрузил их одним запросом и избежать N+1.
6. **Error handling** — внутри `ExecuteActionAsync` обрабатывайте исключения самостоятельно; непойманное исключение прервёт `SaveChangesAsync` и откатит транзакцию.

### Анти-паттерны

| ❌ Анти-паттерн | ✅ Решение |
|----------------|-----------|
| Бизнес-логика в действиях, которая должна быть в Entity | Перенести логику в Entity method, действие — только для side-эффектов |
| `AfterSave` действие, которое модифицирует данные | Использовать `BeforeSave` |
| Действие с `.Result` / `.Wait()` | Всегда `async/await` с `CancellationToken` |
| Игнорирование `RequiredNavigationProperties` | Указать явно — `EfUnitOfWork` загрузит efficiently |

---

## См. также

| Документ | Описание |
|----------|----------|
| [Domain Modeling](domain-modeling.md) | Проектирование Domain слоя |
| [Unit of Work](unit-of-work.md) | `IUnitOfWork`, `EfUnitOfWork` и транзакции |
| [Entity Interfaces](entity-interfaces.md) | `IEntity`, audit-интерфейсы, soft delete |
| [EF Core Internals](efcore-internals.md) | Внутренние механизмы `EfRepository`, `EfUnitOfWork` |
