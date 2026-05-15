# Кэширование: CacheService, ScopedMemoryCache, CacheServiceFactory

**Assembly:** `Shared.Application.Core.dll`  
**Namespace:** `Shared.Application.Core.Cache`  
**Исходники:** `src/Shared/Core/Shared.Application.Core/Cache/`

---

## 🚀 Quick Start

```csharp
// 1. Регистрация именованного кэша с функцией загрузки данных
services.RegisterCacheService(
    "directories",
    async sp =>
    {
        var dbContext = sp.GetRequiredService<AppDbContext>();
        return await dbContext.Directories.ToListAsync();
    });

// 2. Использование в handler'е или сервисе
public class DirectoriesQueryHandler(IServiceProvider serviceProvider)
{
    public async Task<IEnumerable<DirectoryDto>> HandleAsync(CancellationToken ct)
    {
        var cache = serviceProvider.GetCacheService<List<Directory>>(key: "directories");
        var data = await cache.GetCachedDataAsync();
        return data.Select(d => new DirectoryDto(d.Id, d.Name));
    }
}

// 3. Кэш + автоматическое обновление по расписанию (Quartz)
services.RegisterCacheJob(
    "directories",
    "0 0/30 * * * ?", // каждые 30 минут
    async sp =>
    {
        var dbContext = sp.GetRequiredService<AppDbContext>();
        return await dbContext.Directories.ToListAsync();
    });
```

---

## Обзор

Подсистема кэширования Shared предоставляет три уровня кэширования:

| Компонент | Область жизни | Назначение |
|-----------|--------------|-----------|
| `CacheService<TData>` | Application lifetime | Lazy async-кэш с защитой от thundering herd |
| `ScopedMemoryCache` | Request scope | Per-request кэш для дедупликации запросов внутри одного HTTP-запроса |
| `CacheServiceFactory` | Static | Фабрика для регистрации и получения именованных `CacheService<TData>` |

Все компоненты используют `IMemoryCache` из `Microsoft.Extensions.Caching.Memory` под капотом.

---

## CacheService<TData> — Thundering Herd Prevention

`CacheService<TData>` — обёртка над `IMemoryCache`, которая решает классическую проблему **thundering herd** (когда множество одновременных запросов к пустому кэшу вызывают многократное выполнение дорогой операции загрузки данных).

### Механизм защиты

```csharp
public class CacheService<TData>(
    string key,
    IServiceProvider serviceProvider,
    Func<IServiceProvider, Task<TData>> getFunc)
    : ICacheService<TData>
{
    private readonly IMemoryCache _cache = serviceProvider.GetRequiredService<IMemoryCache>();
    private readonly object _sync = new();
    private Task<TData>? _cacheCreationTask;
```

Ключевые элементы:

| Элемент | Назначение |
|---------|-----------|
| `_sync` (object lock) | Блокировка на уровне экземпляра для сериализации доступа |
| `_cacheCreationTask` | Хранит ссылку на текущую задачу загрузки |
| `IsCompleted` check | Если задача уже выполняется — возвращает её, не создавая новую |

### Алгоритм UpdateCacheAsync

```csharp
public Task UpdateCacheAsync()
{
    lock (_sync)
    {
        // Если создание кэша уже в процессе — возвращаем текущую задачу
        if (_cacheCreationTask is { IsCompleted: false })
        {
            return _cacheCreationTask;
        }

        _cache.Remove(key);
        return _cacheCreationTask = GetCachedDataAsync();
    }
}
```

1. **lock (_sync)** — только один поток может войти в критическую секцию
2. **IsCompleted check** — если `_cacheCreationTask` ещё выполняется, все остальные потоки получают ту же задачу
3. **Remove + GetOrCreateAsync** — удаляет старый кэш и запускает новую загрузку

### Алгоритм GetCachedDataAsync

```csharp
public Task<TData> GetCachedDataAsync() =>
    _cache.GetOrCreateAsync(key, _ => getFunc(serviceProvider))!;
```

Использует `IMemoryCache.GetOrCreateAsync` — если данные уже в кэше, возвращает их без вызова `getFunc`.

### Интерфейс ICacheService<TData>

| Метод | Описание |
|-------|----------|
| `Task<TData> GetCachedDataAsync()` | Получить данные из кэша (загрузить при отсутствии) |
| `Task UpdateCacheAsync()` | Принудительно обновить кэш (thread-safe) |

---

## ScopedMemoryCache — Per-Request Кэширование

`ScopedMemoryCache` — реализация `IScopedMemoryCache`, ограниченная временем жизни одного HTTP-запроса (scoped DI lifetime).

### Когда использовать

- Дедупликация повторных запросов к одним данным **в пределах одного HTTP-запроса**
- Кэширование промежуточных результатов внутри pipeline обработки
- Ситуации, когда несколько handler'ов/сервисов обращаются к одним данным

### API

```csharp
public class ScopedMemoryCache : IScopedMemoryCache, IDisposable
{
    // Получить или создать значение (sync)
    public T? GetOrCreate<T>(string key, Func<ICacheEntry, T> factory);

    // Получить или создать значение (async)
    public Task<T?> GetOrCreateAsync<T>(string key, Func<ICacheEntry, Task<T>> factory);

    // Попытка получить значение без создания
    public bool TryGetValue<T>(string key, out T? value);

    // Удалить конкретный ключ
    public void Remove(string key);

    // Очистить весь кэш (пересоздаёт MemoryCache)
    public void Clear();

    // Dispose — освобождает ресурсы
    public void Dispose();
}
```

### Debug-логирование

Каждая операция логируется на уровне `Debug`:

```csharp
public ScopedMemoryCache(ILogger<ScopedMemoryCache> logger)
{
    _cache = new MemoryCache(new MemoryCacheOptions());
    _logger = logger;
    _logger.LogDebug("Создан новый экземпляр ScopedMemoryCache");
}
```

| Операция | Лог-сообщение |
|----------|--------------|
| Создание | `Создан новый экземпляр ScopedMemoryCache` |
| GetOrCreate | `Запрос значения из кэша для ключа {Key}` |
| GetOrCreateAsync | `Асинхронный запрос значения из кэша для ключа {Key}` |
| Remove | `Удаление значения из кэша для ключа {Key}` |
| Clear | `Очистка всего кэша` |
| Dispose | `Освобождение ресурсов ScopedMemoryCache` |

### Регистрация в DI

```csharp
// Program.cs
services.AddScoped<IScopedMemoryCache, ScopedMemoryCache>();
```

Автоматически dispose'ится при завершении запроса.

---

## CacheServiceFactory — Именованные Кэши

`CacheServiceFactory` — статический класс с extension-методами для регистрации и получения именованных `CacheService<TData>` через keyed DI.

### RegisterCacheService<TData>

```csharp
public static IServiceCollection RegisterCacheService<TData>(
    this IServiceCollection serviceCollection,
    string key,
    Func<IServiceProvider, Task<TData>> getOrAddFunc)
```

| Параметр | Описание |
|----------|----------|
| `key` | Уникальный ключ кэша (обязателен, не null/empty) |
| `getOrAddFunc` | Функция загрузки данных при cache miss |

**Что делает:**
1. Вызывает `AddMemoryCache()` (безопасно при повторных вызовах)
2. Регистрирует `ICacheService<TData>` как keyed singleton через `AddKeyedSingleton`

### GetCacheService<TData>

```csharp
public static ICacheService<TData> GetCacheService<TData>(
    this IServiceProvider serviceProvider,
    string key)
```

Получает зарегистрированный `ICacheService<TData>` по ключу. Выбрасывает `CacheNotFoundException` если кэш не найден.

### GetCachedDataAsync<TData>

```csharp
public static Task<TData> GetCachedDataAsync<TData>(
    this IServiceProvider serviceProvider,
    string key)
```

Удобный метод — получает кэш и сразу возвращает данные одной строкой.

### CacheNotFoundException

```csharp
public class CacheNotFoundException(string cacheKey)
    : AppException($"Отсутствует кэш с ключом {cacheKey}.");
```

Выбрасывается при попытке получить незарегистрированный кэш. Наследуется от `AppException`.

---

## Scheduled Cache Refresh с Quartz

`QuartzJobRegistrar.RegisterCacheJob<TData>` объединяет регистрацию кэша и Quartz-задачи для автоматического обновления кэша по расписанию.

### RegisterCacheJob с CRON

```csharp
public static IServiceCollection RegisterCacheJob<TData>(
    this IServiceCollection serviceCollection,
    string cacheKey,
    string cronExpression,
    Func<IServiceProvider, Task<TData>> getOrCreateCacheFunc)
```

**Что делает:**
1. Вызывает `RegisterCacheService(cacheKey, getOrCreateCacheFunc)` — регистрирует кэш
2. Вызывает `RegisterJob("{cacheKey}.job", cronExpression, ...)` — регистрирует Quartz-задачу
3. Задача вызывает `cacheService.UpdateCacheAsync()` — обновляет кэш по расписанию

### RegisterCacheJob с JobTriggerFlags

```csharp
public static IServiceCollection RegisterCacheJob<TData>(
    this IServiceCollection serviceCollection,
    string cacheKey,
    JobTriggerFlags trigger,
    TimeSpan specificTime,
    Func<IServiceProvider, Task<TData>> getOrCreateCacheFunc)
```

Альтернатива CRON — использование флагов триггеров:

| Флаг | Расписание |
|------|-----------|
| `JobTriggerFlags.OnStartup` | При запуске приложения |
| `JobTriggerFlags.EveryMinute` | Каждую минуту |
| `JobTriggerFlags.EveryHour` | Каждый час |
| `JobTriggerFlags.Daily` | Ежедневно |
| `JobTriggerFlags.Weekly` | Еженедельно |
| `JobTriggerFlags.Monthly` | Ежемесячно |

Флаги можно комбинировать через `|`.

### Примеры

**CRON-выражение — каждые 5 минут:**

```csharp
services.RegisterCacheJob(
    "test",
    "0 0/5 * * * ?",
    _ => Task.FromResult(Enumerable.Range(0, 10).Select(i => $"item {i}").ToArray()));
```

**Флаги — ежедневно + каждую минуту:**

```csharp
services.RegisterCacheJob(
    "test",
    JobTriggerFlags.Daily | JobTriggerFlags.EveryMinute,
    new TimeSpan(0, 0, 0, 0),
    _ => Task.FromResult(Enumerable.Range(0, 10).Select(i => $"item {i}").ToArray()));
```

### Интеграция с CQRS

```csharp
// Program.cs — регистрация
services.RegisterCacheJob(
    "person-cache",
    "0 0/15 * * * ?", // каждые 15 минут
    async sp =>
    {
        var dbContext = sp.GetRequiredService<AppDbContext>();
        return await dbContext.Persons
            .AsNoTracking()
            .ToListAsync();
    });

// Query handler — использование
public class PersonListQueryHandler(IServiceProvider serviceProvider)
    : IQueryHandler<PersonReadListQuery, PersonListResponse>
{
    public async Task<PersonListResponse> Handle(
        PersonReadListQuery query, CancellationToken ct)
    {
        var persons = await serviceProvider.GetCachedDataAsync<List<Person>>(
            key: "person-cache");

        var filtered = persons
            .Where(p => MatchesFilter(p, query.Filter))
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return new PersonListResponse(filtered);
    }
}
```

---

---

## См. также

| Документ | Описание |
|----------|----------|
| [Quartz Jobs](quartz-jobs.md) | Планировщик Quartz — RegisterJob, JobTriggerFlags |
| [CQRS](cqrs.md) | Разделение команд и запросов |
| [Configuration](configuration.md) | Управление конфигурацией и .env |
