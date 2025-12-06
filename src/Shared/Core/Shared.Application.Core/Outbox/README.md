# Outbox Pattern Implementation

Унифицированная реализация Outbox pattern для гарантированной доставки событий и HTTP запросов.

## Описание

Outbox pattern обеспечивает надежную доставку событий и внешних HTTP запросов с помощью:
- Атомарного сохранения событий вместе с бизнес-операциями
- Автоматической повторной обработки при ошибках
- Экспоненциального backoff для retry
- Распределенной блокировки для конкурентной обработки
- Приоритезации событий
- Идемпотентности запросов

## Архитектура

### Компоненты

1. **Domain Layer** (`Shared.Domain.Core`)
   - `OutboxEvent` - сущность для хранения событий
   - `OutboxEventStatus` - статусы обработки

2. **Application Layer** (`Shared.Application.Core`)
   - `IOutboxService` - интерфейс сервиса для работы с Outbox
   - `OutboxService` - реализация сервиса
   - `IOutboxEventHandler` - интерфейс обработчика событий
   - `HttpOutboxEventHandler` - стандартный HTTP обработчик
   - `OutboxEventProcessor` - процессор для обработки событий
   - `OutboxEventBuilder` - builder для создания событий
   - `OutboxSettings` - настройки

3. **Infrastructure Layer** (`Shared.Infrastructure.Dal.EFCore`)
   - `OutboxEventConfiguration` - конфигурация EF Core
   - Расширения для DbContext

4. **Job Layer** (`Shared.Infrastructure.Job.Quartz`)
   - `OutboxProcessorJob` - фоновая задача обработки
   - `OutboxCleanupJob` - задача очистки

## Установка и настройка

### 1. Регистрация сервисов

#### В Program.cs или Startup.cs

```csharp
using Shared.Application.Core.Outbox.Extensions;
using Shared.Infrastructure.Job.Quartz.Outbox.Extensions;

// Регистрация Outbox сервисов
builder.Services.AddOutbox(builder.Configuration);

// Регистрация Outbox Jobs
builder.Services.AddOutboxJobs(
    processorCronExpression: "0 * * * * ?",  // Каждую минуту
    cleanupCronExpression: "0 0 * * * ?",    // Каждый час
    batchSize: 100,
    lockDurationMinutes: 5,
    cleanupOlderThanDays: 30
);
```

### 2. Настройка appsettings.json

```json
{
  "Outbox": {
    "ProcessorEnabled": true,
    "ProcessorCronExpression": "0 * * * * ?",
    "BatchSize": 100,
    "LockDurationMinutes": 5,
    "CleanupEnabled": true,
    "CleanupCronExpression": "0 0 * * * ?",
    "CleanupOlderThanDays": 30,
    "DefaultMaxRetryCount": 5,
    "DefaultTimeoutSeconds": 100,
    "DefaultPriority": 0
  }
}
```

### 3. Интеграция с DbContext

```csharp
using Shared.Infrastructure.Dal.EFCore.Outbox.Extensions;

public class YourDbContext : DbContextBase
{
    public YourDbContext(
        DbContextOptions<YourDbContext> options,
        IServiceProvider serviceProvider,
        IHostEnvironment environment)
        : base(options, serviceProvider, environment)
    {
    }

    // Добавляем DbSet для OutboxEvent
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Конфигурируем Outbox
        modelBuilder.ConfigureOutbox();
    }
}
```

### 4. Создание миграции

```bash
dotnet ef migrations add AddOutboxEvents
dotnet ef database update
```

## Использование

### 1. Добавление событий в Outbox

#### Простой способ с Builder

```csharp
using Shared.Application.Core.Outbox.Builders;
using Shared.Application.Core.Outbox.Interfaces;

public class YourService
{
    private readonly IOutboxService _outboxService;
    
    public YourService(IOutboxService outboxService)
    {
        _outboxService = outboxService;
    }
    
    public async Task ProcessOrder(Order order)
    {
        // Бизнес-логика
        await SaveOrder(order);
        
        // Создаем HTTP событие для отправки уведомления
        var outboxEvent = new OutboxEventBuilder()
            .AsHttpRequest("POST", "https://api.example.com/notifications")
            .WithEventData(new { OrderId = order.Id, Status = "Created" })
            .WithCorrelationId(order.CorrelationId)
            .WithPriority(10)
            .WithIdempotencyKey($"order-{order.Id}-notification")
            .WithHttpHeaders(new Dictionary<string, string>
            {
                ["Authorization"] = "Bearer YOUR_TOKEN"
            })
            .Build();
        
        await _outboxService.AddAsync(outboxEvent);
    }
}
```

#### В рамках транзакции с UnitOfWork

```csharp
public async Task ProcessOrderWithTransaction(Order order)
{
    using var unitOfWork = _unitOfWorkFactory.Create();
    
    try
    {
        // Бизнес-операция
        var orderRepo = unitOfWork.GetRepository<Order>();
        await orderRepo.AddAsync(order, null, null);
        
        // Outbox событие
        var outboxRepo = unitOfWork.GetRepository<OutboxEvent>();
        var outboxEvent = new OutboxEventBuilder()
            .AsHttpRequest("POST", "https://api.example.com/orders")
            .WithEventData(order)
            .WithCorrelationId(order.CorrelationId)
            .Build();
            
        await outboxRepo.AddAsync(outboxEvent, null, null);
        
        // Атомарное сохранение
        await unitOfWork.SaveChangesAsync();
    }
    catch
    {
        // Rollback происходит автоматически
        throw;
    }
}
```

### 2. Создание кастомного обработчика

```csharp
using Shared.Application.Core.Outbox.Interfaces;
using Shared.Domain.Core.Entities;

public class CustomEventHandler : IOutboxEventHandler
{
    private readonly ILogger<CustomEventHandler> _logger;
    
    public CustomEventHandler(ILogger<CustomEventHandler> logger)
    {
        _logger = logger;
    }
    
    public bool CanHandle(string eventType)
    {
        return eventType.StartsWith("custom.");
    }
    
    public async Task HandleAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing custom event: {EventType}", outboxEvent.EventType);
        
        // Ваша логика обработки
        await ProcessCustomLogic(outboxEvent.EventData, cancellationToken);
    }
    
    private async Task ProcessCustomLogic(string eventData, CancellationToken cancellationToken)
    {
        // Реализация
    }
}

// Регистрация в DI
builder.Services.AddOutboxEventHandler<CustomEventHandler>();
```

### 3. Примеры различных типов событий

#### HTTP POST запрос

```csharp
var postEvent = new OutboxEventBuilder()
    .AsHttpRequest("POST", "https://api.example.com/webhooks")
    .WithEventData(new { Event = "UserCreated", UserId = userId })
    .WithTimeout(30)
    .WithMaxRetryCount(3)
    .Build();

await _outboxService.AddAsync(postEvent);
```

#### HTTP GET запрос

```csharp
var getEvent = new OutboxEventBuilder()
    .AsHttpRequest("GET", "https://api.example.com/sync")
    .WithEventData("{}")  // Пустое тело для GET
    .Build();

await _outboxService.AddAsync(getEvent);
```

#### Кастомное событие

```csharp
var customEvent = new OutboxEventBuilder()
    .WithEventType("custom.order.payment")
    .WithEventData(JsonSerializer.Serialize(paymentInfo))
    .WithPriority(100)  // Высокий приоритет
    .Build();

await _outboxService.AddAsync(customEvent);
```

## Особенности реализации

### Retry стратегия

- Экспоненциальная задержка: 2^RetryCount минут
- Настраиваемое максимальное количество попыток (по умолчанию: 5)
- События с превышением лимита попыток переходят в статус Failed

### Приоритизация

- События обрабатываются по убыванию приоритета
- При одинаковом приоритете - по времени создания (FIFO)

### Идемпотентность

- Уникальный индекс на поле `IdempotencyKey`
- Заголовок `X-Idempotency-Key` в HTTP запросах

### Распределенная обработка

- Блокировка (lease) с автоматическим истечением
- Защита от одновременной обработки одного события несколькими воркерами

### Observability

- Корреляционный ID (`X-Correlation-Id`)
- Трассировочный ID (`X-Trace-Id`)
- Подробное логирование всех операций

## Мониторинг и обслуживание

### Запросы для мониторинга

```sql
-- Количество необработанных событий
SELECT COUNT(*) FROM outbox_events WHERE status = 0;

-- События с ошибками
SELECT * FROM outbox_events 
WHERE status = 3 
ORDER BY created_at DESC;

-- Топ типов событий с ошибками
SELECT event_type, COUNT(*) as error_count 
FROM outbox_events 
WHERE status = 3 
GROUP BY event_type 
ORDER BY error_count DESC;
```

### Ручная обработка

```csharp
// Повторная обработка failed события
await _outboxService.ReleaseLockAsync(eventId);

// Очистка старых событий
var deletedCount = await _outboxService.CleanupProcessedEventsAsync(olderThanDays: 7);
```

## Best Practices

1. **Всегда используйте транзакции**: Добавляйте Outbox события в той же транзакции, что и бизнес-операции
2. **Устанавливайте IdempotencyKey**: Для критичных запросов всегда указывайте уникальный ключ
3. **Используйте CorrelationId**: Для трассировки сквозных операций
4. **Настраивайте приоритеты**: Важные события должны иметь более высокий приоритет
5. **Мониторьте Failed события**: Настройте алерты на накопление failed событий
6. **Оптимизируйте BatchSize**: Подбирайте размер батча в зависимости от нагрузки

## Производительность

### Индексы

Созданы следующие индексы для оптимизации:
- `(status, next_attempt_at)` - для выборки событий на обработку
- `(status, processed_at)` - для очистки
- `correlation_id` - для поиска связанных событий
- `idempotency_key` - уникальный индекс
- `(priority, created_at)` - для сортировки
- `lock_expires_at` - для поиска просроченных блокировок

### Рекомендации

- **BatchSize**: 50-200 в зависимости от нагрузки
- **ProcessorCronExpression**: от 30 секунд до 5 минут
- **CleanupOlderThanDays**: 7-30 дней
- **LockDurationMinutes**: 5-10 минут

## Troubleshooting

### События не обрабатываются

1. Проверьте, что Job зарегистрированы и запущены
2. Проверьте логи на наличие ошибок
3. Убедитесь, что события не заблокированы

```csharp
// Снять все просроченные блокировки
await _outboxService.ReleaseExpiredLocksAsync();
```

### Высокая latency обработки

1. Увеличьте `BatchSize`
2. Уменьшите интервал обработки (cron expression)
3. Масштабируйте количество воркеров (с учетом lease механизма)

### Накопление Failed событий

1. Анализируйте `ErrorMessage` в failed событиях
2. Проверьте доступность внешних сервисов
3. Увеличьте `MaxRetryCount` для временных проблем
4. Реализуйте Dead Letter Queue для постоянных ошибок

