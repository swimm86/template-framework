# Конфигурация: .env, GetOptions, Module-Based Resolution

**Assembly:** `Shared.Application.Core.dll`
**Namespace:** `Shared.Application.Core.Configuration.Extensions`
**Исходники:** `src/Shared/Core/Shared.Application.Core/Configuration/Extensions/ConfigurationExtensions.cs`

---

## 🚀 Quick Start

```csharp
// Program.cs — инициализация конфигурации
var builder = WebApplication.CreateBuilder(args);

// Добавляет переменные окружения + .env-файлы
builder.Configuration.InitializeConfiguration(builder.Environment);

// appsettings.json, .env, переменные окружения — всё доступно
var connectionString = builder.Configuration.GetConnectionString("Default");

// Получение настроек по модулю (автоматически определяет из какой сборки вызван)
var options = builder.Configuration.GetOptions<ApiOptions>();
```

**.env файл:**
```env
ConnectionStrings__Default=Host=localhost;Database=mydb;Username=postgres
ApiOptions__BaseUrl=https://api.example.com
ApiOptions__Timeout=30
```

---

## Обзор

Система конфигурации Shared обеспечивает многоуровневую загрузку настроек с поддержкой `.env`-файлов и module-based разрешения.

| Компонент | Назначение |
|-----------|-----------|
| `InitializeConfiguration()` | Добавляет env vars + загрузку `.env`-файлов |
| `LoadEnv()` | Загружает `.env` и `.env.{EnvironmentName}` через DotNetEnv |
| `GetOptions<TOptions>()` | Module-based иерархическое разрешение конфигурации |

### Приоритет источников (от высшего к низшему)

| Приоритет | Источник | Пример |
|-----------|----------|--------|
| 1 (высший) | Переменные окружения ОС | `APP_GETTER_API_URL=https://prod.api` |
| 2 | `appsettings.{Environment}.json` | стандартная ASP.NET Core конфигурация |
| 3 | `appsettings.json` | стандартная ASP.NET Core конфигурация |
| 4 | `.env` ИЛИ `.env.{EnvironmentName}` (см. ⚠️ ниже) | `.env`, `.env.Development` |
| 5 (низший) | встроенные дефолты провайдеров | (например, `ChainedConfigurationProvider`) |

> **Важно:** значения из источников с более высоким приоритетом **перезаписывают** значения из источников с более низким приоритетом.

---

## .env File Support

### InitializeConfiguration

```csharp
public static void InitializeConfiguration(
    this IConfigurationBuilder configuration,
    IHostEnvironment hostEnvironment)
```

Метод-обёртка, который последовательно:
1. Добавляет переменные окружения через `AddEnvironmentVariables()`
2. Загружает `.env`-файлы через `LoadEnv(hostEnvironment)`

**Использование:**

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.InitializeConfiguration(builder.Environment);
```

> **Где вызывается.** `InitializeConfiguration` вызывается из `Shared.Presentation.Core.Extensions.WebApplicationBuilderExtensions.ImplementDependencies()` (`src/Shared/Core/Shared.Presentation.Core/Extensions/WebApplicationBuilderExtensions.cs:35`). В DI-инжекторах Shared (`DependencyInjector.Process()`) эта инициализация **не выполняется** — она уже сделана к моменту запуска авто-DI. Для консольных приложений (например, `DatabaseUpgrade`) вызов делается явно в `Program.cs` через `Host.CreateDefaultBuilder(...).ConfigureAppConfiguration(...)`.

### LoadEnv

```csharp
public static IConfigurationBuilder LoadEnv(
    this IConfigurationBuilder configurationBuilder,
    IHostEnvironment hostEnvironment)
```

Загружает `.env`-файлы из директории entry assembly.

**Алгоритм:**

1. Определяет путь к entry assembly (`Assembly.GetEntryAssembly().Location`)
2. Устанавливает текущую директорию (`Directory.SetCurrentDirectory`)
3. Проверяет наличие файлов в порядке:
   - `.env` (базовый)
   - `.env.{EnvironmentName}` (например, `.env.development`)
4. Загружает **первый найденный** файл через `AddDotNetEnv()`

### ⚠️ Приоритет `.env` vs `.env.{EnvironmentName}`

```csharp
// ConfigurationExtensions.cs:156-158
return IsValidName(EnvFileName, out var path) ||
       IsValidName($"{EnvFileName}.{hostEnvironment.EnvironmentName.ToLower()}", out path)
    ? configurationBuilder.AddDotNetEnv(path)
    : configurationBuilder;
```

Из-за логики короткого `||` приоритет такой:

| Сценарий | Что загружается |
|----------|----------------|
| Существует только `.env` | `.env` |
| Существует только `.env.{EnvironmentName}` | `.env.{EnvironmentName}` |
| **Существуют оба** | **`.env` (базовый)** — `.env.{EnvironmentName}` **игнорируется** |

То есть **`.env` имеет приоритет над `.env.{EnvironmentName}`** (при условии что оба существуют). Это поведение определено реализацией `LoadEnv` и в текущей версии является особенностью, а не багом — учитывайте это при локальной разработке.

> **Рекомендация:** если в проекте есть оба файла, держите в `.env` только базовые значения, а environment-specific overrides добавляйте через `appsettings.{Environment}.json` или переменные окружения.

### Environment-Specific Overrides

| Файл | Когда загружается |
|------|------------------|
| `.env` | Если `.env` существует — загружается **всегда** (независимо от окружения) |
| `.env.{EnvironmentName}` | Загружается **только** если `.env` отсутствует |

**Пример `.env`:**
```env
# Базовые настройки
ConnectionStrings__Default=Host=localhost;Database=mydb;Username=postgres
ApiOptions__BaseUrl=http://localhost:5000
ApiOptions__Timeout=30
```

**Пример `.env.Production`:**
```env
# Production-переопределения (загружаются только если .env отсутствует)
ConnectionStrings__Default=Host=db.prod.internal;Database=mydb;Username=svc_account
ApiOptions__BaseUrl=https://api.example.com
ApiOptions__Timeout=10
```

### Формат ключей

DotNetEnv использует `__` (двойное подчёркивание) как разделитель для вложенных секций:

```env
# appsettings.json: { "ConnectionStrings": { "Default": "..." } }
ConnectionStrings__Default=Host=localhost;Database=mydb

# appsettings.json: { "ApiOptions": { "BaseUrl": "..." } }
ApiOptions__BaseUrl=https://api.example.com

# appsettings.json: { "Logging": { "LogLevel": { "Default": "Information" } } }
Logging__LogLevel__Default=Information
```

---

## GetOptions<TOptions>() — Module-Based Resolution

```csharp
public static TOptions? GetOptions<TOptions>(this IConfiguration configuration)
    where TOptions : class
```

Уникальная особенность Shared — конфигурация разрешается **на основе имени модуля** (assembly name), из которого вызван метод.

### Алгоритм работы

1. **Определяет имя модуля** через `AssemblyHelper.GetModuleName()` (например, `App.Getter.Api`)
2. **Разбивает имя по точкам**: `["App", "Getter", "Api"]`
3. **Итеративно проходит** по каждой части, ища соответствующую секцию в конфигурации
4. **Ищет подсекцию с именем `typeof(TOptions).Name`** (например, `SettingOptions`) в каждой найденной секции — **не** `App.Getter.Api.SettingOptions`, а именно короткое имя типа
5. **Возвращает последний найденный** результат (принцип наибольшей специфичности)

### Пример

**Конфигурация (.env):**
```env
app__getter__setting__value="app.get"
app__setting__value="app"
```

**Конфигурация в виде секций:**
```text
app:getter:setting:value = "app.get"
app:setting:value = "app"
```

**Вызов из разных модулей:**

| Вызывающий модуль | Результат `GetOptions<SettingOptions>()` |
|-------------------|----------------------------------------|
| `App.Getter.Api` | `"app.get"` (берётся из секции `app:getter:setting` как последний найденный вариант) |
| `App.Setter.Api` | `"app"` (секция `app:setting`) |
| `App.Common.Api` | `"app"` (секция `app:setting`) |

### Как это работает пошагово

Для модуля `App.Getter.Api` и типа `SettingOptions` (ищется подсекция с именем `SettingOptions`):

```
Часть "App":
  → Секция "App" существует? Да
  → Секция "App.SettingOptions" существует? Нет → продолжаем

Часть "Getter":
  → Секция "App.Getter" существует? Да
  → Секция "App.Getter.SettingOptions" существует? Нет → продолжаем

Часть "Api":
  → Секция "App.Getter.Api" существует? Да
  → Секция "App.Getter.Api.SettingOptions" существует? Нет → стоп

Результат: последний найденный SettingOptions или null
```

> **Ключевой момент:** метод ищет секцию с именем `{typeof(TOptions).Name}` (без namespace), например `SettingOptions`, `ApiOptions`, `DatabaseOptions`. Не `App.Getter.Api.SettingOptions` — только конечное имя типа.

### Пример использования

**Options-класс:**
```csharp
public class ApiOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public int Timeout { get; set; } = 30;
    public bool EnableRetry { get; set; } = true;
}
```

**Конфигурация (.env):**
```env
App__Getter__ApiOptions__BaseUrl=https://getter-api.internal
App__Getter__ApiOptions__Timeout=60
App__ApiOptions__BaseUrl=https://default-api.internal
```

**Использование:**
```csharp
// Из App.Getter.Api — получит App.Getter.ApiOptions
var getterOptions = configuration.GetOptions<ApiOptions>();
// getterOptions.BaseUrl = "https://getter-api.internal"

// Из App.Setter.Api — получит App.ApiOptions (fallback)
var setterOptions = configuration.GetOptions<ApiOptions>();
// setterOptions.BaseUrl = "https://default-api.internal"
```

### Интеграция с Services

```csharp
// Program.cs — регистрация options
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return config.GetOptions<ApiOptions>() ?? new ApiOptions();
});

// Или через IOptions pattern
builder.Services.Configure<ApiOptions>(
    builder.Configuration.GetOptions<ApiOptions>()!);
```

**Использование в handler'е:**
```csharp
public class ExternalApiQueryHandler(
    IOptions<ApiOptions> options,
    IHttpClientFactory httpClientFactory)
    : IQueryHandler<ExternalDataQuery, ExternalDataResponse>
{
    public async Task<ExternalDataResponse> Handle(
        ExternalDataQuery query, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(options.Value.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(options.Value.Timeout);

        var response = await client.GetAsync("/api/data", ct);
        // ...
    }
}
```

---

## Configuration Priority

### Полная цепочка загрузки

```
1. appsettings.json              (низший приоритет, базовая конфигурация)
        ↓
2. appsettings.{Environment}.json (environment-specific overrides)
        ↓
3. .env ИЛИ .env.{EnvironmentName} — кто найден первым через LoadEnv
        ↓
4. Environment Variables         (высший приоритет, переменные ОС)
```

### Практические рекомендации

| Уровень | Что хранить | Пример |
|---------|------------|--------|
| `appsettings.json` | Дефолтные значения, не-sensitive | `Logging.LogLevel.Default=Information` |
| `.env` | Локальные настройки разработки | `ConnectionStrings__Default=Host=localhost` |
| `appsettings.{Environment}.json` | Dev-specific overrides | `ApiOptions__BaseUrl=http://localhost:5000` |
| Environment Variables | CI/CD, secrets, container runtime | `ConnectionStrings__Default` из Kubernetes Secret |

### Безопасность

- ❌ **НЕ** коммитьте `.env` с реальными credentials в репозиторий
- ✅ Используйте `.env.example` как шаблон с placeholder'ами
- ✅ Production secrets храните в Kubernetes Secrets, AWS Secrets Manager, etc.
- ✅ Environment Variables имеют высший приоритет — идеально для containerized deployments

### Настройка JSON-сериализации

`ConfigureJsonSerializer()` — регистрирует `JsonIgnoreCondition.WhenWritingNull` для Minimal API, HTTP-клиентов и MVC-контрроллеров в одном вызове. Используется внутри `Shared.Application.Core.DependencyInjection.DependencyInjector.Process()` (строка 43 `DependencyInjector.cs`).

---

## См. также

| Документ | Описание |
|----------|----------|
| [API Client](api-client.md) | HTTP-клиенты для внешних сервисов |
| [Controllers](controllers.md) | Controllers — Presentation слой |
| [DB Seeder](db-seeder.md) | Автоматическое заполнение базы данных |
| [Service Startup](service-startup.md) | `ImplementDependencies()` — точка вызова `InitializeConfiguration` |
