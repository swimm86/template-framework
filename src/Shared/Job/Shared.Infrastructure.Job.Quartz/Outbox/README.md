# Quartz Jobs for Outbox Pattern

## Описание

Два фоновых Job'а для обработки Outbox событий:
1. **OutboxProcessorJob** - обрабатывает pending события
2. **OutboxCleanupJob** - очищает обработанные события и снимает просроченные блокировки

## Регистрация Jobs

### Простая регистрация с настройками по умолчанию

```csharp
using Shared.Infrastructure.Job.Quartz.Outbox.Extensions;

// В Program.cs
builder.Services.AddOutboxJobs();
```

По умолчанию:
- OutboxProcessorJob: каждую минуту
- OutboxCleanupJob: каждый час
- BatchSize: 100
- LockDuration: 5 минут
- CleanupOlderThanDays: 30 дней

### Регистрация с кастомными настройками

```csharp
builder.Services.AddOutboxJobs(
    processorCronExpression: "0/30 * * * * ?",  // Каждые 30 секунд
    cleanupCronExpression: "0 0 */6 * * ?",     // Каждые 6 часов
    batchSize: 200,
    lockDurationMinutes: 10,
    cleanupOlderThanDays: 7
);
```

### Раздельная регистрация

```csharp
// Только процессор
builder.Services.AddOutboxProcessorJob(
    cronExpression: "0 * * * * ?",
    batchSize: 100,
    lockDurationMinutes: 5
);

// Только очистка
builder.Services.AddOutboxCleanupJob(
    cronExpression: "0 0 * * * ?",
    olderThanDays: 30
);
```

## CRON выражения

### Примеры для OutboxProcessorJob

```csharp
// Каждые 10 секунд (для высокой нагрузки)
"0/10 * * * * ?"

// Каждые 30 секунд
"0/30 * * * * ?"

// Каждую минуту (рекомендовано)
"0 * * * * ?"

// Каждые 5 минут
"0 0/5 * * * ?"
```

### Примеры для OutboxCleanupJob

```csharp
// Каждый час (рекомендовано)
"0 0 * * * ?"

// Каждые 6 часов
"0 0 */6 * * ?"

// Раз в день в 2:00
"0 0 2 * * ?"

// Раз в неделю (воскресенье в 3:00)
"0 0 3 ? * SUN"
```

## OutboxProcessorJob

### Что делает

1. Получает батч pending событий (с учетом приоритета)
2. Блокирует их уникальным LockId
3. Для каждого события находит подходящий обработчик
4. Обрабатывает событие
5. Помечает как Processed или Failed

### Параметры

- **BatchSize**: Количество событий за один проход
  - Маленький (10-50): низкая нагрузка, быстрая реакция
  - Средний (50-200): баланс производительности
  - Большой (200+): высокая пропускная способность
  
- **LockDurationMinutes**: Время блокировки события
  - Короткое (2-5): быстрая обработка
  - Длинное (10-30): долгая обработка, внешние API

### Мониторинг

```csharp
// Логи
[12:00:00 INF] Starting outbox processor job
[12:00:00 INF] Processing 15 outbox events
[12:00:01 INF] Successfully processed outbox event: Id=xxx, Type=http.post
[12:00:05 INF] Completed outbox event processing: 15/15 events processed
```

## OutboxCleanupJob

### Что делает

1. Снимает блокировки с просроченных событий
2. Удаляет обработанные события старше N дней

### Параметры

- **OlderThanDays**: Удалять события старше X дней
  - 7 дней: минимальное хранение, экономия места
  - 30 дней: рекомендовано для аудита
  - 90+ дней: долгосрочное хранение

### Мониторинг

```csharp
// Логи
[01:00:00 INF] Starting outbox cleanup job
[01:00:00 INF] Released 3 expired locks
[01:00:01 INF] Outbox cleanup job completed: 150 events deleted, 3 locks released
```

## Масштабирование

### Горизонтальное масштабирование

Jobs безопасны для запуска на нескольких инстансах благодаря:
- `[DisallowConcurrentExecution]` атрибуту
- Механизму блокировки (lease) на уровне БД
- Уникальному LockId для каждого воркера

```
Instance 1: Lock events with LockId="worker-1-guid"
Instance 2: Lock events with LockId="worker-2-guid"
Instance 3: Lock events with LockId="worker-3-guid"
```

### Вертикальное масштабирование

Увеличьте BatchSize и уменьшите интервал:

```csharp
builder.Services.AddOutboxProcessorJob(
    cronExpression: "0/10 * * * * ?",  // Каждые 10 секунд
    batchSize: 500,                     // Больше событий
    lockDurationMinutes: 3              // Короткая блокировка
);
```

## Настройка под нагрузку

### Низкая нагрузка (< 100 событий/час)

```csharp
builder.Services.AddOutboxJobs(
    processorCronExpression: "0 0/5 * * * ?",  // Каждые 5 минут
    batchSize: 50,
    lockDurationMinutes: 5
);
```

### Средняя нагрузка (100-1000 событий/час)

```csharp
builder.Services.AddOutboxJobs(
    processorCronExpression: "0 * * * * ?",  // Каждую минуту
    batchSize: 100,
    lockDurationMinutes: 5
);
```

### Высокая нагрузка (> 1000 событий/час)

```csharp
builder.Services.AddOutboxJobs(
    processorCronExpression: "0/30 * * * * ?",  // Каждые 30 секунд
    batchSize: 200,
    lockDurationMinutes: 3
);
```

## Troubleshooting

### Job не запускается

1. Проверьте регистрацию Quartz:
```csharp
builder.Services.AddQuartz();
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
```

2. Проверьте логи Quartz:
```csharp
builder.Logging.AddFilter("Quartz", LogLevel.Debug);
```

### События обрабатываются медленно

1. Увеличьте BatchSize
2. Уменьшите интервал (cron)
3. Добавьте больше воркеров (горизонтальное масштабирование)

### Дублирование обработки

Не должно происходить при правильной настройке, но если происходит:
1. Проверьте LockDurationMinutes (должна быть больше времени обработки)
2. Используйте IdempotencyKey для критичных операций
3. Проверьте, что обработчик идемпотентен

## Best Practices

1. **Мониторьте метрики**: Количество pending, failed, processed событий
2. **Настраивайте алерты**: На накопление failed событий
3. **Логируйте подробно**: Используйте structured logging
4. **Тестируйте на продакшн-подобной нагрузке**: Перед деплоем
5. **Используйте Health Checks**: Для мониторинга состояния Jobs

