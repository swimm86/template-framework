# 🌱 Db Seeder — Автоматическое заполнение базы данных

> **Assembly:** `Shared.Application.Core.dll`  
> **Namespace:** `Shared.Application.Core.Dal.DbSeeder`

---

## 1. Обзор

Система **Db Seeder** предназначена для автоматического заполнения базы данных начальными данными (seed data) при первом запуске приложения или после миграций. Каждый seed выполняется **один раз** — информация о выполненных сидах сохраняется в таблице `seed`, что предотвращает повторное выполнение.

### Ключевые особенности

| Возможность | Описание |
|-------------|----------|
| **Автоматическое обнаружение** | Все классы с атрибутом `[Seed]` находятся через reflection |
| **Идемпотентность** | Выполненные сиды не запускаются повторно |
| **Упорядоченное выполнение** | Сиды выполняются в порядке, заданном через `Order` |
| **DI-совместимость** | Конструкторы сидов разрешаются через `IServiceProvider` |

---

## 2. Создание Seed

### 2.1. Атрибут `[Seed]`

Каждый seed-класс должен быть помечен атрибутом `[Seed]`, который задаёт имя и порядок выполнения. Оба параметра передаются **позиционно** (не именованно):

```csharp
[Seed("CreateDefaultRoles", 1)]
public class CreateDefaultRolesSeed : ISeed
{
    // ...
}
```

| Параметр (позиция) | Тип | Описание |
|----------|-----|----------|
| 1 — `Name` | `string` | Уникальное имя сида. Используется для отслеживания выполнения |
| 2 — `Order` | `int` | Порядок выполнения. Меньшие значения выполняются раньше |

### 2.2. Интерфейс `ISeed`

Все сиды реализуют интерфейс `ISeed`:

```csharp
public interface ISeed
{
    /// <summary>
    /// Реализует seed-процесс.
    /// </summary>
    Task SeedAsync();
}
```

### 2.3. Пример реализации

```csharp
using Shared.Application.Core.Dal.DbSeeder.Attributes;
using Shared.Application.Core.Dal.DbSeeder.Interfaces;

namespace MyService.Infrastructure.Seeds;

[Seed("CreateDefaultUser", 10)]
public class CreateDefaultUserSeed : ISeed
{
    private readonly IMyDbContext _dbContext;

    public CreateDefaultUserSeed(IMyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SeedAsync()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "admin",
            Email = "admin@example.com",
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
    }
}
```

---

## 3. DbSeeder — Применение сидов

### 3.1. Класс `DbSeeder`

Класс `DbSeeder` отвечает за обнаружение и применение всех сидов:

```csharp
public class DbSeeder(
    IUnitOfWork unitOfWork,
    IServiceProvider serviceProvider,
    ILogger<DbSeeder>? logger = null)
    : IDbSeeder
{
    public async Task ApplySeedsAsync(CancellationToken cancellationToken);
}
```

### 3.2. Алгоритм работы

1. Получает список уже выполненных сидов из таблицы `seed`
2. Сканирует все загруженные сборки на наличие классов с атрибутом `[Seed]`
3. Исключает уже выполненные сиды (по `Name`)
4. Сортирует оставшиеся сиды по `Order`
5. Для каждого сида:
   - Создаёт экземпляр через `ActivatorUtilities.CreateInstance(serviceProvider, type)`
   - Вызывает `SeedAsync()`
   - Записывает имя сида в таблицу `seed`
6. Сохраняет изменения через `IUnitOfWork.SaveChangesAsync()`

### 3.3. Интеграция с IDbUpdater

`DbSeeder` вызывается внутри `IDbUpdater.Initialize()`:

```csharp
public class DbUpdater : IDbUpdater
{
    private readonly MyDbContext _dbContext;
    private readonly IDbSeeder _dbSeeder;

    public void Initialize()
    {
        CreateDbIfNotExists();
        Migrate();
        _dbSeeder.ApplySeedsAsync(CancellationToken.None).GetAwaiter().GetResult();
    }
}
```

---

## 4. Сущность Seed

### 4.1. Класс `Seed`

Сущность для отслеживания выполненных сидов в базе данных. Имя сида хранится **в `Id`**, поскольку `Seed : IEntity<string>`:

```csharp
public class Seed
    : IEntity<string>
{
    public string Id { get; init; }

    private Seed() { }

    public static Seed Create(string name) => new() { Id = name };
}
```

| Свойство | Тип | Описание |
|----------|-----|----------|
| `Id` | `string` | Первичный ключ. Используется как имя сида (например, `"person"`) |

> **Семантика:** в `Seed` нет отдельного `Name`-свойства — роль уникального идентификатора сида выполняет сам `Id` типа `string`.

### 4.2. EF Core конфигурация — `SeedConfigurationBase`

Реальная реализация (`src/Shared/Dal/Shared.Infrastructure.Dal.EFCore/Configurations/SeedConfigurationBase.cs`):

```csharp
public abstract class SeedConfigurationBase
    : IEntityTypeConfiguration<Seed>
{
    public void Configure(EntityTypeBuilder<Seed> builder)
    {
        builder.ToTable("seed", t => t.HasComment("Таблица с сущностями \"Сид БД\"."));
        builder.Property(x => x.Id)
            .HasComment("Уникальное наименование сида БД.");
    }
}
```

| Настройка | Значение |
|-----------|----------|
| Таблица | `seed` |
| Primary Key | Не задаётся явно в базовом классе — `Id` (`string`) является ключом по контракту `IEntity<string>`. Производные конфигурации в проекте могут доопределить `HasKey(x => x.Id)`. |
| Наследование | Абстрактный базовый класс — каждый сервис создаёт свою конфигурацию |

---

## 5. IDbUpdater — Управление жизненным циклом БД

### 5.1. Интерфейс

```csharp
public interface IDbUpdater
{
    void CreateDbIfNotExists();
    void Migrate();
    void Initialize();
}
```

| Метод | Описание |
|-------|----------|
| `CreateDbIfNotExists()` | Создаёт базу данных, если она ещё не существует |
| `Migrate()` | Применяет все pending миграции EF Core |
| `Initialize()` | Полный цикл: создание → миграция → сиды |

### 5.2. Использование при старте приложения

```csharp
var app = builder.Build();

// Инициализация БД при старте
using (var scope = app.Services.CreateScope())
{
    var dbUpdater = scope.ServiceProvider.GetRequiredService<IDbUpdater>();
    dbUpdater.Initialize();
}

app.Run();
```

---

## 6. `DbSeederJob` — запуск seed-ов через Job Scheduler

### 6.1. Назначение

`DbSeederJob` — фоновая задача (`IScheduledJob`), которая оборачивает `IDbSeeder` и регистрирует его в [Job Scheduler](job-scheduler.md). Задача решает две проблемы:

- **Идемпотентность в рамках процесса.** `DbSeeder` сам по себе идемпотентен на уровне БД (таблица `seed`), но если планировщик по какой-то причине перезапускает итерацию внутри одного процесса (например, при ошибке и срабатывании `RetryOptions`) — повторно выполнять `ApplySeedsAsync` дорого и бессмысленно.
- **Участие в retry-политике.** Если при первом старте БД ещё не готова (накатываются миграции, временно недоступна и т.п.), задача ретраит с задержкой 5 минут до 100 раз.

### 6.2. Регистрация

```csharp
services.RegisterDbSeederJob();
```

Метод `RegisterDbSeederJob` (`Shared.Application.Core.Job.Extensions.DbSeederExtensions`) делает три вещи:

1. Регистрирует `DbSeederJob` в DI как `Scoped`.
2. Подключает фоновую задачу через `AddJobs(...)` с расписанием `JobSchedule.OnStartup()`.
3. Навешивает на задачу `RetryOptions { Delay = 5 минут, MaxAttempts = 100 }` — при неудаче повтор каждые 5 минут до 100 попыток (итого ~8 часов непрерывных попыток).

`RegisterDbSeederJob` **не зависит от Quartz** — выбор провайдера (`Shared.Infrastructure.Job.Quartz` или `Shared.Infrastructure.Job.Hangfire`) определяется проектной ссылкой сервиса.

### 6.3. Идемпотентность

```csharp
public sealed class DbSeederJob(IDbSeeder seeder) : IScheduledJob
{
    private static int _seeded;

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _seeded, 1, 0) != 0)
        {
            return;
        }

        try
        {
            await seeder.ApplySeedsAsync(cancellationToken);
        }
        catch
        {
            Interlocked.Exchange(ref _seeded, 0);
            throw;
        }
    }

    internal static void ResetSeedFlag() => Interlocked.Exchange(ref _seeded, 0);
}
```

| Аспект | Описание |
|--------|----------|
| **Хранение флага** | `static int _seeded` + `Interlocked.CompareExchange(ref _seeded, 1, 0)` — атомарный test-and-set. Гарантирует ровно один «победитель» среди конкурирующих итераций в рамках процесса. |
| **Назначение** | Если retry-middleware повторно вызывает `ExecuteAsync` (например, в первой попытке был `DbUpdateException` из-за блокировки), второй и последующие вызовы мгновенно выходят. |
| **Сброс при ошибке** | Если `ApplySeedsAsync` бросил исключение, флаг сбрасывается через `Interlocked.Exchange(ref _seeded, 0)` — следующая retry-итерация получает шанс выполнить сиды снова. |
| **Гарантии** | Гарантирует **не более одного выполнения `ApplySeedsAsync` за время жизни процесса** при условии, что хотя бы один вызов завершился успешно. Не защищает от параллельного запуска нескольких инстансов сервиса на одной БД — для этого полагайтесь на таблицу `seed`. |
| **`ResetSeedFlag`** | `internal static` метод. Существует **исключительно для unit-тестов** `Shared.Application.Core.Tests`, где каждый `[Fact]` должен стартовать из «чистого» состояния. В production-коде вызывать не нужно — статический флаг уже удерживается процессом, а задача `OnStartup` выполняется ровно один раз. Помечен `internal`, чтобы случайное использование из бизнес-кода приводило к ошибке компиляции. |

### 6.4. Связь с обычным `IDbUpdater.Initialize()`

`DbSeederJob` **не подменяет** `IDbUpdater` — оба могут сосуществовать. Типичный сценарий:

1. `IDbUpdater.Initialize()` синхронно накатывает миграции и вызывает `ApplySeedsAsync` при первом старте.
2. `DbSeederJob` страхует от ситуации, когда `Initialize` не был вызван (например, в сервисах, где нет `IDbUpdater`), или когда `Initialize` упал, но приложение должно стартовать и попробовать позже.

В большинстве сервисов достаточно **одного из двух** механизмов. Выбор зависит от того, что критичнее: гарантированный seed до старта приложения (`IDbUpdater`) или устойчивость к отказам БД на старте (`DbSeederJob`).

---

---

## См. также

| Документ | Описание |
|----------|----------|
| [EF Core Internals](efcore-internals.md) | Внутреннее устройство EF Core слоя |
| [Database Upgrade](database-upgrade.md) | SQL-миграции с использованием DbUp |
| [Entity Interfaces](entity-interfaces.md) | Интерфейсы сущностей (IEntity<TKey>) |
