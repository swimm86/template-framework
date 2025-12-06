# EF Core Integration for Outbox Pattern

## Интеграция с DbContext

### 1. Добавьте конфигурацию в ваш DbContext

```csharp
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Dal.EFCore;
using Shared.Infrastructure.Dal.EFCore.Outbox.Extensions;
using Shared.Domain.Core.Entities;

namespace YourProject.Infrastructure.Dal;

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
        
        // Применяем конфигурацию Outbox
        modelBuilder.ConfigureOutbox();
        
        // Ваши другие конфигурации
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
```

### 2. Создайте миграцию

```bash
cd YourProject.Infrastructure.Dal
dotnet ef migrations add AddOutboxEvents --startup-project ../YourProject.Api
dotnet ef database update --startup-project ../YourProject.Api
```

### 3. SQL скрипт миграции (для справки)

Миграция создаст следующую таблицу:

```sql
CREATE TABLE outbox_events (
    id UUID PRIMARY KEY,
    event_type VARCHAR(255) NOT NULL,
    event_data TEXT NOT NULL,
    correlation_id VARCHAR(255),
    status INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    processed_at TIMESTAMP,
    retry_count INTEGER NOT NULL DEFAULT 0,
    error_message TEXT,
    url VARCHAR(2048),
    http_method VARCHAR(10),
    headers_json TEXT,
    content_type VARCHAR(255),
    timeout_seconds INTEGER NOT NULL DEFAULT 100,
    max_retry_count INTEGER NOT NULL DEFAULT 5,
    next_attempt_at TIMESTAMP,
    priority INTEGER NOT NULL DEFAULT 0,
    idempotency_key VARCHAR(255),
    trace_id VARCHAR(255),
    tenant_id VARCHAR(255),
    lock_id VARCHAR(255),
    lock_expires_at TIMESTAMP
);

-- Индексы
CREATE INDEX ix_outbox_events_status_nextattempt ON outbox_events (status, next_attempt_at);
CREATE INDEX ix_outbox_events_status_processed ON outbox_events (status, processed_at);
CREATE INDEX ix_outbox_events_correlation ON outbox_events (correlation_id);
CREATE UNIQUE INDEX uix_outbox_events_idempotency ON outbox_events (idempotency_key) WHERE idempotency_key IS NOT NULL;
CREATE INDEX ix_outbox_events_priority_created ON outbox_events (priority, created_at);
CREATE INDEX ix_outbox_events_lock_expires ON outbox_events (lock_expires_at) WHERE lock_expires_at IS NOT NULL;
```

## Использование с Repository Pattern

```csharp
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Entities;

public class OrderService
{
    private readonly IUnitOfWork _unitOfWork;
    
    public OrderService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task CreateOrderWithNotification(Order order)
    {
        // Получаем репозитории
        var orderRepo = _unitOfWork.GetRepository<Order>();
        var outboxRepo = _unitOfWork.GetRepository<OutboxEvent>();
        
        // Добавляем заказ
        await orderRepo.AddAsync(order, null, null);
        
        // Добавляем событие в Outbox
        var outboxEvent = new OutboxEvent(
            eventType: "http.post",
            eventData: JsonSerializer.Serialize(new { OrderId = order.Id }),
            correlationId: order.CorrelationId)
        {
            Url = "https://api.example.com/orders",
            HttpMethod = "POST",
            ContentType = "application/json"
        };
        
        await outboxRepo.AddAsync(outboxEvent, null, null);
        
        // Атомарное сохранение в одной транзакции
        await _unitOfWork.SaveChangesAsync();
    }
}
```

## Конфигурация таблицы

Конфигурация находится в `OutboxEventConfiguration.cs` и включает:

- Автоматическую генерацию GUID для ID
- Оптимизированные индексы для быстрого поиска
- Уникальный индекс для IdempotencyKey
- Правильные типы данных для PostgreSQL
- Default значения для важных полей

## Особенности PostgreSQL

Конфигурация оптимизирована для PostgreSQL:
- TEXT тип для больших данных (EventData, ErrorMessage, HeadersJson)
- Partial indexes с WHERE clause для экономии места
- NOW() функция для default timestamp

Для других БД может потребоваться адаптация конфигурации.

