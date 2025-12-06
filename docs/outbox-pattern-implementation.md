# Реализация Outbox Pattern в GPN Template Backend

## Обзор

Реализован унифицированный Outbox pattern для обеспечения надежной доставки событий и HTTP запросов к внешним системам. Реализация следует архитектуре проекта с разделением на слои: Domain, Application, Infrastructure (Dal, Job).

## Архитектура решения

```
Shared/
├── Core/
│   ├── Shared.Domain.Core/
│   │   ├── Entities/
│   │   │   └── OutboxEvent.cs                    # Domain-сущность
│   │   └── Enums/
│   │       └── OutboxEventStatus.cs              # Статусы событий
│   │
│   └── Shared.Application.Core/
│       └── Outbox/
│           ├── Interfaces/
│           │   ├── IOutboxService.cs             # Интерфейс сервиса
│           │   └── IOutboxEventHandler.cs        # Интерфейс обработчика
│           ├── OutboxService.cs                  # Реализация сервиса
│           ├── OutboxEventProcessor.cs           # Процессор событий
│           ├── Handlers/
│           │   └── HttpOutboxEventHandler.cs     # HTTP обработчик
│           ├── Builders/
│           │   └── OutboxEventBuilder.cs         # Builder для событий
│           ├── Settings/
│           │   └── OutboxSettings.cs             # Настройки
│           ├── Extensions/
│           │   ├── OutboxDependencyInjection.cs
│           │   └── OutboxServiceCollectionExtensions.cs
│           └── README.md
│
├── Dal/
│   └── Shared.Infrastructure.Dal.EFCore/
│       └── Outbox/
│           ├── OutboxEventConfiguration.cs       # EF Core конфигурация
│           ├── Extensions/
│           │   └── OutboxDbContextExtensions.cs
│           └── README.md
│
└── Job/
    └── Shared.Infrastructure.Job.Quartz/
        └── Outbox/
            ├── OutboxProcessorJob.cs             # Job обработки
            ├── OutboxCleanupJob.cs               # Job очистки
            ├── Extensions/
            │   └── OutboxJobExtensions.cs
            └── README.md
```

## Ключевые компоненты

### 1. Domain Layer

#### OutboxEvent
Сущность для хранения событий с полной информацией для HTTP запросов:
- Базовые поля: EventType, EventData, CorrelationId, Status
- HTTP-специфичные: Url, HttpMethod, Headers, ContentType
- Управление retry: RetryCount, MaxRetryCount, NextAttemptAt, ErrorMessage
- Приоритизация: Priority
- Идемпотентность: IdempotencyKey
- Распределенная блокировка: LockId, LockExpiresAt
- Observability: TraceId, TenantId

#### OutboxEventStatus
Статусы жизненного цикла события:
- Pending (0) - ожидает обработки
- Processing (1) - в процессе обработки
- Processed (2) - успешно обработано
- Failed (3) - обработка не удалась

### 2. Application Layer

#### IOutboxService
Основной сервис для работы с Outbox:
- `AddAsync()` - добавление события
- `AddRangeAsync()` - добавление нескольких событий
- `GetPendingEventsAsync()` - получение событий для обработки с блокировкой
- `MarkAsProcessedAsync()` - пометка как обработанного
- `MarkAsFailedAsync()` - пометка как неудачного с retry логикой
- `ReleaseLockAsync()` - снятие блокировки
- `CleanupProcessedEventsAsync()` - очистка старых событий
- `ReleaseExpiredLocksAsync()` - снятие просроченных блокировок

#### OutboxEventProcessor
Процессор для пакетной обработки событий:
- Получает батч событий с блокировкой
- Находит подходящий обработчик для каждого типа события
- Обрабатывает события с обработкой ошибок
- Обновляет статусы событий

#### IOutboxEventHandler
Интерфейс для реализации кастомных обработчиков:
- `CanHandle(eventType)` - проверка, может ли обработчик обработать событие
- `HandleAsync(outboxEvent)` - обработка события

#### HttpOutboxEventHandler
Стандартный обработчик HTTP запросов:
- Поддержка всех HTTP методов
- Настраиваемые заголовки
- Таймауты
- Идемпотентность через X-Idempotency-Key
- Корреляция через X-Correlation-Id
- Трассировка через X-Trace-Id

#### OutboxEventBuilder
Fluent API для создания событий:
```csharp
var event = new OutboxEventBuilder()
    .AsHttpRequest("POST", "https://api.example.com/webhook")
    .WithEventData(data)
    .WithCorrelationId(correlationId)
    .WithPriority(10)
    .WithIdempotencyKey(key)
    .Build();
```

### 3. Infrastructure Dal Layer

#### OutboxEventConfiguration
EF Core конфигурация для PostgreSQL:
- Оптимизированные типы данных (TEXT для больших данных)
- Индексы для производительности:
  - `(status, next_attempt_at)` - для выборки pending событий
  - `(status, processed_at)` - для очистки
  - `correlation_id` - для поиска связанных событий
  - `idempotency_key` (UNIQUE) - для гарантии уникальности
  - `(priority, created_at)` - для приоритизации
  - `lock_expires_at` - для снятия просроченных блокировок
- Default значения для важных полей

#### OutboxDbContextExtensions
Расширения для интеграции с DbContext:
- `ConfigureOutbox()` - применение конфигурации
- `OutboxEvents()` - доступ к DbSet

### 4. Infrastructure Job Layer

#### OutboxProcessorJob
Фоновая задача обработки событий:
- `[DisallowConcurrentExecution]` - защита от параллельного выполнения
- Настраиваемые параметры через JobDataMap:
  - BatchSize - размер батча
  - LockDurationMinutes - длительность блокировки
- Интеграция с OutboxEventProcessor
- Обработка ошибок с логированием

#### OutboxCleanupJob
Фоновая задача обслуживания:
- Снятие просроченных блокировок
- Удаление старых обработанных событий
- Настраиваемый параметр OlderThanDays

#### OutboxJobExtensions
Удобные методы регистрации:
- `AddOutboxProcessorJob()` - регистрация процессора
- `AddOutboxCleanupJob()` - регистрация очистки
- `AddOutboxJobs()` - регистрация всех jobs с настройками по умолчанию

## Ключевые возможности

### 1. Надежная доставка
- Атомарное сохранение события вместе с бизнес-операцией в транзакции
- Автоматическая повторная обработка при сбоях
- Экспоненциальная задержка между попытками (2^RetryCount минут)

### 2. Приоритизация
- События с более высоким приоритетом обрабатываются первыми
- При одинаковом приоритете - FIFO (First In First Out)

### 3. Идемпотентность
- Уникальный IdempotencyKey на уровне БД
- Передача ключа в заголовке X-Idempotency-Key
- Защита от дублирования операций

### 4. Распределенная обработка
- Lease-based locking для безопасной конкурентной обработки
- Автоматическое истечение блокировок
- Горизонтальное масштабирование без конфликтов

### 5. Observability
- Корреляция запросов через CorrelationId
- Трассировка через TraceId
- Подробное логирование всех операций
- Метаданные для мультиарендности (TenantId)

### 6. Retry стратегия
- Настраиваемое максимальное количество попыток
- Экспоненциальный backoff
- Хранение информации об ошибках
- Автоматический переход в Failed после исчерпания попыток

### 7. Производительность
- Батчевая обработка событий
- Оптимизированные индексы БД
- Partial indexes для экономии места
- Настраиваемый размер батча и интервалы обработки

## Сценарии использования

### 1. HTTP Webhooks
```csharp
var webhook = new OutboxEventBuilder()
    .AsHttpRequest("POST", "https://partner.com/webhook")
    .WithEventData(eventPayload)
    .WithIdempotencyKey($"webhook-{orderId}")
    .Build();

await _outboxService.AddAsync(webhook);
```

### 2. Интеграция с внешними API
```csharp
var apiCall = new OutboxEventBuilder()
    .AsHttpRequest("POST", "https://api.external.com/process")
    .WithEventData(request)
    .WithTimeout(30)
    .WithMaxRetryCount(3)
    .WithHttpHeaders(headers)
    .Build();

await _outboxService.AddAsync(apiCall);
```

### 3. Атомарные операции
```csharp
using var uow = _unitOfWorkFactory.Create();

// Бизнес-операция
await orderRepo.AddAsync(order);

// Outbox событие
var event = new OutboxEventBuilder()
    .AsHttpRequest("POST", notificationUrl)
    .WithEventData(notification)
    .Build();
await outboxRepo.AddAsync(event);

// Атомарное сохранение
await uow.SaveChangesAsync();
```

### 4. Кастомная обработка
```csharp
public class EmailHandler : IOutboxEventHandler
{
    public bool CanHandle(string eventType) 
        => eventType.StartsWith("email.");

    public async Task HandleAsync(OutboxEvent e, CancellationToken ct)
    {
        var email = Deserialize<Email>(e.EventData);
        await _emailService.SendAsync(email, ct);
    }
}
```

## Настройка и конфигурация

### Минимальная настройка
```csharp
// Program.cs
builder.Services.AddOutbox(builder.Configuration);
builder.Services.AddOutboxJobs();
```

### Продвинутая настройка
```csharp
// Кастомные настройки
builder.Services.AddOutbox(settings =>
{
    settings.ProcessorEnabled = true;
    settings.BatchSize = 200;
    settings.LockDurationMinutes = 10;
    settings.DefaultMaxRetryCount = 3;
});

// Кастомные Jobs
builder.Services.AddOutboxJobs(
    processorCronExpression: "0/30 * * * * ?",  // Каждые 30 сек
    cleanupCronExpression: "0 0 */6 * * ?",     // Каждые 6 часов
    batchSize: 200,
    lockDurationMinutes: 10,
    cleanupOlderThanDays: 7
);

// Кастомный обработчик
builder.Services.AddOutboxEventHandler<CustomHandler>();
```

## Мониторинг и операционное управление

### Метрики для отслеживания
1. Количество pending событий
2. Количество failed событий
3. Количество обработанных событий за период
4. Среднее время обработки
5. Количество событий с блокировками

### SQL запросы для мониторинга
```sql
-- Pending события
SELECT COUNT(*) FROM outbox_events WHERE status = 0;

-- Failed события
SELECT event_type, COUNT(*), MAX(retry_count) 
FROM outbox_events 
WHERE status = 3 
GROUP BY event_type;

-- Старые необработанные
SELECT * FROM outbox_events 
WHERE status = 0 
  AND created_at < NOW() - INTERVAL '1 hour';
```

### Health Checks
Рекомендуется реализовать health check для мониторинга состояния Outbox:
- Количество failed событий < threshold
- Количество старых pending событий < threshold
- Наличие просроченных блокировок

## Производительность и масштабирование

### Рекомендации по нагрузке

| Нагрузка | Cron Process | Batch Size | Lock Duration |
|----------|--------------|------------|---------------|
| Низкая (<100/час) | 0 0/5 * * * ? | 50 | 5 мин |
| Средняя (100-1000/час) | 0 * * * * ? | 100 | 5 мин |
| Высокая (>1000/час) | 0/30 * * * * ? | 200 | 3 мин |

### Горизонтальное масштабирование
- Jobs безопасны для запуска на нескольких инстансах
- Lease-based locking предотвращает конфликты
- Каждый воркер получает уникальный LockId

### Оптимизация БД
- Регулярная очистка обработанных событий
- Мониторинг размера индексов
- Партиционирование таблицы при больших объемах

## Тестирование

Рекомендуется покрыть тестами:
1. Добавление событий в Outbox
2. Обработку событий с успехом
3. Обработку событий с ошибками и retry
4. Механизм блокировки
5. Приоритизацию событий
6. Идемпотентность через IdempotencyKey
7. Интеграцию с HTTP обработчиком

## Безопасность

1. **Хранение токенов**: Используйте секреты для хранения API токенов
2. **Валидация данных**: Валидируйте EventData перед обработкой
3. **Rate limiting**: Настройте лимиты на стороне обработчика
4. **HTTPS**: Используйте только HTTPS для внешних вызовов
5. **Аудит**: Логируйте все операции с Outbox

## Миграция существующего кода

Для миграции существующих HTTP вызовов на Outbox:

1. **Идентифицируйте критичные вызовы**: Те, что требуют гарантии доставки
2. **Замените прямые вызовы на Outbox**: Используйте OutboxEventBuilder
3. **Оберните в транзакции**: Где требуется атомарность
4. **Настройте мониторинг**: Отслеживайте метрики
5. **Тестируйте постепенно**: Начните с некритичных операций

## Заключение

Реализация Outbox pattern предоставляет:
- ✅ Надежную доставку событий
- ✅ Автоматическую обработку с retry
- ✅ Приоритизацию и идемпотентность
- ✅ Горизонтальное масштабирование
- ✅ Полную observability
- ✅ Простоту интеграции
- ✅ Соответствие архитектуре проекта

Реализация полностью интегрирована в существующую архитектуру и следует принципам Clean Architecture с разделением на слои Domain, Application, Infrastructure.

