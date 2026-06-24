# Unit of Work Pattern

## Обзор

Unit of Work (UoW) — это паттерн, который координирует работу нескольких репозиториев в рамках одной бизнес-транзакции, обеспечивая атомарность операций и согласованность данных. В фреймворке Shared реализован через интерфейс `IUnitOfWork`.

**Assembly / Namespace:** `Shared.Domain.Core.Dal.UnitOfWork.Interfaces` (контракт) и `Shared.Infrastructure.Dal.EFCore.EfUnitOfWork<TDbContext>` (реализация).

## Интерфейс IUnitOfWork

Базовый интерфейс определён в `Shared.Domain.Core.Dal.UnitOfWork.Interfaces.IUnitOfWork`:

```csharp
public interface IUnitOfWork : IDisposable
{
    // Получение репозитория
    IRepository<TEntity> GetRepository<TEntity>() where TEntity : class, IEntity;

    // Сохранение изменений
    int SaveChanges(bool commitTransaction = true, bool resetLifecycleActionSettingsAfterSave = true);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default, bool commitTransaction = true, bool resetLifecycleActionSettingsAfterSave = true);

    // Управление транзакциями
    Task CommitTransactionAsync(CancellationToken cancellationToken);
    Task RollbackTransactionAsync(CancellationToken cancellationToken);
    Task ResetTransactionAsync(CancellationToken cancellationToken);
    IUnitOfWork EnableTransaction();
    IUnitOfWork DisableTransaction();

    // Управление отслеживанием
    void ClearTracking();
}
```

> **⚠️ Критично про `SaveChangesAsync(commitTransaction: true)`.**  
> В `EfUnitOfWork` параметр `commitTransaction` имеет дефолт `true` (см. `EfUnitOfWork.cs:106-110`). Это означает, что **любой** `await _unitOfWork.SaveChangesAsync()` **сразу** делает `CommitTransactionAsync` + `ResetTransactionAsync` (см. блок `finally` в `SaveChangesAsync`). После такого `SaveChangesAsync` текущая транзакция уже зафиксирована и заменена на новую (или вообще сброшена, если транзакции отключены). Для **многошаговой** транзакции, где `SaveChanges` вызывается несколько раз **до** финального коммита, передавайте `commitTransaction: false` явно на каждом промежуточном вызове. Иначе первый же `SaveChanges` завершит всю транзакцию.

## Ключевые возможности

### 1. Централизованное получение репозиториев

`IUnitOfWork` инжектируется в Scoped-scope через DI. Все репозитории создаются в рамках одного контекста данных:

```csharp
var userRepo = _unitOfWork.GetRepository<User>();
var orderRepo = _unitOfWork.GetRepository<Order>();
var productRepo = _unitOfWork.GetRepository<Product>();

// Все операции выполняются в рамках одного DbContext
```

### 2. Атомарное сохранение изменений

Для многошаговых операций (несколько `SaveChanges` внутри одной транзакции) используйте `commitTransaction: false` на промежуточных вызовах:

```csharp
public async Task CreateOrder(CreateOrderRequest request)
{
    var userRepo = _unitOfWork.GetRepository<User>();
    var orderRepo = _unitOfWork.GetRepository<Order>();
    var productRepo = _unitOfWork.GetRepository<Product>();

    // Проверка пользователя
    var user = await userRepo.GetAsync(request.UserId);
    if (user == null)
    {
        throw new NotFoundException("User not found");
    }

    // Проверка и резервирование товаров
    foreach (var item in request.Items)
    {
        // Change tracking: модифицируем загруженную сущность, а не вызываем UpdateAsync —
        // у IRepository<T> нет метода UpdateAsync; обновление идёт через SaveChangesAsync
        // при условии, что сущность была прочитана с withTracking: true.
        var options = new QueryOptions<Product>(withTracking: true);
        var product = await productRepo.GetAsync(item.ProductId, options);
        if (product.Stock < item.Quantity)
        {
            throw new InvalidOperationException($"Insufficient stock for product {product.Name}");
        }
        product.Stock -= item.Quantity;
    }

    // Создание заказа
    var order = new Order
    {
        UserId = user.Id,
        TotalAmount = request.Items.Sum(i => i.Quantity * i.Price),
        Status = OrderStatus.Pending
    };

    await orderRepo.AddAsync(order);

    // Один SaveChanges в конце — дефолтный commitTransaction: true сразу коммитит транзакцию
    await _unitOfWork.SaveChangesAsync();
}
```

> **Альтернатива для bulk-операций:** если нужно обновить много продуктов одним SQL-запросом без загрузки в память, используйте `productRepo.ExecuteUpdateRangeAsync(x => productIds.Contains(x.Id), IRepository<Product>.GetUpdateRangeAsyncLambdaFunc(x => x.Stock, x => x.Stock - delta))`.

### 3. Явное управление транзакциями

Контроль над транзакциями с возможностью commit/rollback. **Не** вызывайте `SaveChangesAsync()` без `commitTransaction: false` между `EnableTransaction()` и `CommitTransactionAsync()` — это завершит транзакцию преждевременно.

```csharp
public async Task TransferMoney(Guid fromAccountId, Guid toAccountId, decimal amount)
{
    _unitOfWork.EnableTransaction();

    try
    {
        var accountRepo = _unitOfWork.GetRepository<Account>();

        var options = new QueryOptions<Account>(withTracking: true);
        var fromAccount = await accountRepo.GetAsync(fromAccountId, options);
        var toAccount = await accountRepo.GetAsync(toAccountId, options);

        if (fromAccount.Balance < amount)
        {
            throw new InsufficientFundsException();
        }

        fromAccount.Balance -= amount;
        toAccount.Balance += amount;

        // commitTransaction: false — НЕ коммитим промежуточный save,
        // транзакция ждёт финального CommitTransactionAsync()
        await _unitOfWork.SaveChangesAsync(commitTransaction: false);

        await _unitOfWork.CommitTransactionAsync();
    }
    catch
    {
        await _unitOfWork.RollbackTransactionAsync();
        throw;
    }
}
```

### 4. Управление действиями перехвата

Управление активностью lifecycle-действий вынесено из `IUnitOfWork` в `ILifecycleActionOrchestrator` (см. [Lifecycle Actions](lifecycle-actions.md)). `EfUnitOfWork` интегрирован с оркестратором автоматически.

```csharp
public class MyService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILifecycleActionOrchestrator _orchestrator;

    public async Task BulkImportAsync(IEnumerable<Person> people)
    {
        // Глобальное отключение действий на время операции
        _orchestrator.DisableActions();

        try
        {
            var personRepo = _unitOfWork.GetRepository<Person>();
            foreach (var person in people)
            {
                await personRepo.AddAsync(person);
            }
            await _unitOfWork.SaveChangesAsync(); // Действия не будут выполнены
        }
        finally
        {
            // Гарантированный сброс состояния оркестратора даже при исключении
            _orchestrator.ResetAllActions();
        }

        // Отключение конкретной фазы для всех сущностей
        _orchestrator.DisablePhase(LifecyclePhase.AfterSave);
        try
        {
            // ...работа без AfterSave-действий...
        }
        finally
        {
            _orchestrator.EnablePhase(LifecyclePhase.AfterSave);
        }

        // Отключение действия по ключу для конкретной сущности
        var order = await orderRepo.GetAsync(orderId);
        _orchestrator.DisableActionForEntity("SendOrderConfirmation", order);
        try
        {
            // ...работа без уведомления...
        }
        finally
        {
            // Включение обратно
            _orchestrator.EnableActionForEntity("SendOrderConfirmation", order);
        }
    }
}
```

> **Автоматический сброс:** `EfUnitOfWork.SaveChangesAsync` при `resetLifecycleActionSettingsAfterSave: true` (по умолчанию) вызывает `orchestrator.ResetAllActions()` в блоке `finally` (см. `EfUnitOfWork.cs:142-148`). Поэтому явный `try/finally` с `ResetAllActions()` нужен, если между `DisableActions()` и `SaveChangesAsync()` возможно исключение **до** самого `SaveChangesAsync` — иначе состояние не сбросится автоматически.

**Фазы действий перехвата** (см. `Shared.Domain.Core.Enums.LifecyclePhase`):
- `LifecyclePhase.BeforeSave` — действие до сохранения сущности
- `LifecyclePhase.AfterSave` — действие после сохранения сущности

### 5. Управление отслеживанием изменений

Контроль над tracking сущностей в EF Core. **Свойство называется `WithTracking`**, а не `AsNoTracking` — это **прямая** семантика (`true` = отслеживать, `false` = не отслеживать):

```csharp
// Очистка всех отслеживаемых сущностей
uow.ClearTracking();

// Чтение БЕЗ отслеживания (по умолчанию)
var options = new QueryOptions<User>(withTracking: false);
var users = await repo.GetRangeAsync(options);

// Чтение С отслеживанием — для последующего изменения сущности
var trackedOptions = new QueryOptions<User>(withTracking: true);
var trackedUser = await repo.GetAsync(userId, trackedOptions);
trackedUser.Name = "new"; // change tracking в EF Core подхватит изменение
```

## Примеры использования

### Базовый сценарий с использованием

```csharp
public class OrderService
{
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Order> CreateOrder(CreateOrderRequest request)
    {
        var userRepo = _unitOfWork.GetRepository<User>();
        var orderRepo = _unitOfWork.GetRepository<Order>();
        var productRepo = _unitOfWork.GetRepository<Product>();

        // Бизнес-логика...

        var result = await orderRepo.AddAsync(order);
        await _unitOfWork.SaveChangesAsync();

        return result;
    }
}
```

### Транзакция с явным rollback

```csharp
public async Task ProcessRefund(Guid orderId, decimal amount)
{
    _unitOfWork.EnableTransaction();

    try
    {
        var orderRepo = _unitOfWork.GetRepository<Order>();
        var paymentRepo = _unitOfWork.GetRepository<Payment>();

        var options = new QueryOptions<Order>(withTracking: true);
        var order = await orderRepo.GetAsync(orderId, options);
        if (order.Status != OrderStatus.Completed)
        {
            throw new InvalidOperationException("Order is not completed");
        }

        // Создание возврата
        var refund = new Payment
        {
            OrderId = orderId,
            Amount = -amount,
            Type = PaymentType.Refund
        };

        order.Status = OrderStatus.Refunded;

        await paymentRepo.AddAsync(refund);
        // order модифицирован через change tracking — отдельный UpdateAsync не нужен

        // commitTransaction: false — НЕ коммитим, ждём CommitTransactionAsync
        await _unitOfWork.SaveChangesAsync(commitTransaction: false);

        await _unitOfWork.CommitTransactionAsync();
    }
    catch (Exception ex)
    {
        await _unitOfWork.RollbackTransactionAsync();
        // Логирование ошибки
        throw;
    }
}
```

### Пакетная обработка с контролем событий

```csharp
public async Task BulkUpdatePrices(IEnumerable<PriceUpdate> updates)
{
    // Отключаем BeforeSave-действия для массовой операции через orchestrator
    _orchestrator.DisablePhase(LifecyclePhase.BeforeSave);

    try
    {
        var productRepo = _unitOfWork.GetRepository<Product>();

        foreach (var update in updates)
        {
            var options = new QueryOptions<Product>(withTracking: true);
            var product = await productRepo.GetAsync(update.ProductId, options);
            if (product != null)
            {
                product.Price = update.NewPrice;
                product.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _unitOfWork.SaveChangesAsync();
        // resetLifecycleActionSettingsAfterSave = true сбросит orchestrator автоматически
    }
    catch
    {
        // Если исключение дошло до сюда (минуя SaveChanges),
        // принудительно сбрасываем состояние оркестратора
        _orchestrator.ResetAllActions();
        throw;
    }
}
```

### Вложенные транзакции (Reset)

```csharp
public async Task ComplexOperation()
{
    _unitOfWork.EnableTransaction();

    // Первая часть операции
    await PartOneAsync(_unitOfWork);

    // Сброс транзакции для независимой второй части
    await _unitOfWork.ResetTransactionAsync();
    _unitOfWork.EnableTransaction();

    // Вторая часть операции (не зависит от первой)
    await PartTwoAsync(_unitOfWork);

    await _unitOfWork.CommitTransactionAsync();
}
```

## Интеграция с CQRS

Unit of Work естественно интегрируется с CQRS паттерном:

```csharp
// Command Handler
public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, OrderDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateOrderCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<OrderDto> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var orderRepo = _unitOfWork.GetRepository<Order>();

        var order = new Order
        {
            UserId = command.UserId,
            Items = command.Items,
            Status = OrderStatus.Pending
        };

        await orderRepo.AddAsync(order, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<OrderDto>(order);
    }
}
```

## Best Practices

### 1. Не оборачивайте инжектированный `IUnitOfWork` в `using`

`IUnitOfWork` зарегистрирован в DI как **Scoped** (см. `Shared.Infrastructure.Dal.EFCore/Extensions/ServiceCollectionExtensions.cs:57`): dispose происходит автоматически при завершении scope. Повторный dispose инжектированного инстанса приводит к двойному освобождению ресурсов и потенциальным ошибкам в тестах.

```csharp
// Правильно — используем инжектированный инстанс напрямую
public class OrderService
{
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task CreateOrder(CreateOrderRequest request)
    {
        var orderRepo = _unitOfWork.GetRepository<Order>();
        await orderRepo.AddAsync(/* ... */);
        await _unitOfWork.SaveChangesAsync();
    }
}

// Неправильно — двойной dispose инжектированного сервиса
public async Task BadPractice()
{
    using var uow = _unitOfWork; // Scoped-инстанс будет утилизирован здесь
    await uow.SaveChangesAsync();
    // ... позже, при завершении scope, DI повторно вызовет Dispose
}
```

> Если в кодовой базе встречается паттерн `IUnitOfWorkFactory.Create()` (фабрика возвращает новый инстанс) — именно тогда `using` оправдан. Для DI-инжекции — нет.

### 2. Минимизируйте scope транзакции

```csharp
// Правильно - короткая транзакция
public async Task UpdateUserEmail(Guid userId, string email)
{
    var options = new QueryOptions<User>(withTracking: true);
    var repo = _unitOfWork.GetRepository<User>();
    var user = await repo.GetAsync(userId, options);
    user.Email = email;
    await _unitOfWork.SaveChangesAsync();
}

// Неправильно - длительная транзакция включает внешние вызовы
public async Task UpdateUserWithEmailVerification(Guid userId, string email)
{
    var options = new QueryOptions<User>(withTracking: true);
    var repo = _unitOfWork.GetRepository<User>();
    var user = await repo.GetAsync(userId, options);

    // Долгий внешний вызов внутри транзакции!
    await _emailService.SendVerificationEmail(email);

    user.Email = email;
    await _unitOfWork.SaveChangesAsync();
}
```

### 3. Явно указывайте необходимость транзакции

```csharp
// Для операций чтения транзакция не нужна
var user = await repo.GetAsync(id);

// Для операций записи явно включайте транзакцию при необходимости
_unitOfWork.EnableTransaction();
```

### 4. Обрабатывайте исключения корректно

```csharp
try
{
    await _unitOfWork.SaveChangesAsync();
}
catch
{
    await _unitOfWork.RollbackTransactionAsync();
    throw; // Передаём исключение выше
}
```

### 5. Используйте ClearTracking для длинных процессов

```csharp
foreach (var batch in batches)
{
    foreach (var item in batch)
    {
        await ProcessItemAsync(_unitOfWork, item);
    }

    await _unitOfWork.SaveChangesAsync();
    _unitOfWork.ClearTracking(); // Освобождаем память
}
```

### 6. Помните о дефолте `commitTransaction: true`

```csharp
// ❌ Внутри явной транзакции это ОБРВЁТ её после первого SaveChanges
_unitOfWork.EnableTransaction();
await _unitOfWork.SaveChangesAsync(); // ← уже закоммитили и сбросили транзакцию
// ... дальнейшие операции вне транзакции

// ✅ Правильно для многошаговых сценариев
_unitOfWork.EnableTransaction();
try
{
    await _unitOfWork.SaveChangesAsync(commitTransaction: false);
    // ... ещё операции ...
    await _unitOfWork.CommitTransactionAsync();
}
catch
{
    await _unitOfWork.RollbackTransactionAsync();
    throw;
}
```

## Отличия от Repository Pattern

| Аспект | Repository | Unit of Work |
|--------|-----------|--------------|
| **Ответственность** | Доступ к данным конкретной сущности | Координация нескольких репозиториев |
| **Транзакции** | Поддержка на уровне одной сущности | Управление транзакциями между сущностями |
| **Scope** | Один тип сущности | Контекст выполнения (use case) |
| **Действия** | Генерация действий сущности | Контроль над всеми действиями |

## См. также

| Документ | Описание |
|----------|----------|
| [Repository Pattern](repository.md) | Доступ к данным через репозиторий |
| [Specification Pattern](specification.md) | Инкапсуляция критериев выборки |
| [Lifecycle Actions](lifecycle-actions.md) | Управление перехватом жизненного цикла |
| [CQRS](cqrs.md) | Разделение команд и запросов |
