# Repository Pattern

## Обзор

Repository Pattern в фреймворке Shared предоставляет централизованный доступ к данным через универсальный интерфейс `IRepository<TEntity>`. Этот паттерн абстрагирует работу с источником данных, обеспечивая тестируемость и соблюдение принципа единой ответственности.

## Интерфейс IRepository<TEntity>

Базовый интерфейс репозитория определен в `Shared.Domain.Core.Dal.Repository.Interfaces.IRepository`:

```csharp
public interface IRepository<TEntity> where TEntity : class, IEntity
{
    // Методы чтения
    Task<TEntity?> GetAsync(object? id, QueryOptions<TEntity>? options = null, CancellationToken cancellationToken = default);
    Task<List<TEntity>> GetRangeAsync(QueryOptions<TEntity>? options = null, int? skip = null, int? take = null, CancellationToken cancellationToken = default);
    Task<TEntity?> FirstOrDefaultAsync(QueryOptions<TEntity>? options = null, CancellationToken cancellationToken = default);
    Task<TEntity?> SingleOrDefaultAsync(QueryOptions<TEntity>? options = null, CancellationToken cancellationToken = default);
    Task<TEntity?> LastOrDefaultAsync(QueryOptions<TEntity>? options = null, CancellationToken cancellationToken = default);

    // Агрегация
    Task<int> CountAsync(QueryOptions<TEntity>? options = null, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(QueryOptions<TEntity>? options = null, CancellationToken cancellationToken = default);
    Task<TAccumulate> AggregateAsync<TAccumulate>(Func<TEntity, TAccumulate, TAccumulate> accumulator, TAccumulate seed, ...);

    // Команды
    Task<TEntity> AddAsync(TEntity entity, Guid? userId = default, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<TEntity> entities, Guid? userId = default, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, Guid? userId = default, CancellationToken cancellationToken = default);
    Task UpdateRangeAsync(Expression<Func<TEntity, bool>>? condition, Action<TEntity> updateAction, Guid? userId = default, ...);
    Task RemoveAsync(TEntity entity, bool hard = false, Guid? userId = default, CancellationToken cancellationToken = default);
    Task RemoveRangeAsync(IEnumerable<TEntity> entities, bool hard = false, Guid? userId = default, CancellationToken cancellationToken = default);

    // Транзакции
    Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> process, bool useTransaction = false, CancellationToken cancellationToken = default);
}
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

`QueryOptions<T>` предоставляет расширенные возможности для настройки запросов:

```csharp
var options = new QueryOptions<User>
{
    Filter = u => u.IsActive,
    OrderBy = new[] { QueryOrderByOption.Desc(u => u.CreatedAt) },
    Includes = new Expression<Func<User, object>>[] { u => u.Profile },
    AsSplitQuery = true,
    AsNoTracking = true
};

var users = await repository.GetRangeAsync(options, skip: 0, take: 50);
```

**Доступные опции:**
- `Filter` — выражение фильтрации
- `OrderBy` / `OrderByDescending` — сортировка
- `ThenBy` / `ThenByDescending` — дополнительная сортировка
- `Includes` — связанные сущности для загрузки
- `AsSplitQuery` — использование split queries для includes
- `AsNoTracking` — чтение без отслеживания изменений
- `IgnoreQueryFilters` — игнорирование глобальных фильтров
- `DisableValidation` — отключение валидации

### 3. Projection на лету

Поддержка проекции данных в DTO без промежуточного маппинга:

```csharp
// Проекция в анонимный тип или DTO
var userDtos = await repository.GetRangeAsync(
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

### 4. Soft/Hard Delete

Репозиторий поддерживает мягкое удаление через флаг `hard`:

```csharp
// Soft delete (по умолчанию) - устанавливает IsDeleted = true
await repository.RemoveAsync(user);

// Hard delete - физическое удаление из БД
await repository.RemoveAsync(user, hard: true);
```

### 5. Audit Fields

Автоматическое заполнение аудиторских полей при операциях записи:

```csharp
// userId и userName автоматически записываются в CreatedBy/ModifiedBy
await repository.AddAsync(entity, userId: currentUserId);
await repository.UpdateAsync(entity, userId: currentUserId);
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

        return await _repository.AddAsync(user, userId: request.CreatedBy);
    }

    public async Task UpdateUserEmail(Guid userId, string newEmail)
    {
        await _repository.UpdateAsync(
            new User { Id = userId, Email = newEmail },
            userId: GetCurrentUserId()
        );
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

        await _repository.UpdateAsync(fromAccount);
        await _repository.UpdateAsync(toAccount);

        return true;
    }, useTransaction: true);
}
```

### Расширенная фильтрация и сортировка

```csharp
var options = new QueryOptions<Order>
{
    Filter = o => o.Status == OrderStatus.Pending && o.CreatedAt > DateTime.UtcNow.AddDays(-7),
    OrderBy = new[] { QueryOrderByOption.Desc(o => o.TotalAmount) },
    ThenBy = new[] { QueryOrderByOption.Asc(o => o.CreatedAt) },
    Includes = new Expression<Func<Order, object>>[]
    {
        o => o.Customer,
        o => o.Items
    },
    AsSplitQuery = true
};

var orders = await _repository.GetRangeAsync(
    options,
    skip: 0,
    take: 100
);
```

## Реализация

Базовая реализация `EfRepository<T>` находится в `Shared.Infrastructure.Dal.EFCore.Repository.EfRepository` и использует Entity Framework Core для доступа к данным.

### Расширения репозитория

Фреймворк предоставляет дополнительные методы расширения в `RepositoryExtensions`:

```csharp
// Пагинация
var pagedResult = await repository.GetPagedAsync(specification, page: 1, pageSize: 20);

// Пакетная обработка
await repository.ProcessInBatchesAsync(
    specification,
    batchSize: 100,
    processBatch: async batch =>
    {
        foreach (var entity in batch)
        {
            // Обработка сущности
        }
    }
);
```

## Best Practices

1. **Используйте спецификации для сложной бизнес-логики выборки**
   - Инкапсулируйте критерии в отдельные классы спецификаций
   - Делайте спецификации переиспользуемыми и тестируемыми

2. **Применяйте AsNoTracking для операций чтения**
   - Улучшает производительность при чтении без последующего изменения

3. **Ограничивайте количество возвращаемых записей**
   - Всегда используйте `skip`/`take` для пагинации
   - Избегайте загрузки больших объемов данных в память

4. **Используйте проекции для DTO**
   - Применяйте `selector` для эффективной загрузки только нужных полей

5. **Оборачивайте несколько операций в транзакцию**
   - Используйте `ExecuteAsync` с `useTransaction: true` для атомарности

## См. также

| Документ | Описание |
|----------|----------|
| [Unit of Work](unit-of-work.md) | Координация транзакций и доменных событий |
| [Specification Pattern](specification.md) | Инкапсуляция критериев выборки |
| [Filtering & Sorting Guide](filtering-sorting-guide.md) | Подробное руководство по фильтрации и сортировке |
| [CQRS](cqrs.md) | Разделение команд и запросов