# Service Startup — Загрузка приложения

**Assembly:** `Shared.Presentation.Core.dll`, `Shared.Infrastructure.Core.dll`, `Shared.Application.Core.dll`  
**Namespace:** `Shared.Presentation.Core.Extensions`, `Shared.Infrastructure.Core.DependencyInjection.Extensions`

---

## Обзор

Фреймворк Shared предоставляет унифицированный механизм загрузки (bootstrap) .NET-приложения, состоящий из двух этапов:

1. **`ImplementDependencies()`** — настройка сервисов и DI-контейнера на `WebApplicationBuilder`
2. **`UsePresentationCore()`** — конфигурация middleware-пайплайна на `WebApplication`

Весь процесс автоматизирован через `DependencyInjectorBase` — каждый слой фреймворка предоставляет собственный инжектор, который обнаруживается и выполняется автоматически через reflection.

---

## ImplementDependencies() — Сервисы

Метод расширения для `WebApplicationBuilder`, выполняющий полную регистрацию зависимостей:

```csharp
// WebApplicationBuilderExtensions.cs
public static WebApplicationBuilder ImplementDependencies(this WebApplicationBuilder builder)
{
    builder.Configuration.InitializeConfiguration(builder.Environment);
    builder.Services
        .AddControllers(options =>
        {
            options.Conventions.Add(new ControllerTypeConvention());
            options.Conventions.Add(new ControllerNameConvention());
            options.Filters.Add<RequestLoggingFilter>();
        }).Services
        .AddReferencedDependencyInjectors();
    return builder;
}
```

### Порядок выполнения

| Шаг | Действие | Описание |
|-----|---------|----------|
| 1 | `InitializeConfiguration` | Загрузка `.env` и переменных окружения (см. [configuration.md](configuration.md)) |
| 2 | `AddControllers` | Регистрация контроллеров с конвенциями маршрутизации |
| 3 | `ControllerTypeConvention` | Автоматическая подстановка `[appName]`, `[controllerType]` в маршруты |
| 4 | `ControllerNameConvention` | Преобразование имён контроллеров в kebab-case |
| 5 | `RequestLoggingFilter` | Фильтр логирования входящих HTTP-запросов |
| 6 | `AddReferencedDependencyInjectors` | Автообнаружение и запуск всех `DependencyInjectorBase` |

### AddReferencedDependencyInjectors

Главный механизм авто-регистрации. Выполняет:

1. **Загрузка ссылочных сборок** через `DependencyContext.Default` — загружает все project-сборки из рантайм-зависимостей
2. **Обнаружение** всех неабстрактных наследников `DependencyInjectorBase` через `AssemblyHelper.GetDerivedTypesFromAssemblies`
3. **Сортировка** — Shared-инжекторы регистрируются первыми, затем конкретные модули. Порядок внутри Shared: `Application` → `Infrastructure`
4. **Инстанцирование** через `ActivatorUtilities.CreateInstance` — DI-контейнер автоматически резолвит параметры конструктора
5. **Вызов** `Inject(serviceCollection)` — каждый инжектор регистрирует свои сервисы

```csharp
// ServiceCollectionExtensions.cs
public static IServiceCollection AddReferencedDependencyInjectors(this IServiceCollection services)
{
    LoadReferencedProjects();  // Загрузка assembly из DependencyContext

    using var provider = services.BuildServiceProvider();

    // Сортировка: Shared → Module, Application → Infrastructure
    var dependencyInjectorTypes =
        AssemblyHelper.GetDerivedTypesFromAssemblies<DependencyInjectorBase>()
            .OrderBy(x => !x.FullName!.StartsWith(nameof(Shared), StringComparison.OrdinalIgnoreCase))
            .ThenBy(x => !x.FullName!.Contains(nameof(Application), StringComparison.OrdinalIgnoreCase))
            .ThenBy(x => !x.FullName!.Contains(nameof(Infrastructure), StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(x => x.FullName!.Split('.').Length)
            .ToArray();

    dependencyInjectorTypes.ForEach(x => services.ApplyDependencyInjector(provider, x));
    return services;
}
```

---

## UsePresentationCore() — Middleware

Метод расширения для `WebApplication`, конфигурирующий middleware-пайплайн:

```csharp
// ApplicationBuilderExtensions.cs (Shared.Presentation.Core)
public static IApplicationBuilder UsePresentationCore(this WebApplication app)
{
    LoggingServiceAccessor.Configure(app.Services);

    app.UseCorrelationId();
    if (app.Environment.IsDevelopment())
    {
        app.UseSwaggerConfigured();
    }

    app.UseExceptionHandler();
    app.UseAuthorization();
    app.MapControllers();

    return app;
}
```

### Порядок middleware

| Порядок | Middleware | Описание |
|---------|-----------|----------|
| 1 | `UseCorrelationId` | Генерация/проброс Correlation ID для трассировки запросов |
| 2 | `UseSwaggerConfigured` | Swagger UI (только Development) |
| 3 | `UseExceptionHandler` | Глобальный обработчик исключений → ProblemDetails |
| 4 | `UseAuthorization` | Авторизация |
| 5 | `MapControllers` | Маршрутизация запросов к контроллерам |

**Важно:** порядок критичен. Correlation ID должен быть первым — он добавляет заголовок `X-Correlation-Id` для всех последующих middleware и log-записей.

### UseCommonPresentation (пример из Template)

В сервисах микросервисов `UsePresentationCore` оборачивается в `UseCommonPresentation`, добавляя CORS:

```csharp
// Template.Presentation/Extensions/ApplicationBuilderExtensions.cs
public static IApplicationBuilder UseCommonPresentation(this WebApplication app)
{
    return app
        .UsePresentationCore()
        .UseCors(Constants.CorsDefaultPolicyName);
}
```

---

## Per-layer DependencyInjectors

Каждый слой фреймворка предоставляет собственный `DependencyInjector`, наследующий `DependencyInjectorBase`:

### DependencyInjectorBase

```csharp
public abstract class DependencyInjectorBase
{
    protected readonly ILogger Logger;

    protected DependencyInjectorBase(ILoggerFactory loggerFactory)
    {
        Logger = loggerFactory.CreateLogger(GetType());
    }

    public IServiceCollection Inject(IServiceCollection serviceCollection)
    {
        try
        {
            var result = Process(serviceCollection);
            Logger.LogInformation("Dependencies injected.");
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Dependencies not injected.");
            throw;
        }
    }

    protected abstract IServiceCollection Process(IServiceCollection serviceCollection);
}
```

Template Method Pattern: открытый `Inject()` — логирование и обработка ошибок; абстрактный `Process()` — регистрация сервисов.

### Сводная таблица инжекторов

| Слой | Assembly | Что регистрирует |
|------|----------|------------------|
| Application.Core | `Shared.Application.Core` | `IHttpContextAccessor`, JSON-сериализация, репозитории (`IRepository<>`), DbSeeder, PropertyUtil, `IUriValidator`, `IResponseValidator`, `IScopedMemoryCache` |
| Application.Cqrs.Core | `Shared.Application.Cqrs.Core` | MediatR |
| Infrastructure.Core | `Shared.Infrastructure.Core` | ApiClient: DelegatingHandlers, HttpClient-конфигурация |
| Presentation.Core | `Shared.Presentation.Core` | Swagger, FluentValidation, ExceptionHandling, EndpointsApiExplorer |
| Dal.EFCore.Postgres | `Shared.Infrastructure.Dal.EFCore.Postgres` | Npgsql legacy timestamp, `IDbContextOptionsBuilderInitializer`, DbContext'ы, `IQueryEvaluator` → `EfQueryEvaluator`, `IRepository<>` → `EfRepository<>`, `IUnitOfWork` |
| Logging | `Shared.Infrastructure.Logging` | NLog |
| Mapper | `Shared.Infrastructure.Mapper.AutoMapper` | AutoMapper profiles, `IMapper` → `Mapper` |
| Job.Quartz | `Shared.Infrastructure.Job.Quartz` | Quartz hosted service (`WaitForJobsToComplete = true`) |

### Подробности по слоям

#### Application.Core

```csharp
protected override IServiceCollection Process(IServiceCollection serviceCollection)
{
    return serviceCollection
        .AddHttpContextAccessor()
        .ConfigureJsonSerializer()
        .AddRepositories()          // авто-регистрация IRepository<>
        .AddDbSeeder()              // авто-регистрация IDbSeeder
        .AddPropertyUtil()
        .AddSingleton<IUriValidator, RelativeUriValidator>()
        .AddSingleton<IResponseValidator, ProxiedResponseValidator>()
        .AddScoped<IScopedMemoryCache, ScopedMemoryCache>();
}
```

#### Application.Cqrs.Core

```csharp
protected override IServiceCollection Process(IServiceCollection serviceCollection)
{
    return serviceCollection
        .AddMediatR();
}
```

#### Dal.EFCore (базовый)

```csharp
protected override IServiceCollection Process(IServiceCollection serviceCollection)
{
    return serviceCollection
        .AddSingleton<IQueryEvaluator, EfQueryEvaluator>()
        .AddDbContexts();  // авто-обнаружение DbContext и регистрация
}
```

#### Dal.EFCore.Postgres

```csharp
protected override IServiceCollection Process(IServiceCollection serviceCollection)
{
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    serviceCollection
        .AddTransient<IDbContextOptionsBuilderInitializer, DbContextOptionsBuilderInitializer>();
    base.Process(serviceCollection);  // EfQueryEvaluator + DbContexts
    return serviceCollection;
}
```

#### Presentation.Core

```csharp
protected override IServiceCollection Process(IServiceCollection serviceCollection)
{
    return serviceCollection
        .AddEndpointsApiExplorer()
        .AddSwagger()
        .AddFluentValidation()
        .AddExceptionHandling();
}
```

#### Logging

```csharp
protected override IServiceCollection Process(IServiceCollection serviceCollection)
{
    return serviceCollection
        .AddNlog(configuration);
}
```

#### Mapper (AutoMapper)

```csharp
protected override IServiceCollection Process(IServiceCollection serviceCollection)
{
    var profileType = typeof(Profile);
    var mapperProfilesTypes = AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(a => a.GetTypes())
        .Where(type => profileType.IsAssignableFrom(type) && !type.IsAbstract).ToArray();
    return serviceCollection
        .AddAutoMapper(mapperProfilesTypes)
        .AddSingleton<IMapper, Mapper>();
}
```

#### Job.Quartz

```csharp
protected override IServiceCollection Process(IServiceCollection serviceCollection)
{
    return serviceCollection
        .AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
}
```

---

## WebApplicationBuilder vs WebApplication

Методы расширения строго разделены между этапами конфигурации:

| Этап | Тип | Метод | Что делает |
|------|-----|-------|-----------|
| Регистрация сервисов | `WebApplicationBuilder` | `ImplementDependencies()` | Конфигурация, контроллеры, DI |
| Регистрация сервисов | `WebApplicationBuilder` | `builder.Services.AddScoped<...>()` | Кастомные сервисы |
| Middleware pipeline | `WebApplication` | `UsePresentationCore()` | Correlation ID, Swagger, Exception Handler, Auth |
| Middleware pipeline | `WebApplication` | `UseCommonPresentation()` | Presentation + CORS |
| Endpoint routing | `WebApplication` | `MapControllers()` | Маршрутизация контроллеров |

**Правило:** всё, что регистрирует сервисы — на `WebApplicationBuilder`. Всё, что конфигурирует pipeline — на `WebApplication`.

---

## Полный пример Program.cs

### Типичный микросервис (Getter / Setter)

```csharp
using Shared.Presentation.Core.Extensions;
using Template.Presentation.Extensions;

var builder = WebApplication.CreateBuilder(args);

// === WebApplicationBuilder: регистрация сервисов ===
builder.ImplementDependencies();

// Кастомные регистрации (после ImplementDependencies)
builder.Services.AddScoped<IUserProvider, HttpUserProvider>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

var app = builder.Build();

// === WebApplication: middleware pipeline ===
app.UseCommonPresentation();

// Кастомные middleware (если нужны)
// app.UseCustomMiddleware();

app.Run();
```

### BFF-сервис

```csharp
using Shared.Presentation.Core.Extensions;
using Template.Presentation.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.ImplementDependencies();

var app = builder.Build();
app.UseCommonPresentation();

app.Run();
```

### DatabaseUpgrade (консольное приложение)

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Application.Core.Configuration.Extensions;
using Shared.Infrastructure.Core.DependencyInjection.Extensions;
using Shared.Infrastructure.Dal.EFCore.Attributes;

[assembly: MigrationAssembly]

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostContext, config) =>
    {
        config.InitializeConfiguration(hostContext.HostingEnvironment);
    })
    .ConfigureServices((_, services) =>
    {
        services.AddReferencedDependencyInjectors();  // Только DI, без контроллеров
    });

using var host = builder.Build();
var dbUpdater = host.Services.GetRequiredService<IDbUpdater>();
dbUpdater.CreateDbIfNotExists();
dbUpdater.Migrate();
dbUpdater.Initialize();
```

---

## Точки расширения

### Собственный DependencyInjector

Для регистрации сервисов конкретного микросервиса создайте собственный инжектор:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.DependencyInjection.Base;

namespace Template.Presentation.DependencyInjection;

public class DependencyInjector(ILoggerFactory loggerFactory)
    : DependencyInjectorBase(loggerFactory)
{
    protected override IServiceCollection Process(IServiceCollection services)
    {
        return services
            .AddScoped<IUserProvider, HttpUserProvider>()
            .AddScoped<ICurrentUserService, CurrentUserService>()
            .AddScoped<IOrderService, OrderService>();
    }
}
```

Он будет обнаружен `AddReferencedDependencyInjectors` автоматически — при условии, что сборка ссылается на `Shared.Application.Core`.

### Ручная регистрация

Если класс требует runtime-параметров, используйте ручную регистрацию после `ImplementDependencies()`:

```csharp
builder.ImplementDependencies();

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new ExternalApiClient(config["ExternalApi:BaseUrl"]);
});
```

### Кастомный middleware

Добавляйте кастомный middleware между вызовами `UsePresentationCore()` или внутри собственного расширения:

```csharp
public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCommonPresentation(this WebApplication app)
    {
        return app
            .UsePresentationCore()        // Shared pipeline
            .UseCors(Constants.CorsDefaultPolicyName)
            .UseCustomRateLimiting();     // Кастомное расширение
    }
}
```

### Атрибут ManualConfiguration

Классы, отмеченные `[ManualConfiguration]`, исключаются из авто-регистрации. Используйте, когда требуется кастомная конфигурация DI:

```csharp
[ManualConfiguration]
public class CustomHttpClientHandler : HttpClientHandler
{
    // Не будет зарегистрирован автоматически
}
```

---

## См. также

| Документ | Описание |
|----------|----------|
| [Auto-Registration](auto-registration.md) | Механизм `RegisterDerivedTypeDependencies` и `DependencyInjectorBase` |
| [Configuration](configuration.md) | Загрузка `.env` и конфигурация по модулям |
| [Controllers](controllers.md) | Конвенции контроллеров, маршрутизация |
| [Correlation ID](correlation-id.md) | Трассировка запросов через `X-Correlation-Id` |
| [Exception Mapping](exception-mapping.md) | Глобальная обработка исключений |
| [Swagger](swagger.md) | Настройка и конфигурация Swagger/OpenAPI |