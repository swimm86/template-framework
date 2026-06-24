# Repository Pattern

## Обзор

Repository Pattern в фреймворке Shared предоставляет централизованный доступ к данным через универсальный интерфейс `IRepository<TEntity>`. Этот паттерн абстрагирует работу с источником данных, обеспечивая тестируемость и соблюдение принципа единой ответственности.

**Assembly / Namespace:** `Shared.Domain.Core.Dal.Repository.Interfaces` (контракт) и `Shared.Infrastructure.Dal.EFCore.Repository` (реализация `EfRepository<TEntity>`).

## Интерфейс IRepository<TEntity>

Контракт определён в `Shared.Domain.Core.Dal.Repository.Interfaces.IRepository<TEntity>`, где `TEntity : class, IEntity`. Полный XML-doc — в исходниках (`src/Shared/Core/Shared.Domain.Core/Dal/Repository/Interfaces/IRepository.cs`). Ниже — обзор методов по группам; подробные примеры и поведение — в следующих разделах.

### Чтение

| Метод | Перегрузки | Назначение |
|-------|------------|------------|
| `GetAsync` | 4 (по `id` × `QueryOptions`/`ISpecification` × `TEntity`/`TOut`) | Возвращает сущность по идентификатору (опционально с проекцией) |
| `GetRangeAsync` | 4 (`QueryOptions`/`ISpecification` × `TEntity`/`TOut`) | Коллекция с пагинацией `skip`/`take` |
| `FirstOrDefaultAsync` | 4 | Первый элемент выборки (опц. проекция) |
| `SingleOrDefaultAsync` | 4 | Единственный элемент; бросает `InvalidOperationException` при >1 |
| `LastOrDefaultAsync` | 4 | Последний элемент выборки (опц. проекция) |
| `GetGroupingAsync<TKey>` / `<TKey, TOut>` | 2 | Группировка с опциональной пагинацией и сортировкой групп |

### Агрегация

`CountAsync`, `AnyAsync`, `SumAsync`, `CountGroupsAsync<TKey>`, `MaxAsync<TOut>` — каждая с перегрузками по `QueryOptions` / `ISpecification`.

### Запись

| Метод | Назначение |
|-------|------------|
| `AddAsync` / `AddRangeAsync` | Добавление (принимает `userId`, `userName` для аудита) |
| `ExecuteUpdateRangeAsync(predicate / options / specification, ...)` | DB-level пакетное обновление через `ExecuteUpdateAsync` |
| `RemoveAsync` / `RemoveRangeAsync` / `RemovePermanentRangeAsync` | Soft/hard удаление через `ChangeTracker` |
| `ExecuteRemoveRangeAsync(options / predicate / specification)` | DB-level физическое удаление через `ExecuteDeleteAsync` |

### Выполнение и служебные

- `Execute(Action)` / `Execute<TResult>(Func<TResult>)` — синхронное выполнение с опциональной транзакцией.
- `ExecuteAsync(Func<Task>)` / `ExecuteAsync<TResult>(Func<Task<TResult>>)` — асинхронный аналог.
- `Set(QueryOptions<TEntity>?)` — доступ к `IQueryable<TEntity>` с применёнными настройками.
- `SaveChanges()` / `SaveChangesAsync(ct)` — сброс `ChangeTracker` в БД.

### Вспомогательный статический метод

`IRepository<TEntity>.GetUpdateRangeAsyncLambdaFunc<TProp>(propertyExpression, valueExpression)` — формирует кортеж `(LambdaExpression, LambdaExpression)` для `ExecuteUpdateRangeAsync`.

> **⚠️ Внимание: `ExecuteUpdateRangeAsync(predicate = null)`**  
> Если перегрузка `ExecuteUpdateRangeAsync(Expression<Func<TEntity, bool>>? predicate = null, ...)` вызвана **без условия** (или с `null`), `EfRepository` формирует `QueryOptions` без фильтров и выполняет `ExecuteUpdateAsync` **по всей таблице** — то же поведение, что и `UPDATE Table SET ...` без `WHERE`. Всегда передавайте явное условие, иначе обновление затронет каждую строку.

### Вспомогательный метод для ExecuteUpdateRangeAsync

Для формирования кортежей `(LambdaExpression, LambdaExpression)` используется **статический** метод интерфейса:

```csharp
public static (LambdaExpression, LambdaExpression) GetUpdateRangeAsyncLambdaFunc<TProp>(
    Expression<Func<TEntity, TProp>> propertyExpression,
    Expression<Func<TEntity, TProp>> valueExpression)
```

Вызывается **через тип** (`IRepository<TEntity>`), а не через экземпляр:

```csharp
await repository.ExecuteUpdateRangeAsync(
    x => x.Id == Guid.Parse("4c2ca6bf-8c8f-4cbf-9097-d40615241a2c"),
    IRepository<Person>.GetUpdateRangeAsyncLambdaFunc(x => x.Name, x => "name2"),
    IRepository<Person>.GetUpdateRangeAsyncLambdaFunc(x => x.Email, x => "user@example.com"));
```

## Ключевые возможности

### 1. Поддержка спецификаций

Репозиторий поддерживает Specification Pattern для инкапсуляции сложных критериев выборки:

```csharp
// Получение по спецификации
var spec = new ActiveUsersSpecification();
var users = await repository.GetRangeAsync(spec, skip: 0, take: 20);

// Спецификация автоматически преобразуется в QueryOptions
Task<TEntity?> GetAsync(object? id, ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
```

### 2. Гибкие настройки запроса (QueryOptions)

`QueryOptions<TEntity>` предоставляет расширенные возможности для настройки запросов. Конструктор принимает именованные параметры:

```csharp
var options = new QueryOptions<User>(withTracking: true, asSplitQuery: true, distinct: true);
```

**Свойства:**

| Свойство | Тип | Описание |
|----------|-----|----------|
| `Filters` | `List<Expression<Func<TEntity, bool>>>` | Список выражений фильтрации |
| `OrderBy` | `List<QueryOrderByOption<TEntity>>` | Список настроек сортировки |
| `Includes` | `List<IIncludable<TEntity>>` | Список связанных сущностей для загрузки |
| `WithTracking` | `bool` | Признак отслеживания изменений сущностей (`false` по умолчанию) |
| `AsSplitQuery` | `bool` | Использование split queries для includes |
| `Distinct` | `bool` | Признак исключения дублей |
| `DistinctBy` | `Expression<Func<TEntity, object>>?` | Условие для исключения дублей |
| `CustomQueryPostProcesses` | `List<Func<IQueryable<TEntity>, IQueryable<TEntity>>>` | Пользовательские пост-преобразования IQueryable |
| `CustomQueryBeforeProcesses` | `List<Func<IQueryable<TEntity>, IQueryable<TEntity>>>` | Пользовательские пре-преобразования IQueryable |

**Методы QueryOptions:**

| Метод | Описание |
|-------|----------|
| `AddFilter(Expression)` | Добавление фильтра |
| `AddFilterIf(bool, Expression)` | Условное добавление фильтра |
| `AddOrderBy(Expression, OrderDirectionType, int?)` | Добавление сортировки |
| `AddOrderBy(SortOption)` | Добавление сортировки из `SortOption` (по строковому ключу через reflection) |
| `AddOrderByIf(bool, Expression, OrderDirectionType, int?)` | Условное добавление сортировки |
| `AddInclude<TProperty>(Expression)` | Include для плоских свойств |
| `AddInclude<TProperty>(Expression<Func<TEntity, IEnumerable<TProperty>>>)` | Include для навигационных коллекций |

### 3. QueryOrderByOption

`QueryOrderByOption<TEntity>` — record-тип для настройки сортировки:

```csharp
public record QueryOrderByOption<TEntity>(
    Expression<Func<TEntity, object>> Expression,
    OrderDirectionType Direction);
```

**OrderDirectionType** (см. `Shared.Domain.Core.Dal/Enums.cs`):

| Значение | Описание |
|----------|----------|
| `Ascending` | По возрастанию (`[Description("asc")]`) |
| `Descending` | По убыванию (`[Description("desc")]`) |

**Пример создания:**
```csharp
var orderBy = new QueryOrderByOption<User>(u => u.CreatedAt, OrderDirectionType.Descending);
```

### 4. IIncludable — кастомное представление Include

`IIncludable<TSrcEntity>` — интерфейс для цепочечных включений:

| Свойство/Метод | Тип | Описание |
|----------------|-----|----------|
| `Expression` | `LambdaExpression` | Выражение Include |
| `Child` | `IIncludable<TSrcEntity>?` | Последующий Include |
| `SetChild(IIncludable)` | `void` | Установление последующего Include |

Реализация — `Includable<TSrcEntity, TDstEntity>` поддерживает вложенные includes через `SetChild` и расширения `ThenInclude` из `Shared.Domain.Core.Dal.Repository.Extensions.IncludableExtension` (4 перегрузки для плоского свойства, коллекции, фильтрованной и сортированной коллекции).

### 5. Projection на лету

Поддержка проекции данных в DTO без промежуточного маппинга:

```csharp
// Проекция в анонимный тип или DTO через generic-перегрузку <TOut>
var userDtos = await repository.GetRangeAsync<UserDto>(
    options: null,
    skip: 0,
    take: 20,
    selector: u => new UserDto
    {
        Id = u.Id,
        Name = u.Name,
        Email = u.Email
    }
);
```

Если `selector` не указан, используется AutoMapper для преобразования.

### 6. Soft/Hard Delete

Репозиторий поддерживает мягкое и физическое удаление. Поведение `RemoveAsync` зависит от маркерного интерфейса `IWithDeleted` (см. раздел [«Маркерные интерфейсы»](#7-маркерные-интерфейсы-поведения-addremove)):

```csharp
// Soft delete (по умолчанию) — вызывает IWithDeleted.SetIsDeleted()/OnDelete(),
// НЕ вызывает DbSet.Remove()
await repository.RemoveAsync(user);

// Hard delete — физическое удаление из БД (DbSet.Remove)
await repository.RemoveAsync(user, hard: true);

// RemoveAsync с userId для аудита (для soft delete используется в OnDelete)
await repository.RemoveAsync(user, userId: currentUserId);

// Физическое удаление коллекции (без soft delete)
await repository.RemovePermanentRangeAsync(entities);
```

#### DB-level физическое удаление (`ExecuteRemoveRangeAsync`)

Для сценариев, когда нужно удалить большое количество записей **без** загрузки сущностей в `ChangeTracker` и **без** вызова `SaveChangesAsync`, используйте `ExecuteRemoveRangeAsync` — он транслируется в `ExecuteDeleteAsync` EF Core и выполняется одним SQL-запросом `DELETE ... WHERE ...`. Доступны три перегрузки:

```csharp
// По QueryOptions
var options = new QueryOptions<User>().AddFilter(u => u.IsActive == false);
await repository.ExecuteRemoveRangeAsync(options, cancellationToken);

// По предикату
await repository.ExecuteRemoveRangeAsync(u => u.LastLoginAt < DateTime.UtcNow.AddYears(-1), cancellationToken);

// По спецификации
await repository.ExecuteRemoveRangeAsync(new InactiveUsersSpecification(), cancellationToken);
```

> **⚠️ TPT-наследование не поддерживается** для `ExecuteRemoveRangeAsync`: провайдер EF Core не умеет транслировать `ExecuteDeleteAsync` через TPT-иерархии. Используйте `RemoveRangeAsync` (через tracker) для сущностей, участвующих в TPT-схеме.
>
> **Эффект наступает немедленно** — `SaveChangesAsync` вызывать не нужно. Soft-delete (`IWithDeleted`) при этом **не** срабатывает; записи удаляются физически.

#### Сводная таблица: tracked vs DB-level операции

Операции изменения (`Update*` / `Remove*`) существуют в двух вариантах — через `ChangeTracker` (трекерные) и через SQL напрямую (`Execute*`). Выбор между ними определяется требованиями к soft-delete, аудиту и производительности:

| Сценарий | Метод | Загружает в `ChangeTracker` | Требует `SaveChangesAsync` | Soft-delete (`IWithDeleted`) | TPT | SQL |
|----------|-------|:--:|:--:|:--:|:--:|:--:|
| Массовое обновление с фильтром | `ExecuteUpdateRangeAsync(predicate/options/spec, ...)` | ❌ | ❌ | n/a | ✅ | один `UPDATE ... WHERE ...` |
| Массовое физическое удаление с фильтром | `ExecuteRemoveRangeAsync(predicate/options/spec)` | ❌ | ❌ | ❌ (всегда физическое) | ❌ | один `DELETE ... WHERE ...` |
| Soft/hard-удаление с фильтром | `RemoveRangeAsync(predicate/options/spec, hard)` | ✅ | ✅ | ✅ при `hard: false` | ✅ | `SELECT` + `DELETE`-ы поштучно |

> В DB-level методах `ExecuteUpdateRangeAsync(QueryOptions<TEntity> options, ...)` и `ExecuteRemoveRangeAsync(QueryOptions<TEntity> options, ...)` параметр `options` — **non-nullable**: вы должны передать готовый `QueryOptions` (с фильтрами и/или сортировкой). Это отличается от трекерного `RemoveRangeAsync(QueryOptions<TEntity>? options, ...)`, где `options` может быть `null`.

### 7. Маркерные интерфейсы (поведения Add/Remove)

EfRepository в `AddAsync` / `RemoveAsync` реагирует на **маркерные интерфейсы** из `Shared.Domain.Core.Interfaces`. Если сущность их реализует, репозиторий вызывает соответствующие методы автоматически.

| Интерфейс | Когда срабатывает | Что делает EfRepository |
|-----------|-------------------|-------------------------|
| `IWithCreated` : `IWithDateCreated` | `AddAsync(entity, userId, userName)` | Вызывает `entity.OnCreate(userId, userName)` (заполняет `CreatedByUserId`, `CreatedByUserName`, `DateCreated`) |
| `IWithDateUpdated` / `IWithUpdated` | при `SaveChangesAsync` для tracked-сущности (через `IBeforeSaveChangesService`) | Устанавливает `DateUpdated` |
| `IWithDeleted` : `IWithDateDeleted` | `RemoveAsync(entity, ..., hard: false)` | Вызывает `SetIsDeleted()` + `OnDelete(userId)` — soft delete. При `hard: true` — `DbSet.Remove(entity)` |
| `IWithAdditionalData` | — | Контракт для передачи доп. данных через HTTP-ответы (на поведение репозитория не влияет) |

> **Пример реализации Person** (см. `src/Services/Common/Template.Domain/Entities/Person.cs`): сущность `Person : EntityBase<Guid>` не реализует ни один из маркерных интерфейсов аудита, поэтому `AddAsync(person, ...)` и `RemoveAsync(person, ...)` не вызывают никаких auto-методов. Чтобы аудит заработал, добавьте `: IWithCreated, IWithUpdated, IWithDeleted` к domain-классу.

### 8. Audit Fields

Автоматическое заполнение аудиторских полей при операциях записи — **только если сущность реализует соответствующий маркерный интерфейс** (см. выше):

```csharp
// При условии, что TEntity : IWithCreated
await repository.AddAsync(entity, userId: currentUserId, userName: currentUserName);
await repository.AddRangeAsync(entities, userId: currentUserId, userName: currentUserName);
```

### 9. Выполнение операций (Execute / ExecuteAsync)

Синхронные и асинхронные операции с опциональной транзакцией:

```csharp
// Синхронное выполнение без результата
repository.Execute(() =>
{
    // логика
}, useTransaction: true);

// Синхронное выполнение с результатом
var result = repository.Execute(() =>
{
    return DoWork();
}, useTransaction: true);

// Асинхронное выполнение без результата
await repository.ExecuteAsync(async () =>
{
    // асинхронная логика
}, useTransaction: true);

// Асинхронное выполнение с результатом
var result = await repository.ExecuteAsync(async () =>
{
    return await DoWorkAsync();
}, useTransaction: true);
```

### 10. Доступ к IQueryable (Set)

Метод `Set()` предоставляет прямой доступ к `IQueryable<TEntity>` с опциональными настройками:

```csharp
var query = repository.Set(options);
```

### 11. Явное сохранение изменений (SaveChanges)

```csharp
// Синхронное
repository.SaveChanges();

// Асинхронное
await repository.SaveChangesAsync(cancellationToken);
```

## Примеры использования

### Базовое CRUD

```csharp
public class UserService
{
    private readonly IRepository<User> _repository;

    public UserService(IRepository<User> repository)
    {
        _repository = repository;
    }

    public async Task<User?> GetUserById(Guid id)
    {
        return await _repository.GetAsync(id);
    }

    public async Task<List<User>> GetActiveUsers(int page, int pageSize)
    {
        var spec = new ActiveUsersSpecification();
        return await _repository.GetRangeAsync(
            spec,
            skip: (page - 1) * pageSize,
            take: pageSize
        );
    }

    public async Task<User> CreateUser(CreateUserRequest request)
    {
        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            IsActive = true
        };

        return await _repository.AddAsync(user, userId: request.CreatedBy, userName: request.CreatedByName);
    }

    public async Task DeleteUser(Guid userId)
    {
        var user = await _repository.GetAsync(userId);
        if (user != null)
        {
            await _repository.RemoveAsync(user);
        }
    }
}
```

### Массовое обновление

```csharp
// Безопасное обновление по условию — ВСЕГДА указывайте predicate
await repository.ExecuteUpdateRangeAsync(
    x => x.Status == OrderStatus.Pending,
    IRepository<Order>.GetUpdateRangeAsyncLambdaFunc(x => x.Status, x => OrderStatus.Cancelled),
    IRepository<Order>.GetUpdateRangeAsyncLambdaFunc(x => x.CancelledAt, x => DateTime.UtcNow));

// Обновление по QueryOptions
var options = new QueryOptions<Order>().AddFilter(o => o.TotalAmount > 10000);
await repository.ExecuteUpdateRangeAsync(
    options,
    IRepository<Order>.GetUpdateRangeAsyncLambdaFunc(x => x.Priority, x => OrderPriority.High));

// Обновление по спецификации
await repository.ExecuteUpdateRangeAsync(
    new HighValueOrdersSpecification(),
    IRepository<Order>.GetUpdateRangeAsyncLambdaFunc(x => x.RequiresReview, x => true));
```

> **⚠️ Напоминание:** вызов `ExecuteUpdateRangeAsync()` **без параметра** `predicate` (и без `options`/`specification`) обновит **все** строки таблицы. Никогда не полагайтесь на `predicate = null` как «обновить всё» — пишите явное выражение.

### Использование с транзакциями

```csharp
public async Task TransferFunds(Guid fromAccountId, Guid toAccountId, decimal amount)
{
    await _repository.ExecuteAsync(async () =>
    {
        var fromAccount = await _repository.GetAsync(fromAccountId);
        var toAccount = await _repository.GetAsync(toAccountId);

        if (fromAccount.Balance < amount)
        {
            throw new InsufficientFundsException();
        }

        fromAccount.Balance -= amount;
        toAccount.Balance += amount;

        await _repository.SaveChangesAsync();

        return true;
    }, useTransaction: true);
}
```

### Расширенная фильтрация и сортировка

```csharp
var options = new QueryOptions<Order>(withTracking: false, asSplitQuery: true);
options
    .AddFilter(o => o.Status == OrderStatus.Pending)
    .AddFilter(o => o.CreatedAt > DateTime.UtcNow.AddDays(-7))
    .AddOrderBy(o => o.TotalAmount, OrderDirectionType.Descending)
    .AddOrderBy(o => o.CreatedAt, OrderDirectionType.Ascending)
    .AddInclude(o => o.Customer)
    .AddInclude(o => o.Items);

var orders = await _repository.GetRangeAsync(
    options,
    skip: 0,
    take: 100
);
```

### Проекция (Select)

```csharp
// Проекция с selector
var userDtos = await _repository.GetRangeAsync<UserDto>(
    options: null,
    skip: 0,
    take: 50,
    selector: u => new UserDto { Id = u.Id, Name = u.Name }
);

// FirstOrDefault с проекцией
var userDto = await _repository.FirstOrDefaultAsync<UserDto>(
    options: null,
    selector: u => new UserDto { Id = u.Id, Name = u.Name }
);
```

### Агрегация

```csharp
var count = await _repository.CountAsync(options);
var exists = await _repository.AnyAsync(options);
var totalAmount = await _repository.SumAsync(o => o.TotalAmount, options);
```

### Группировка

Группировка элементов по ключу с опциональной пагинацией и сортировкой:

```csharp
// Простая группировка без сортировки и пагинации
Expression<Func<Product, int>> keySelector = p => p.CategoryId;
var groups = await _repository.GetGroupingAsync(keySelector, cancellationToken: cancellationToken);

// Группировка с проекцией и сортировкой групп по ключу
Expression<Func<IGrouping<int, Product>, CategorySummary>> selector =
    g => new CategorySummary(g.Key, g.Count());
var summaries = await _repository.GetGroupingAsync(
    keySelector: keySelector,
    selector: selector,
    groupKeyOrderDirection: OrderDirectionType.Ascending,
    cancellationToken: cancellationToken);

// Пагинация групп (skip/take ОБЯЗАТЕЛЬНО требует groupKeyOrderDirection)
var page = await _repository.GetGroupingAsync(
    keySelector: keySelector,
    skip: 0,
    take: 10,
    groupKeyOrderDirection: OrderDirectionType.Ascending,
    cancellationToken: cancellationToken);
```

> **⚠️ При использовании `skip`/`take` параметр `groupKeyOrderDirection` обязателен** — иначе порядок групп недетерминирован, и `EfRepository` бросает `ArgumentException`.
>
> **Трансляция `OrderBy(g => g.Key)` в SQL** зависит от провайдера: Npgsql и MSSQL поддерживают; SQLite и InMemory-провайдер — нет (соответствующие интеграционные тесты помечены `[Fact(Skip = "...")]` в `EfRepositoryIntegrationTests.GetGroupingAsync_AscendingOrder_*` / `DescendingOrder_*`).

Количество уникальных ключей группировки:

```csharp
var groupCount = await _repository.CountGroupsAsync(keySelector, options, cancellationToken);
var groupCountBySpec = await _repository.CountGroupsAsync(keySelector, new HighValueOrdersSpecification(), cancellationToken);
```

Максимум проекции:

```csharp
Expression<Func<Order, decimal>> maxSelector = o => o.TotalAmount;
var max = await _repository.MaxAsync(maxSelector, cancellationToken);
var maxFiltered = await _repository.MaxAsync(maxSelector, options, cancellationToken);
var maxBySpec = await _repository.MaxAsync(maxSelector, new HighValueOrdersSpecification(), cancellationToken);
```

> **Примечание:** `MaxAsync` для пустой выборки возвращает `default(TOut)` (в реализации `EfRepository` используется `DefaultIfEmpty()`).

## Реализация

Базовая реализация `EfRepository<T>` находится в `Shared.Infrastructure.Dal.EFCore.Repository.EfRepository` и использует Entity Framework Core для доступа к данным.

### Расширения репозитория

Фреймворк предоставляет дополнительные методы расширения в `Shared.Domain.Core.Dal.Repository.Extensions.RepositoryExtensions`:

```csharp
// Получение сущности или выброс NotFoundException (по одиночному id)
var user = await repository.GetByIdOrThrowAsync<User, Guid>(userId);

// Получение сущности с проекцией или выброс NotFoundException
var userDto = await repository.GetByIdOrThrowAsync<User, Guid, UserDto>(userId, selector: u => new UserDto { ... });

// Получение коллекции по массиву id или выброс NotFoundException
//   если хотя бы один id не найден
var users = await repository.GetByIdOrThrowAsync<User, Guid>(
    ids: new[] { userId1, userId2 },
    options: new QueryOptions<Person>(withTracking: true));

// Обновление навигационных свойств коллекции сущностей на основе DTO.
// Существует две перегрузки:
//   1) Простая — по DTO + IEntity<Guid> навигациям и comparisonFunc
//   2) Расширенная — с явным filter / destComparisonFunc / navDestComparisonFunc / dtoSelector
await repository.UpdateNavigationPropertiesAsync<UserDto, RoleDto, Role>(
    entities: users,
    navigationDtos: roleDtos,
    comparisonFunc: (user, role) => user.Id == role.UserId,
    mapper: _mapper,
    addAction: (user, role) => user.Roles.Add(role),
    removeAction: (user, role) => user.Roles.Remove(role));

// Пакетная обработка через репозиторий
await repository.ProcessBatchesAsync(
    options,
    batchSize: 100,
    processBatchAction: async batch =>
    {
        foreach (var entity in batch)
        {
            // Обработка сущности
        }
    });

// Поиск сущностей по именам (с фильтрацией по ID)
var existingEntities = await repository.FindEntitiesByNamesAsync(
    ids, nameFilter, cancellationToken);
```

> **Примечание о `ProcessBatchesAsync`:** в отличие от `BatchHelper.ProcessBatchesAsync`, который **не** пробрасывает `CancellationToken` в `getBatchFunc` (см. `Shared.Common.Batch.BatchHelper.cs:64-70`), `RepositoryExtensions.ProcessBatchesAsync` использует перегрузку `BatchSelectAsync` **с** `cancellationToken` в `getBatchFunc`, поэтому отмена прерывает и `GetRangeAsync` тоже. Это безопаснее при работе с БД.

## Best Practices

1. **Используйте спецификации для сложной бизнес-логики выборки**
   - Инкапсулируйте критерии в отдельные классы спецификаций
   - Делайте спецификации переиспользуемыми и тестируемыми

2. **Применяйте `WithTracking` осознанно**
   - По умолчанию `WithTracking = false` — чтение без отслеживания
   - Включайте отслеживание только для сущностей, которые будут изменены

3. **Ограничивайте количество возвращаемых записей**
   - Всегда используйте `skip`/`take` для пагинации
   - Избегайте загрузки больших объёмов данных в память

4. **Используйте проекции для DTO**
   - Применяйте `selector` для эффективной загрузки только нужных полей

5. **Оборачивайте несколько операций в транзакцию**
   - Используйте `ExecuteAsync` с `useTransaction: true` для атомарности

6. **Используйте `SumAsync` для агрегации вместо загрузки всех данных**
   - Проекция вычисляется на стороне БД

7. **Всегда задавайте условие в `ExecuteUpdateRangeAsync`**
   - `predicate = null` обновляет все строки таблицы. Передавайте явный `Expression<Func<TEntity, bool>>`.

8. **Реализуйте маркерные интерфейсы для аудита**
   - Если нужны `CreatedBy/DateCreated` или soft delete — добавьте сущности `: IWithCreated, IWithDeleted`. Без них `AddAsync`/`RemoveAsync` не вызывают автозаполнение.

## См. также

| Документ | Описание |
|----------|----------|
| [Unit of Work](unit-of-work.md) | Координация транзакций и доменных событий |
| [Specification Pattern](specification.md) | Инкапсуляция критериев выборки |
| [Filtering & Sorting Guide](filtering-sorting-guide.md) | Подробное руководство по фильтрации и сортировке |
| [CQRS](cqrs.md) | Разделение команд и запросов |
| [Entity Interfaces](entity-interfaces.md) | Маркерные интерфейсы `IWithCreated`, `IWithDeleted` и т.п. |
| [EF Core Internals](efcore-internals.md) | Внутренние механизмы `EfRepository`, `EfUnitOfWork`, регистрация `IRepository<>` → `EfRepository<>` (Scoped) |
