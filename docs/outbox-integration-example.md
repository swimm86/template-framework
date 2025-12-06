# Пример интеграции Outbox Pattern

Этот документ показывает, как интегрировать Outbox pattern в существующий сервис проекта.

## Пример для Gpn.Template.Getter.Api

### 1. Обновление DbContext

```csharp
// В файле: Gpn.Template.Infrastructure.Dal/DbContext.cs

using Shared.Domain.Core.Entities;
using Shared.Infrastructure.Dal.EFCore.Outbox.Extensions;

public class DbContext : DbContextBase
{
    // Добавляем DbSet для Outbox
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Конфигурируем Outbox
        modelBuilder.ConfigureOutbox();
    }
}
```

### 2. Регистрация сервисов в API

```csharp
// В файле: Gpn.Template.Getter.Api/Program.cs

using Shared.Application.Core.Outbox.Extensions;
using Shared.Infrastructure.Job.Quartz.Outbox.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ... существующие регистрации ...

// Регистрируем Outbox сервисы
builder.Services.AddOutbox(builder.Configuration);

// Регистрируем Outbox Jobs
builder.Services.AddOutboxJobs(
    processorCronExpression: "0 * * * * ?",  // Каждую минуту
    cleanupCronExpression: "0 0 * * * ?",    // Каждый час
    batchSize: 100,
    lockDurationMinutes: 5,
    cleanupOlderThanDays: 30
);

var app = builder.Build();
// ... остальная конфигурация ...
```

### 3. Настройка appsettings.json

```json
// В файле: Gpn.Template.Getter.Api/appsettings.json

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

### 4. Создание миграции

```bash
cd src/Services/DatabaseUpgrade/Gpn.Template.DatabaseUpgrade
dotnet ef migrations add AddOutboxEvents --context DbContext
dotnet ef database update --context DbContext
```

### 5. Использование в Feature

```csharp
// В файле: Gpn.Template.Getter.Application/Features/GetItemFeature.cs

using Shared.Application.Core.Outbox.Interfaces;
using Shared.Application.Core.Outbox.Builders;

public class GetItemFeature : IGetItemFeature
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxService _outboxService;
    private readonly ILogger<GetItemFeature> _logger;

    public GetItemFeature(
        IUnitOfWork unitOfWork,
        IOutboxService outboxService,
        ILogger<GetItemFeature> logger)
    {
        _unitOfWork = unitOfWork;
        _outboxService = outboxService;
        _logger = logger;
    }

    public async Task<Item> GetAndNotifyAsync(Guid itemId, CancellationToken cancellationToken)
    {
        // Получаем элемент
        var repository = _unitOfWork.GetRepository<Item>();
        var item = await repository.GetAsync(itemId, cancellationToken: cancellationToken);

        if (item == null)
        {
            throw new NotFoundException($"Item {itemId} not found");
        }

        // Создаем событие для отправки уведомления через Outbox
        var outboxEvent = new OutboxEventBuilder()
            .AsHttpRequest("POST", "https://notification-service.example.com/api/notifications")
            .WithEventData(new 
            { 
                EventType = "ItemAccessed",
                ItemId = item.Id,
                AccessedAt = DateTime.UtcNow
            })
            .WithCorrelationId(Guid.NewGuid().ToString())
            .WithPriority(5)
            .WithIdempotencyKey($"item-accessed-{item.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}")
            .WithHttpHeaders(new Dictionary<string, string>
            {
                ["Authorization"] = "Bearer YOUR_TOKEN",
                ["Content-Type"] = "application/json"
            })
            .Build();

        // Добавляем в Outbox - будет обработано асинхронно
        await _outboxService.AddAsync(outboxEvent, cancellationToken);

        _logger.LogInformation(
            "Created outbox event for item access notification: ItemId={ItemId}, EventId={EventId}",
            item.Id,
            outboxEvent.Id);

        return item;
    }
}
```

### 6. Использование в транзакции

```csharp
// Пример атомарной операции с Outbox

public async Task CreateItemWithNotificationAsync(CreateItemDto dto, CancellationToken cancellationToken)
{
    using var unitOfWork = _unitOfWorkFactory.Create();
    
    try
    {
        // 1. Создаем элемент
        var itemRepo = unitOfWork.GetRepository<Item>();
        var item = new Item 
        { 
            Id = Guid.NewGuid(),
            Name = dto.Name,
            CreatedAt = DateTime.UtcNow
        };
        
        await itemRepo.AddAsync(item, null, null, cancellationToken);

        // 2. Создаем Outbox событие в той же транзакции
        var outboxRepo = unitOfWork.GetRepository<OutboxEvent>();
        var outboxEvent = new OutboxEventBuilder()
            .AsHttpRequest("POST", "https://api.example.com/webhooks/item-created")
            .WithEventData(new { ItemId = item.Id, ItemName = item.Name })
            .WithCorrelationId(Guid.NewGuid().ToString())
            .WithPriority(10)
            .Build();
        
        await outboxRepo.AddAsync(outboxEvent, null, null, cancellationToken);

        // 3. Сохраняем всё атомарно
        await unitOfWork.SaveChangesAsync(true, cancellationToken);

        _logger.LogInformation(
            "Created item with outbox event: ItemId={ItemId}, EventId={EventId}",
            item.Id,
            outboxEvent.Id);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to create item with notification");
        // Rollback происходит автоматически
        throw;
    }
}
```

### 7. Создание кастомного обработчика

```csharp
// В файле: Gpn.Template.Getter.Application/Outbox/CustomNotificationHandler.cs

using Shared.Application.Core.Outbox.Interfaces;
using Shared.Domain.Core.Entities;

public class CustomNotificationHandler : IOutboxEventHandler
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<CustomNotificationHandler> _logger;

    public CustomNotificationHandler(
        INotificationService notificationService,
        ILogger<CustomNotificationHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public bool CanHandle(string eventType)
    {
        return eventType.StartsWith("custom.notification.");
    }

    public async Task HandleAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing custom notification: EventId={EventId}, Type={EventType}",
            outboxEvent.Id,
            outboxEvent.EventType);

        var data = JsonSerializer.Deserialize<NotificationData>(outboxEvent.EventData);
        
        if (data == null)
        {
            throw new InvalidOperationException("Failed to deserialize notification data");
        }

        await _notificationService.SendAsync(data, cancellationToken);
    }
}

// Регистрация в DI
builder.Services.AddOutboxEventHandler<CustomNotificationHandler>();
```

## Пример для Gpn.Template.Bff.Api

### Интеграция с HTTP клиентами

```csharp
// В файле: Gpn.Template.Bff.Application/Features/SomeFeature.cs

public class BffFeature
{
    private readonly IOutboxService _outboxService;

    public async Task ForwardRequestToBackendAsync(RequestDto request)
    {
        // Вместо прямого HTTP вызова используем Outbox для надежности
        var outboxEvent = new OutboxEventBuilder()
            .AsHttpRequest("POST", "https://backend-service.internal/api/process")
            .WithEventData(request)
            .WithCorrelationId(request.CorrelationId)
            .WithTimeout(30)
            .WithMaxRetryCount(3)
            .WithHttpHeaders(new Dictionary<string, string>
            {
                ["X-Request-Source"] = "BFF",
                ["Authorization"] = $"Bearer {_tokenService.GetToken()}"
            })
            .Build();

        await _outboxService.AddAsync(outboxEvent);
    }
}
```

## Мониторинг и метрики

### Добавление Health Check

```csharp
// В Program.cs

builder.Services.AddHealthChecks()
    .AddCheck<OutboxHealthCheck>("outbox_health");

// Реализация OutboxHealthCheck
public class OutboxHealthCheck : IHealthCheck
{
    private readonly IOutboxService _outboxService;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken)
    {
        // Проверяем количество failed событий
        var repository = _unitOfWork.GetRepository<OutboxEvent>();
        var failedCount = await repository.CountAsync(
            new QueryOptions<OutboxEvent>()
                .AddFilter(e => e.Status == OutboxEventStatus.Failed),
            cancellationToken);

        if (failedCount > 100)
        {
            return HealthCheckResult.Unhealthy(
                $"Too many failed outbox events: {failedCount}");
        }

        if (failedCount > 10)
        {
            return HealthCheckResult.Degraded(
                $"Some failed outbox events: {failedCount}");
        }

        return HealthCheckResult.Healthy();
    }
}
```

### Метрики для мониторинга

```csharp
// Добавление кастомных метрик

public class OutboxMetricsService
{
    private readonly IUnitOfWork _unitOfWork;

    public async Task<OutboxMetrics> GetMetricsAsync()
    {
        var repository = _unitOfWork.GetRepository<OutboxEvent>();

        var pending = await repository.CountAsync(
            new QueryOptions<OutboxEvent>()
                .AddFilter(e => e.Status == OutboxEventStatus.Pending));

        var processing = await repository.CountAsync(
            new QueryOptions<OutboxEvent>()
                .AddFilter(e => e.Status == OutboxEventStatus.Processing));

        var failed = await repository.CountAsync(
            new QueryOptions<OutboxEvent>()
                .AddFilter(e => e.Status == OutboxEventStatus.Failed));

        var processed24h = await repository.CountAsync(
            new QueryOptions<OutboxEvent>()
                .AddFilter(e => 
                    e.Status == OutboxEventStatus.Processed &&
                    e.ProcessedAt >= DateTime.UtcNow.AddHours(-24)));

        return new OutboxMetrics
        {
            PendingCount = pending,
            ProcessingCount = processing,
            FailedCount = failed,
            Processed24Hours = processed24h
        };
    }
}
```

## Тестирование

### Unit тесты

```csharp
public class OutboxIntegrationTests
{
    [Fact]
    public async Task Should_Add_Event_To_Outbox_In_Transaction()
    {
        // Arrange
        var outboxService = _serviceProvider.GetRequiredService<IOutboxService>();
        var outboxEvent = new OutboxEventBuilder()
            .AsHttpRequest("POST", "https://test.com/api")
            .WithEventData(new { Test = "data" })
            .Build();

        // Act
        var result = await outboxService.AddAsync(outboxEvent);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(OutboxEventStatus.Pending, result.Status);
    }
}
```

## Резюме

Основные шаги интеграции:
1. ✅ Добавить конфигурацию в DbContext
2. ✅ Зарегистрировать сервисы в DI
3. ✅ Настроить appsettings.json
4. ✅ Создать миграцию БД
5. ✅ Использовать в бизнес-логике
6. ✅ Настроить мониторинг

После интеграции вы получите:
- Надежную доставку событий
- Автоматическую обработку с retry
- Мониторинг и observability
- Масштабируемость
- Идемпотентность операций

