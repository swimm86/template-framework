# Repository Pattern

## Обзор

Repository Pattern в фреймворке Shared предоставляет централизованный доступ к данным через универсальный интерфейс `IRepository<TEntity>`. Этот паттерн абстрагирует работу с источником данных, обеспечивая тестируемость и соблюдение принципа единой ответственности.

## Интерфейс IRepository<TEntity>

Базовый интерфейс репозитория определен в `Shared.Domain.Core.Dal.Repository.Interfaces.IRepository`:

```csharp
public interface IRepository<TEntity> where TEntity : class, IEntity
{
    // Чтение — по идентификатору
    Task<TEntity?> GetAsync(object? id, QueryOptions<TEntity>? options = null, CancellationToken cancellationToken = default);
    Task<TEntity?> GetAsync(object? id, ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
    Task<TOut?> GetAsync<TOut>(object? id, QueryOptions<TEntity>? options = null, Expression<Func<TEntity, TOut>>? selector = default, CancellationToken cancellationToken = default);
    Task<TOut?> GetAsync<TOut>(object? id, ISpecification<TEntity> specification, Expression<Func<TEntity, TOut>>? selector = default, CancellationToken cancellationToken = default);

    // Чтение — коллекции
    Task<List<TEntity>> GetRangeAsync(QueryOptions<TEntity>? options = null, int? skip = null, int? take = null, CancellationToken cancellationToken = default);
    Task<List<TEntity>> GetRangeAsync(ISpecification<TEntity> specification, int? skip = null, int? take = null, CancellationToken cancellationToken = default);
    Task<List<TOut>> GetRangeAsync<TOut>(QueryOptions<TEntity>? options = null, int? skip = null, int? take = null, Expression<Func<TEntity, TOut>>? selector = default, CancellationToken cancellationToken = default);
    Task<List<TOut>> GetRangeAsync<TOut>(ISpecification<TEntity> specification, int? skip = null, int? take = null, Expression<Func<TEntity, TOut>>? selector = default, CancellationToken cancellationToken = default);

    // Чтение — FirstOrDefault
    Task<TEntity?> FirstOrDefaultAsync(QueryOptions<TEntity>? options = null, CancellationToken cancellationToken = default);
    Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
    Task<TOut?> FirstOrDefaultAsync<TOut>(QueryOptions<TEntity>? options = null, Expression<Func<TEntity, TOut>>? selector = default, CancellationToken cancellationToken = default);
    Task<TOut?> FirstOrDefaultAsync<TOut>(ISpecification<TEntity> specification, Expression<Func<TEntity, TOut>>? selector = default, CancellationToken cancellationToken = default);

    // Чтение — SingleOrDefault
    Task<TEntity?> SingleOrDefaultAsync(QueryOptions<TEntity>? options = null, CancellationToken cancellationToken = default);
    Task<TEntity?> SingleOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
    Task<TOut?> SingleOrDefaultAsync<TOut>(QueryOptions<TEntity>? options = null, Expression<Func<TEntity, TOut>>? selector = default, CancellationToken cancellationToken = default);
    Task<TOut?> SingleOrDefaultAsync<TOut>(ISpecification<TEntity> specification, Expression<Func<TEntity, TOut>>? selector = default, CancellationToken cancellationToken = default);

    // Чтение — LastOrDefault
    Task<TEntity?> LastOrDefaultAsync(QueryOptions<TEntity>? options = null, CancellationToken cancellationToken = default);
    Task<TEntity?> LastOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
    Task<TOut?> LastOrDefaultAsync<TOut>(QueryOptions<TEntity>? options = null, Expression<Func<TEntity, TOut>>? selector = default, CancellationToken cancellationToken = default);
    Task<TOut?> LastOrDefaultAsync<TOut>(ISpecification<TEntity> specification, Expression<Func<TEntity, TOut>>? selector = default, CancellationToken cancellationToken = default);

    // Агрегация
    Task<int> CountAsync(QueryOptions<TEntity>? options = null, CancellationToken cancellationToken = default);
    Task<int> CountAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(QueryOptions<TEntity>? options = null, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
    Task<decimal> SumAsync(Expression<Func<TEntity, decimal>> selector, QueryOptions<TEntity>? options = null, CancellationToken cancellationToken = default);
    Task<decimal> SumAsync(Expression<Func<TEntity, decimal>> selector, ISpecification<TEntity> specification, CancellationToken cancellationToken = default);

    // Добавление
    Task<TEntity> AddAsync(TEntity entity, Guid? userId = default, string? userName = default, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<TEntity> entities, Guid? userId = default, string? userName = default, CancellationToken cancellationToken = default);

    // Обновление
    Task UpdateRangeAsync(Expression<Func<TEntity, bool>>? condition = default, params (LambdaExpression propertyExpression, LambdaExpression valueExpression)[] updateData);
    Task UpdateRangeAsync(QueryOptions<TEntity> options, params (LambdaExpression propertyExpression, LambdaExpression valueExpression)[] updateData);
    Task UpdateRangeAsync(ISpecification<TEntity> specification, params (LambdaExpression propertyExpression, LambdaExpression valueExpression)[] updateData);

    // Удаление
    Task RemoveAsync(TEntity entity, Guid? userId, bool hard = false, CancellationToken cancellationToken = default);
    Task RemoveAsync(TEntity entity, bool hard = false, CancellationToken cancellationToken = default);
    Task RemoveRangeAsync(IEnumerable<TEntity> entities, bool hard = false, CancellationToken cancellationToken = default);
    Task RemovePermanentRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    Task RemoveRangeAsync(QueryOptions<TEntity> options, bool hard = false, CancellationToken cancellationToken = default);
    Task RemoveRangeAsync(Expression<Func<TEntity, bool>> conditions, bool hard = false, CancellationToken cancellationToken = default);
    Task RemoveRangeAsync(ISpecification<TEntity> specification, bool hard = false, CancellationToken cancellationToken = default);

    // Выполнение операций
    void Execute(Action process, bool useTransaction = false);
    TResult Execute<TResult>(Func<TResult> process, bool useTransaction = false);
    Task ExecuteAsync(Func<Task> process, bool useTransaction = false, CancellationToken cancellationToken = default);
    Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> process, bool useTransaction = false, CancellationToken cancellationToken = default);

    // Доступ к IQueryable
    IQueryable<TEntity> Set(QueryOptions<TEntity>? options = null);

    // Сохранение изменений
    void SaveChanges();
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

### Вспомогательный метод для UpdateRangeAsync

Для формирования кортежей `(LambdaExpression, LambdaExpression)` используется статический метод:

```csharp
public static (LambdaExpression, LambdaExpression) GetUpdateRangeAsyncLambdaFunc<TProp>(
    Expression<Func<TEntity, TProp>> propertyExpression,
    Expression<Func<TEntity, TProp>> valueExpression)
```

**Пример использования:**
```csharp
await repository.UpdateRangeAsync(
    x => x.Id == Guid.Parse("4c2ca6bf-8c8f-4cbf-9097-d40615241a2c"),
    repository.GetUpdateRangeAsyncLambdaFunc(x => x.Name, x => "name2"),
    repository.GetUpdateRangeAsyncLambdaFunc(x => x.PpsVersionName, x => "ppsVersionName 2"));
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
| `WithTracking` | `bool` | Признак отслеживания изменений сущностей |
| `AsSplitQuery` | `bool` | Использование split queries для includes |
| `Distinct` | `bool` | Признак исключения дублей |
| `DistinctBy` | `Expression<Func<TEntity, bool>>?` | Условие для исключения дублей |
| `CustomQueryPostProcesses` | `List<Func<IQueryable<TEntity>, IQueryable<TEntity>>>` | Пользовательские пост-преобразования IQueryable |
| `CustomQueryBeforeProcesses` | `List<Func<IQueryable<TEntity>, IQueryable<TEntity>>>` | Пользовательские пре-преобразования IQueryable |

**Методы QueryOptions:**

| Метод | Описание |
|-------|----------|
| `AddFilter(Expression)` | Добавление фильтра |
| `AddFilterIf(bool, Expression)` | Условное добавление фильтра |
| `AddOrderBy(Expression, OrderDirectionType, int?)` | Добавление сортировки |
| `AddOrderBy(SortOption)` | Добавление сортировки из SortOption |
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

**OrderDirectionType** (перечисление):

| Значение | Описание |
|----------|----------|
| `Ascending` | По возрастанию (description: "asc") |
| `Descending` | По убыванию (description: "desc") |

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

Реализация — `Includable<TSrcEntity, TDstEntity>` поддерживает вложенные includes через `SetChild`.

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

Репозиторий поддерживает мягкое и физическое удаление:

```csharp
// Soft delete (по умолчанию) - устанавливает IsDeleted = true
await repository.RemoveAsync(user);

// Hard delete - физическое удаление из БД
await repository.RemoveAsync(user, hard: true);

// RemoveAsync с userId для аудита
await repository.RemoveAsync(user, userId: currentUserId);

// Физическое удаление коллекции (без soft delete)
await repository.RemovePermanentRangeAsync(entities);
```

### 7. Audit Fields

Автоматическое заполнение аудиторских полей при операциях записи:

```csharp
// userId и userName автоматически записываются в CreatedBy/ModifiedBy
await repository.AddAsync(entity, userId: currentUserId, userName: currentUserName);
await repository.AddRangeAsync(entities, userId: currentUserId, userName: currentUserName);
```

### 8. Выполнение операций (Execute / ExecuteAsync)

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

### 9. Доступ к IQueryable (Set)

Метод `Set()` предоставляет прямой доступ к `IQueryable<TEntity>` с опциональными настройками:

```csharp
var query = repository.Set(options);
```

### 10. Явное сохранение изменений (SaveChanges)

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
// Обновление по условию
await repository.UpdateRangeAsync(
    x => x.Status == OrderStatus.Pending,
    repository.GetUpdateRangeAsyncLambdaFunc(x => x.Status, x => OrderStatus.Cancelled),
    repository.GetUpdateRangeAsyncLambdaFunc(x => x.CancelledAt, x => DateTime.UtcNow));

// Обновление по QueryOptions
var options = new QueryOptions<Order>().AddFilter(o => o.TotalAmount > 10000);
await repository.UpdateRangeAsync(
    options,
    repository.GetUpdateRangeAsyncLambdaFunc(x => x.Priority, x => OrderPriority.High));

// Обновление по спецификации
await repository.UpdateRangeAsync(
    new HighValueOrdersSpecification(),
    repository.GetUpdateRangeAsyncLambdaFunc(x => x.RequiresReview, x => true));
```

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

## Реализация

Базовая реализация `EfRepository<T>` находится в `Shared.Infrastructure.Dal.EFCore.Repository.EfRepository` и использует Entity Framework Core для доступа к данным.

### Расширения репозитория

Фреймворк предоставляет дополнительные методы расширения в `RepositoryExtensions`:

```csharp
// Получение сущности или выброс NotFoundException
var user = await repository.GetByIdOrThrowAsync<User, Guid>(userId);

// Получение сущности с проекцией или выброс NotFoundException
var userDto = await repository.GetByIdOrThrowAsync<User, Guid, UserDto>(userId, selector: u => new UserDto { ... });

// Пакетная обработка
await repository.ProcessBatchesAsync(
    options,
    batchSize: 100,
    processBatchAction: async batch =>
    {
        foreach (var entity in batch)
        {
            // Обработка сущности
        }
    }
);

// Поиск сущностей по именам (с фильтрацией по ID)
var existingEntities = await repository.FindEntitiesByNamesAsync(
    ids, nameFilter, cancellationToken);
```

## Best Practices

1. **Используйте спецификации для сложной бизнес-логики выборки**
   - Инкапсулируйте критерии в отдельные классы спецификаций
   - Делайте спецификации переиспользуемыми и тестируемыми

2. **Применяйте WithTracking осознанно**
   - По умолчанию `WithTracking = false` — чтение без отслеживания
   - Включайте отслеживание только для сущностей, которые будут изменены

3. **Ограничивайте количество возвращаемых записей**
   - Всегда используйте `skip`/`take` для пагинации
   - Избегайте загрузки больших объемов данных в память

4. **Используйте проекции для DTO**
   - Применяйте `selector` для эффективной загрузки только нужных полей

5. **Оборачивайте несколько операций в транзакцию**
   - Используйте `ExecuteAsync` с `useTransaction: true` для атомарности

6. **Используйте SumAsync для агрегации вместо загрузки всех данных**
   - Проекция вычисляется на стороне БД

## См. также

| Документ | Описание |
|----------|----------|
| [Unit of Work](unit-of-work.md) | Координация транзакций и доменных событий |
| [Specification Pattern](specification.md) | Инкапсуляция критериев выборки |
| [Filtering & Sorting Guide](filtering-sorting-guide.md) | Подробное руководство по фильтрации и сортировке |
| [CQRS](cqrs.md) | Разделение команд и запросов |