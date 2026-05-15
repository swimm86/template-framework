# Конфигурация: .env, GetOptions, Module-Based Resolution

**Assembly:** `Shared.Application.Core.dll`  
**Namespace:** `Shared.Application.Core.Configuration.Extensions`  
**Исходники:** `src/Shared/Core/Shared.Application.Core/Configuration/Extensions/`

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
| 2 | `.env.{EnvironmentName}` | `.env.Production`, `.env.Development` |
| 3 | `.env` (базовый) | `.env` |
| 4 (низший) | `appsettings.json` | Стандартная конфигурация ASP.NET Core |

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
   - `.env.{EnvironmentName}` (например, `.env.Development`)
   - `.env` (базовый)
4. Загружает **первый найденный** файл через `AddDotNetEnv()`

> **Примечание:** загружается только один файл — environment-specific имеет приоритет над базовым `.env`. Если найден `.env.Development`, базовый `.env` не загружается.

### Environment-Specific Overrides

| Файл | Когда загружается |
|------|------------------|
| `.env` | Всегда, если нет environment-specific файла |
| `.env.Development` | Когда `ASPNETCORE_ENVIRONMENT=Development` |
| `.env.Production` | Когда `ASPNETCORE_ENVIRONMENT=Production` |
| `.env.Staging` | Когда `ASPNETCORE_ENVIRONMENT=Staging` |

**Пример `.env`:**
```env
# Базовые настройки
ConnectionStrings__Default=Host=localhost;Database=mydb;Username=postgres
ApiOptions__BaseUrl=http://localhost:5000
ApiOptions__Timeout=30
```

**Пример `.env.Production`:**
```env
# Production-переопределения
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
4. **Ищет подсекцию** с именем типа `TOptions` в каждой найденной секции
5. **Возвращает последний найденный** результат (принцип наибольшей специфичности)

### Пример

**Конфигурация:**
```env
app__getter__setting__value="app.get"
app__setting__value="app"
```

**Вызов из разных модулей:**

| Вызывающий модуль | Результат `GetOptions<SettingOptions>()` |
|-------------------|----------------------------------------|
| `App.Getter.Api` | `"app.get"` (секция `App.Getter.Setting`) |
| `App.Setter.Api` | `"app"` (секция `App.Setting`) |
| `App.Common.Api` | `"app"` (секция `App.Setting`) |

### Как это работает пошагово

Для модуля `App.Getter.Api` и типа `SettingOptions`:

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

> **Примечание:** метод ищет секцию с именем `{typeof(TOptions).Name}` (без namespace), например `SettingOptions`, `ApiOptions`, `DatabaseOptions`.

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
1. appsettings.json          (низший приоритет, базовая конфигурация)
        ↓
2. appsettings.{Environment}.json  (environment-specific overrides)
        ↓
3. .env                      (базовый .env-файл)
        ↓
4. .env.{EnvironmentName}    (environment-specific .env, если найден)
        ↓
5. Environment Variables     (высший приоритет, переменные ОС)
```

### Практические рекомендации

| Уровень | Что хранить | Пример |
|---------|------------|--------|
| `appsettings.json` | Дефолтные значения, не-sensitive | `Logging.LogLevel.Default=Information` |
| `.env` | Локальные настройки разработки | `ConnectionStrings__Default=Host=localhost` |
| `.env.Development` | Dev-specific overrides | `ApiOptions__BaseUrl=http://localhost:5000` |
| `.env.Production` | Production-настройки | `ConnectionStrings__Default=Host=db.prod` |
| Environment Variables | CI/CD, secrets, container runtime | `ConnectionStrings__Default` из Kubernetes Secret |

### Безопасность

- ❌ **НЕ** коммитьте `.env` с реальными credentials в репозиторий
- ✅ Используйте `.env.example` как шаблон с placeholder'ами
- ✅ Production secrets храните в Kubernetes Secrets, AWS Secrets Manager, etc.
- ✅ Environment Variables имеют высший приоритет — идеально для containerized deployments

---

---

## См. также

| Документ | Описание |
|----------|----------|
| [API Client](api-client.md) | HTTP-клиенты для внешних сервисов |
| [Controllers](controllers.md) | Controllers — Presentation слой |
| [DB Seeder](db-seeder.md) | Автоматическое заполнение базы данных |
