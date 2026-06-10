# Database Upgrade

## Обзор

**Assembly:** `Shared.Utils.DatabaseUpgrade.dll`
**Namespace:** `Shared.Utils.DatabaseUpgrade`
**Исходники:** `src/Shared/Utils/Shared.Utils.DatabaseUpgrade/` (`DbMigrator.cs`, `DbUtils.cs`)

Утилита для выполнения SQL-миграций на PostgreSQL с использованием библиотеки **DbUp**. Поддерживает выполнение embedded-скриптов из сборки, автоматическое создание базы данных при отсутствии и CLI-интерфейс для запуска миграций.

---

## DbMigrator

**Класс:** `DbMigrator` (`Shared.Utils.DatabaseUpgrade`)

Внутренний класс-обёртка над DbUp для выполнения миграций.

### Как работает

1. **Проверка существования БД** — `EnsureDatabase.For.PostgresqlDatabase(connectionString)` проверяет и создаёт базу данных, если она отсутствует
2. **Определение сборки** — если `scriptsAssemblyName` не указан, определяется сборка вызывающего кода через `StackTrace`
3. **Поиск скриптов** — фильтрует embedded resources по префиксу `{assemblyName}.{scriptsPath}` (по умолчанию `Scripts`)
4. **Выполнение** — `DeployChanges.To.PostgresqlDatabase(...).WithScriptsEmbeddedInAssembly(...).LogToConsole().Build().PerformUpgrade()`

### Метод

```csharp
public static void Upgrade(
    string connectionString,
    string? scriptsPath = null,
    string? scriptsAssemblyName = null)
```

| Параметр | Тип | Описание |
|----------|-----|----------|
| `connectionString` | `string` | Строка подключения к PostgreSQL |
| `scriptsPath` | `string?` | Путь к папке embedded-скриптов (например, `"DatabaseUpgrade.Scripts"`) |
| `scriptsAssemblyName` | `string?` | Имя сборки со скриптами. Если `null` — определяется автоматически |

### Автоматическое определение сборки

Метод `GetAssembly()` анализирует `StackTrace` и находит последнюю сборку, которая ссылается на `Shared.Utils.DatabaseUpgrade`:

```csharp
private static Assembly? GetAssembly()
    => new StackTrace().GetFrames()
        .Select(x => x.GetMethod()?.ReflectedType?.Assembly).Distinct()
        .LastOrDefault(x =>
            x?.GetReferencedAssemblies().Any(y => y.FullName == Assembly.GetExecutingAssembly().FullName) == true);
```

### Пример использования

```csharp
// Прямой вызов
DbMigrator.Upgrade(
    connectionString: "Host=localhost;Database=mydb;Username=postgres;Password=secret",
    scriptsPath: "DatabaseUpgrade.Scripts",
    scriptsAssemblyName: "MyService.DatabaseUpgrade");
```

---

## DbUtils

**Класс:** `DbUtils` (`Shared.Utils.DatabaseUpgrade`)

Публичный CLI-интерфейс для запуска миграций. Читает connection string из UserSecrets или `appsettings.json`.

### Методы

| Метод | Описание |
|-------|----------|
| `Upgrade(string[] args)` | CLI-точка входа. Принимает аргументы командной строки с путями к скриптам |
| `Upgrade(string? connectionString, string? connectionStringKey, string? scriptsPath, string? scriptsAssemblyName)` | Программный вызов с явными параметрами |
| `GetConnectionStringFromSecrets<T>(string? key)` | Получение connection string из UserSecrets по типу |
| `GetConnectionStringFromSecrets(Assembly, string? key)` | Получение connection string из UserSecrets по сборке |

### CLI Usage

```bash
# Запуск из командной строки
dotnet run -- "DatabaseUpgrade.Scripts"

# Несколько путей через запятую
dotnet run -- "DatabaseUpgrade.Scripts.V1,DatabaseUpgrade.Scripts.V2"
```

Аргументы читаются из:
1. Первого аргумента командной строки (`args[0]`)
2. Переменной окружения `ScriptPaths` (если аргументы не переданы)

### Порядок поиска Connection String

1. **UserSecrets** — `ConfigurationBuilder.AddUserSecrets<T>()` → секция `ConnectionString`
2. **appsettings.json** — `AddJsonFile("appsettings.json")` → секция `ConnectionString`
3. **appsettings.{Environment}.json** — `AddJsonFile($"appsettings.{ASPNETCORE_ENVIRONMENT}.json")` → секция `ConnectionString`
4. **Environment Variables** — `AddEnvironmentVariables()` → секция `ConnectionString`

### Пример программного вызова

```csharp
// С параметрами по умолчанию (connection string из UserSecrets)
DbUtils.Upgrade(scriptsPath: "DatabaseUpgrade.Scripts");

// С явным connection string
DbUtils.Upgrade(
    connectionString: "Host=localhost;Database=mydb;Username=postgres;Password=secret",
    scriptsPath: "DatabaseUpgrade.Scripts");

// С кастомным ключом
DbUtils.Upgrade(
    connectionStringKey: "MyCustomConnection",
    scriptsPath: "DatabaseUpgrade.Scripts");

// Получение connection string из UserSecrets
var cs = DbUtils.GetConnectionStringFromSecrets<Program>();
```

### Структура скриптов

SQL-скрипты должны быть добавлены как **Embedded Resource** в проект:

```
MyService.DatabaseUpgrade/
├── Scripts/
│   ├── 001_CreateUsersTable.sql
│   ├── 002_AddEmailColumn.sql
│   └── 003_CreateOrdersTable.sql
├── DbMigrator.cs
└── DbUtils.cs
```

В `.csproj`:

```xml
<ItemGroup>
  <EmbeddedResource Include="Scripts\*.sql" />
</ItemGroup>
```

### Именование скриптов

DbUp выполняет скрипты в **алфавитном порядке**. Рекомендуется использовать числовой префикс:

```
001_InitialSchema.sql
002_AddUsersTable.sql
010_AddOrdersTable.sql
```

---

## Отслеживание выполненных миграций

DbUp автоматически создаёт таблицу `schemaversions` в базе данных для отслеживания выполненных скриптов:

| Колонка | Описание |
|---------|----------|
| `schemaversionid` | Уникальный идентификатор версии |
| `version` | Номер версии (имя скрипта) |
| `scriptname` | Имя скрипта |
| `applied` | Дата применения |

Повторный запуск пропускает уже выполненные скрипты.

---

---

## См. также

| Документ | Описание |
|----------|----------|
| [EF Core Internals](efcore-internals.md) | Внутреннее устройство EF Core |
| [DB Seeder](db-seeder.md) | Начальное заполнение базы данных |
| [Configuration](configuration.md) | Управление конфигурацией и UserSecrets |
