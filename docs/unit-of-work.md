# Unit of Work Pattern

## Обзор

Unit of Work (UoW) — это паттерн, который координирует работу нескольких репозиториев в рамках одной бизнес-транзакции, обеспечивая атомарность операций и согласованность данных. В фреймворке Shared реализован через интерфейс `IUnitOfWork`.

## Интерфейс IUnitOfWork

Базовый интерфейс определен в `Shared.Domain.Core.Dal.UnitOfWork.Interfaces.IUnitOfWork`:

```csharp
public interface IUnitOfWork : IDisposable
{
    // Получение репозитория
    IRepository<TEntity> GetRepository<TEntity>() where TEntity : class, IEntity;

    // Сохранение изменений
    int SaveChanges(bool commitTransaction = true, bool resetEventSettingsAfterSave = true);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default, bool commitTransaction = true, bool resetEventSettingsAfterSave = true);

    // Управление транзакциями
    Task CommitTransactionAsync(CancellationToken cancellationToken);
    Task RollbackTransactionAsync(CancellationToken cancellationToken);
    Task ResetTransactionAsync(CancellationToken cancellationToken);
    IUnitOfWork EnableTransaction();
    IUnitOfWork DisableTransaction();

    // Управление доменными событиями
    IUnitOfWork EnableEvents();
    IUnitOfWork DisableEvents();
    IUnitOfWork DisableEvents<TEntity>(DomainEventType? eventType = default) where TEntity : IEntity, IWithDomainEvents;
    IUnitOfWork EnableEvents<TEntity>(DomainEventType? eventType = default) where TEntity : IEntity, IWithDomainEvents;
    IUnitOfWork DisableEvents<TEntity>(DomainEventType eventType, Enum eventKeyFlags) where TEntity : IEntity, IWithDomainEvents;
    IUnitOfWork EnableEvents<TEntity>(DomainEventType eventType, Enum eventKeyFlags) where TEntity : IEntity, IWithDomainEvents;
    IUnitOfWork ResetEventSettings();

    // Управление отслеживанием
    void ClearTracking();
}
```

## Ключевые возможности

### 1. Централизованное получение репозиториев

Все репозитории создаются в рамках одного контекста данных:

```csharp
using var uow = _unitOfWorkFactory.Create();

var userRepo = uow.GetRepository<User>();
var orderRepo = uow.GetRepository<Order>();
var productRepo = uow.GetRepository<Product>();

// Все операции выполняются в рамках одного DbContext
```

### 2. Атомарное сохранение изменений

Гарантия того, что все изменения сохраняются как единая транзакция:

```csharp
public async Task CreateOrder(CreateOrderRequest request)
{
    using var uow = _unitOfWorkFactory.Create();

    var userRepo = uow.GetRepository<User>();
    var orderRepo = uow.GetRepository<Order>();
    var productRepo = uow.GetRepository<Product>();

    // Проверка пользователя
    var user = await userRepo.GetAsync(request.UserId);
    if (user == null)
    {
        throw new NotFoundException("User not found");
    }

    // Проверка и резервирование товаров
    foreach (var item in request.Items)
    {
        var product = await productRepo.GetAsync(item.ProductId);
        if (product.Stock < item.Quantity)
        {
            throw new InvalidOperationException($"Insufficient stock for product {product.Name}");
        }
        product.Stock -= item.Quantity;
        await productRepo.UpdateAsync(product);
    }

    // Создание заказа
    var order = new Order
    {
        UserId = user.Id,
        TotalAmount = request.Items.Sum(i => i.Quantity * i.Price),
        Status = OrderStatus.Pending
    };

    await orderRepo.AddAsync(order);

    // Атомарное сохранение всех изменений
    await uow.SaveChangesAsync();
}
```

### 3. Явное управление транзакциями

Контроль над транзакциями с возможностью commit/rollback:

```csharp
public async Task TransferMoney(Guid fromAccountId, Guid toAccountId, decimal amount)
{
    using var uow = _unitOfWorkFactory.Create();
    uow.EnableTransaction();

    try
    {
        var accountRepo = uow.GetRepository<Account>();

        var fromAccount = await accountRepo.GetAsync(fromAccountId);
        var toAccount = await accountRepo.GetAsync(toAccountId);

        if (fromAccount.Balance < amount)
        {
            throw new InsufficientFundsException();
        }

        fromAccount.Balance -= amount;
        toAccount.Balance += amount;

        await accountRepo.UpdateAsync(fromAccount);
        await accountRepo.UpdateAsync(toAccount);

        await uow.CommitTransactionAsync();
    }
    catch
    {
        await uow.RollbackTransactionAsync();
        throw;
    }
}
```

### 4. Управление доменными событиями

Гибкий контроль над генерацией и обработкой доменных событий:

```csharp
// Глобальное отключение событий
using var uow = _unitOfWorkFactory.Create();
uow.DisableEvents();

await userRepository.AddAsync(user);
await uow.SaveChangesAsync(); // События не будут сгенерированы

// Отключение событий для конкретного типа сущности
uow.DisableEvents<User>();

// Отключение событий определенного типа для сущности
uow.DisableEvents<User>(DomainEventType.Created);

// Отключение событий по флагам
uow.DisableEvents<User>(DomainEventType.Updated, UserUpdateFlags.EmailChanged);

// Включение событий обратно
uow.EnableEvents<User>();

// Сброс всех настроек событий
uow.ResetEventSettings();
```

**Типы доменных событий:**
- `DomainEventType.Created` — событие создания сущности
- `DomainEventType.Updated` — событие обновления сущности
- `DomainEventType.Deleted` — событие удаления сущности

### 5. Управление отслеживанием изменений

Контроль над tracking сущностей в EF Core:

```csharp
// Очистка всех отслеживаемых сущностей
uow.ClearTracking();

// Чтение без отслеживания (через QueryOptions)
var options = new QueryOptions<User> { AsNoTracking = true };
var users = await repo.GetRangeAsync(options);
```

## Примеры использования

### Базовый сценарий с использованием

```csharp
public class OrderService
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public OrderService(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task<Order> CreateOrder(CreateOrderRequest request)
    {
        using var uow = _unitOfWorkFactory.Create();

        var userRepo = uow.GetRepository<User>();
        var orderRepo = uow.GetRepository<Order>();
        var productRepo = uow.GetRepository<Product>();

        // Бизнес-логика...

        var result = await orderRepo.AddAsync(order);
        await uow.SaveChangesAsync();

        return result;
    }
}
```

### Транзакция с явным rollback

```csharp
public async Task ProcessRefund(Guid orderId, decimal amount)
{
    using var uow = _unitOfWorkFactory.Create();
    uow.EnableTransaction();

    try
    {
        var orderRepo = uow.GetRepository<Order>();
        var paymentRepo = uow.GetRepository<Payment>();

        var order = await orderRepo.GetAsync(orderId);
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
        await orderRepo.UpdateAsync(order);

        await uow.CommitTransactionAsync();
    }
    catch (Exception ex)
    {
        await uow.RollbackTransactionAsync();
        // Логирование ошибки
        throw;
    }
}
```

### Пакетная обработка с контролем событий

```csharp
public async Task BulkUpdatePrices(IEnumerable<PriceUpdate> updates)
{
    using var uow = _unitOfWorkFactory.Create();

    // Отключаем события обновления для массовой операции
    uow.DisableEvents<Product>(DomainEventType.Updated);

    var productRepo = uow.GetRepository<Product>();

    foreach (var update in updates)
    {
        var product = await productRepo.GetAsync(update.ProductId);
        if (product != null)
        {
            product.Price = update.NewPrice;
            product.UpdatedAt = DateTime.UtcNow;
            await productRepo.UpdateAsync(product);
        }
    }

    await uow.SaveChangesAsync();
}
```

### Вложенные транзакции (Reset)

```csharp
public async Task ComplexOperation()
{
    using var uow = _unitOfWorkFactory.Create();
    uow.EnableTransaction();

    // Первая часть операции
    await PartOneAsync(uow);

    // Сброс транзакции для независимой второй части
    await uow.ResetTransactionAsync();
    uow.EnableTransaction();

    // Вторая часть операции (не зависит от первой)
    await PartTwoAsync(uow);

    await uow.CommitTransactionAsync();
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

### 1. Используйте `using` для автоматического disposal

```csharp
// Правильно
using var uow = _unitOfWorkFactory.Create();
await uow.SaveChangesAsync();

// Неправильно - может привести к утечкам ресурсов
var uow = _unitOfWorkFactory.Create();
await uow.SaveChangesAsync();
```

### 2. Минимизируйте scope транзакции

```csharp
// Правильно - короткая транзакция
public async Task UpdateUserEmail(Guid userId, string email)
{
    using var uow = _unitOfWorkFactory.Create();
    var repo = uow.GetRepository<User>();
    var user = await repo.GetAsync(userId);
    user.Email = email;
    await repo.UpdateAsync(user);
    await uow.SaveChangesAsync();
}

// Неправильно - длительная транзакция включает внешние вызовы
public async Task UpdateUserWithEmailVerification(Guid userId, string email)
{
    using var uow = _unitOfWorkFactory.Create();
    var repo = uow.GetRepository<User>();
    var user = await repo.GetAsync(userId);

    // Долгий внешний вызов внутри транзакции!
    await _emailService.SendVerificationEmail(email);

    user.Email = email;
    await repo.UpdateAsync(user);
    await uow.SaveChangesAsync();
}
```

### 3. Явно указывайте необходимость транзакции

```csharp
// Для операций чтения транзакция не нужна
var user = await repo.GetAsync(id);

// Для операций записи явно включайте транзакцию при необходимости
uow.EnableTransaction();
```

### 4. Обрабатывайте исключения корректно

```csharp
try
{
    await uow.SaveChangesAsync();
}
catch
{
    await uow.RollbackTransactionAsync();
    throw; // Передаем исключение выше
}
```

### 5. Используйте ClearTracking для длинных процессов

```csharp
foreach (var batch in batches)
{
    using var uow = _unitOfWorkFactory.Create();

    foreach (var item in batch)
    {
        await ProcessItemAsync(uow, item);
    }

    await uow.SaveChangesAsync();
    uow.ClearTracking(); // Освобождаем память
}
```

## Отличия от Repository Pattern

| Аспект | Repository | Unit of Work |
|--------|-----------|--------------|
| **Ответственность** | Доступ к данным конкретной сущности | Координация нескольких репозиториев |
| **Транзакции** | Поддержка на уровне одной сущности | Управление транзакциями между сущностями |
| **Scope** | Один тип сущности | Контекст выполнения (use case) |
| **События** | Генерация событий сущности | Контроль над всеми событиями |

## См. также

- [Repository Pattern](repository.md) — доступ к данным сущности
- [Specification Pattern](specification.md) — критерии выборки
- [CQRS Pattern](../README.md#cqrs-pattern) — разделение команд и запросов