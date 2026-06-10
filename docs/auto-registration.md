# Авто-регистрация зависимостей (Auto-Registration)

**Сборка:** `Shared.Application.Core.dll`  
**Namespace:** `Shared.Application.Core.DependencyInjection.Extensions`  
**Исходники:** `src/Shared/Core/Shared.Application.Core/DependencyInjection/`

---

## Быстрый старт

```csharp
// Program.cs — регистрация всех IValidator<T> из всех сборок
builder.Services.RegisterDerivedTypeDependencies<IValidator>(
    serviceTypeAsInterface: true,
    lifetime: ServiceLifetime.Transient);
```

Все классы, реализующие `IValidator<T>`, будут найдены и зарегистрированы автоматически.

---

## Обзор API

### RegisterDerivedTypeDependency (single)

Регистрирует **один** производный тип. Если найдено более одного — выбрасывает `InvalidOperationException`.

```csharp
// Generic overload
services.RegisterDerivedTypeDependency<TBaseType>(
    serviceTypeAsInterface: true,
    lifetime: ServiceLifetime.Scoped);

// Non-generic overload
services.RegisterDerivedTypeDependency(
    baseType: typeof(IExceptionHandler),
    serviceTypeAsInterface: true,
    lifetime: ServiceLifetime.Singleton);
```

**Use case:** когда для базового типа/интерфейса ожидается ровно одна реализация (например, `IExceptionHandler`).

### RegisterDerivedTypeDependencies (multiple)

Регистрирует **все** найденные производные типы.

```csharp
// Generic overload
services.RegisterDerivedTypeDependencies<IValidator>(
    serviceTypeAsInterface: true,
    lifetime: ServiceLifetime.Transient);

// Non-generic overload
services.RegisterDerivedTypeDependencies(
    baseType: typeof(IExceptionMapper),
    serviceTypeAsInterface: true,
    lifetime: ServiceLifetime.Singleton);
```

**Use case:** когда для базового типа ожидается множество реализаций (валидаторы, мапперы, handlers).

---

## Параметры

| Параметр | Тип | Описание |
|----------|-----|----------|
| `baseType` / `TBaseType` | `Type` / generic | Базовый тип, производные от которого ищутся |
| `serviceTypeAsInterface` | `bool` | Если `true` — регистрация как `interface → implementation`; если `false` — `implementation → implementation` (self-registration) |
| `lifetime` | `ServiceLifetime` | Жизненный цикл: `Transient`, `Scoped`, `Singleton` |
| `includedAttributesTypes` | `Type[]?` | Фильтр: регистрировать только типы, содержащие указанные атрибуты |
| `excludedAttributesTypes` | `Type[]?` | Фильтр: исключить типы, содержащие указанные атрибуты |

---

## serviceTypeAsInterface: подробное объяснение

### `serviceTypeAsInterface = true`

Регистрация **интерфейс → реализация**. Сервис резолвится по интерфейсу.

```csharp
// Регистрация
services.RegisterDerivedTypeDependencies<IValidator>(
    serviceTypeAsInterface: true,
    lifetime: ServiceLifetime.Transient);

// Эквивалентно ручной регистрации:
services.AddTransient<IValidator<PersonCreateRequest>, PersonCreateValidator>();
services.AddTransient<IValidator<PersonUpdateRequest>, PersonUpdateValidator>();

// Резолв
var validators = serviceProvider.GetServices<IValidator>();
```

Алгоритм поиска service type:

```csharp
var serviceType = type.GetInterfaces()
    .First(t => baseType.IsGenericTypeDefinition
        ? t.GetGenericTypeDefinition() == baseType
        : t == baseType);
```

### `serviceTypeAsInterface = false`

Регистрация **реализация → реализация** (self-registration). Сервис резолвится по конкретному типу.

```csharp
// Регистрация
services.RegisterDerivedTypeDependencies<DelegatingHandler>(
    serviceTypeAsInterface: false,
    lifetime: ServiceLifetime.Transient);

// Эквивалентно ручной регистрации:
services.AddTransient<AuthDelegatingHandler>();
services.AddTransient<LoggingDelegatingHandler>();

// Резолв
var handlers = serviceProvider.GetServices<AuthDelegatingHandler>();
```

---

## Внутренняя реализация

### RegisterDerivedTypeDependenciesInternal

```csharp
private static IServiceCollection RegisterDerivedTypeDependenciesInternal(
    this IServiceCollection serviceCollection,
    Type baseType,
    bool serviceTypeAsInterface,
    ServiceLifetime lifetime,
    bool requireSingleImplementation,
    Type[]? includedAttributesTypes,
    Type[]? excludedAttributesTypes)
{
    // 1. ManualConfigurationAttribute ВСЕГДА исключается
    excludedAttributesTypes =
        excludedAttributesTypes?.Append(typeof(ManualConfigurationAttribute)).ToArray() ??
        [typeof(ManualConfigurationAttribute)];

    // 2. Сканирование всех сборок домена
    var candidateTypes = AssemblyHelper
        .GetDerivedTypesFromAssemblies(
            baseType,
            includedAttributesTypes: includedAttributesTypes,
            excludedAttributesTypes: excludedAttributesTypes)
        // 3. Исключение открытых generic-типов
        .Where(type => !type.IsGenericTypeDefinition)
        .ToArray();

    // 4. Валидация единственности (для RegisterDerivedTypeDependency)
    if (requireSingleImplementation &&
        !baseType.IsGenericTypeDefinition &&
        candidateTypes.Length > 1)
    {
        throw new InvalidOperationException(
            $"Only one derived type is allowed for service type '{baseType.Name}'.");
    }

    // 5. Регистрация
    foreach (var type in candidateTypes)
    {
        var serviceType = serviceTypeAsInterface
            ? type.GetInterfaces().First(t =>
                baseType.IsGenericTypeDefinition
                    ? t.GetGenericTypeDefinition() == baseType
                    : t == baseType)
            : type;

        serviceCollection.Add(new ServiceDescriptor(serviceType, type, lifetime));
    }

    return serviceCollection;
}
```

### Шаг 1: Исключение ManualConfigurationAttribute

`ManualConfigurationAttribute` **всегда** добавляется в `excludedAttributesTypes`. Это гарантирует, что типы, требующие ручной конфигурации, не будут зарегистрированы автоматически.

```csharp
excludedAttributesTypes =
    excludedAttributesTypes?.Append(typeof(ManualConfigurationAttribute)).ToArray() ??
    [typeof(ManualConfigurationAttribute)];
```

### Шаг 2: Сканирование сборок

`AssemblyHelper.GetDerivedTypesFromAssemblies()` проходит по всем сборкам текущего AppDomain и находит типы, которые:

- Являются неабстрактными классами.
- Наследуются от `baseType` (классы) или реализуют `baseType` (интерфейсы).
- Не содержат атрибутов из `excludedAttributesTypes`.
- Содержат атрибуты из `includedAttributesTypes` (если указан).

### Шаг 3: Фильтрация IsGenericTypeDefinition

Открытые generic-типы (`class Foo<T> : IValidator<T>`) **исключаются** из регистрации:

```csharp
.Where(type => !type.IsGenericTypeDefinition)
```

**Почему:** невозможно зарегистрировать `Foo<>` как `IValidator<>` без конкретного типа-аргумента. Закрытые generic-типы (`class PersonValidator : IValidator<Person>`) регистрируются нормально.

| Тип | `IsGenericTypeDefinition` | Зарегистрируется? |
|-----|--------------------------|-------------------|
| `class PersonValidator : IValidator<Person>` | `false` | ✅ Да |
| `class Foo<T> : IValidator<T>` | `true` | ❌ Нет |
| `class IntValidator : IValidator<int>` | `false` | ✅ Да |

---

## ManualConfigurationAttribute

**Файл:** `Shared.Application.Core/DependencyInjection/Attributes/ManualConfigurationAttribute.cs`

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class ManualConfigurationAttribute : Attribute;
```

Маркирует классы, которые **должны быть сконфигурированы вручную** и исключаются из авто-регистрации.

### Когда использовать

| Ситуация | Пример |
|----------|--------|
| Требуется custom factory | `services.AddSingleton<IHttpClientCache>(sp => new MemoryCache(...))` |
| Зависит от runtime-конфигурации | `services.AddScoped<IEmailSender>(sp => new SmtpSender(config))` |
| Требует дополнительных настроек | `services.AddHttpClient<IApiClient, ApiClient>().AddPolicyHandler(...)` |
| Conditional registration | Регистрация зависит от feature flags |

### Пример

```csharp
// Этот класс НЕ будет зарегистрирован автоматически
[ManualConfiguration]
public class CustomHttpClientHandler : HttpClientHandler
{
    public CustomHttpClientHandler(ICertificateProvider provider)
    {
        ClientCertificates.Add(provider.GetClientCertificate());
        SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
    }
}

// Ручная регистрация с кастомной логикой
builder.Services.AddSingleton<CustomHttpClientHandler>(sp =>
{
    var provider = sp.GetRequiredService<ICertificateProvider>();
    return new CustomHttpClientHandler(provider);
});
```

---

## DependencyInjectorBase

**Файл:** `Shared.Application.Core/DependencyInjection/Base/DependencyInjectorBase.cs`

Абстрактный базовый класс для реализации внедрения зависимостей слоя. Использует **Template Method** + **структурированные лог-сообщения** из `DependencyInjectionLogMessages`.

```csharp
public abstract class DependencyInjectorBase
{
    protected readonly ILogger Logger;

    protected DependencyInjectorBase(ILoggerFactory loggerFactory)
    {
        Logger = loggerFactory.CreateLogger(GetType());
    }

    /// Public entry point — с логированием и обработкой ошибок
    public IServiceCollection Inject(IServiceCollection serviceCollection)
    {
        try
        {
            var result = Process(serviceCollection);
            Logger.LogInformation(DependencyInjectionLogMessages.DependenciesInjected);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, DependencyInjectionLogMessages.DependenciesNotInjected);
            throw;
        }
    }

    /// Abstract — наследники реализуют здесь регистрацию
    protected abstract IServiceCollection Process(IServiceCollection serviceCollection);
}
```

**`DependencyInjectionLogMessages`** (`Shared.Application.Core/DependencyInjection/DependencyInjectionLogMessages.cs`):

```csharp
public static class DependencyInjectionLogMessages
{
    public const string DependenciesInjected = "Dependencies injected.";
    public const string DependenciesNotInjected = "Dependencies not injected.";
}
```

> Лог-сообщения централизованы и могут быть подхвачены source-generator'ом для structured logging.

### Template Method Pattern

```
Inject()  ← public, с logging + error handling
    │
    └── Process()  ← abstract, наследники реализуют регистрацию
```

### Пример наследника (реальный код)

```csharp
// /src/Shared/Core/Shared.Application.Core/DependencyInjection/DependencyInjector.cs
public class DependencyInjector(ILoggerFactory loggerFactory)
    : DependencyInjectorBase(loggerFactory)
{
    protected override IServiceCollection Process(IServiceCollection serviceCollection)
    {
        return serviceCollection
            // TODO: вынести в Presentation-layer после миграции туда ApiClient
            .AddHttpContextAccessor()
            .ConfigureJsonSerializer()
            .AddRepositories()
            .AddDatabaseUpdater()
            .AddDbSeeder()
            .AddPropertyUtil()
            .AddSingleton<IUriValidator, RelativeUriValidator>()
            .AddSingleton<IResponseValidator, ProxiedResponseValidator>()
            .AddScoped<IScopedMemoryCache, ScopedMemoryCache>()
            .AddLifecycleActions();
    }
}
```

> Здесь используются **специализированные extension-методы** (`AddRepositories`, `AddDatabaseUpdater`, `AddDbSeeder`, `AddPropertyUtil`, `ConfigureJsonSerializer`, `AddLifecycleActions`), а не `RegisterDerivedTypeDependencies<IRepository>` напрямую. Это связано с тем, что репозитории требуют generic-обёртки (`IRepository<T>`) и оптимизированной регистрации по сборкам.

### Использование

```csharp
var injector = new Shared.Application.Core.DependencyInjection.DependencyInjector(loggerFactory);
injector.Inject(builder.Services);
```

---

## Примеры использования

### IValidator (FluentValidation)

```csharp
// Все валидаторы регистрируются автоматически
builder.Services.RegisterDerivedTypeDependencies<IValidator>(
    serviceTypeAsInterface: true,
    lifetime: ServiceLifetime.Transient);

// Реализации (находятся автоматически):
public sealed class PersonCreateValidator : AbstractValidator<PersonCreateRequest> { ... }
public sealed class PersonUpdateValidator : AbstractValidator<PersonUpdateRequest> { ... }
```

### DelegatingHandler

```csharp
// Self-registration — каждый handler резолвится по конкретному типу
builder.Services.RegisterDerivedTypeDependencies<DelegatingHandler>(
    serviceTypeAsInterface: false,
    lifetime: ServiceLifetime.Transient);

// Реализации:
public sealed class AuthDelegatingHandler : DelegatingHandler { ... }
public sealed class LoggingDelegatingHandler : DelegatingHandler { ... }
public sealed class RetryDelegatingHandler : DelegatingHandler { ... }
```

### HttpClientHandler

```csharp
// Все HttpClientHandler регистрируются как concrete types
builder.Services.RegisterDerivedTypeDependencies<HttpClientHandler>(
    serviceTypeAsInterface: false,
    lifetime: ServiceLifetime.Transient);

// Но кастомный handler — вручную:
[ManualConfiguration]
public class MutualTlsHttpClientHandler : HttpClientHandler
{
    // Требует runtime-конфигурации сертификатов
}
```

### IExceptionMapper

```csharp
// Все мапперы исключений регистрируются автоматически
builder.Services.RegisterDerivedTypeDependencies<IExceptionMapper>(
    serviceTypeAsInterface: true,
    lifetime: ServiceLifetime.Singleton);

// Реализации (находятся автоматически):
public sealed class NotFoundExceptionMapper : IExceptionMapper<NotFoundException> { ... }
public sealed class BusinessLogicExceptionMapper : IExceptionMapper<BusinessLogicException> { ... }
public sealed class ValidationExceptionMapper : IExceptionMapper<ValidationException> { ... }
```

---

## Best Practices

### ✅ Делайте

| Практика | Пример |
|----------|--------|
| Используйте `serviceTypeAsInterface: true` для интерфейсов | `RegisterDerivedTypeDependencies<IValidator>(true, ...)` |
| Используйте `serviceTypeAsInterface: false` для абстрактных классов | `RegisterDerivedTypeDependencies<DelegatingHandler>(false, ...)` |
| Помечайте `[ManualConfiguration]` типы с runtime-зависимостями | `[ManualConfiguration] public class CustomHandler` |
| Используйте `includedAttributesTypes` для selective registration | `RegisterDerivedTypeDependencies(..., includedAttributesTypes: [typeof(FeatureFlagAttribute)])` |
| Используйте `DependencyInjectorBase` для регистрации целого слоя | `: DependencyInjectorBase(loggerFactory) { Process(...) }` |

### ❌ Избегайте

| Анти-паттерн | Проблема |
|--------------|----------|
| Несколько реализаций для `RegisterDerivedTypeDependency` | Выбросит `InvalidOperationException` |
| Открытые generic-типы как реализации | Исключаются через `IsGenericTypeDefinition` |
| Забытый `[ManualConfiguration]` на типе с runtime-зависимостями | DI выбросит ошибку при резолве |
| Регистрация одного и того же типа дважды | Second registration silently overwrites first |
| Строковые литералы в лог-сообщениях injector'а | Используйте `DependencyInjectionLogMessages` для структурированного логирования |

### AddRepositories

`AddRepositories()` — специальный метод для автоматического обнаружения и регистрации всех реализаций `IRepository<>` через reflection. Вызывается внутри `Shared.Application.Core.DependencyInjection.DependencyInjector`:

```csharp
services.AddRepositories();
```

Принцип работы аналогичен `RegisterDerivedTypeDependencies<IRepository>()`, но оптимизирован для репозиториев — сканирует сборки, находит все `EfRepository<T>`, регистрирует их как `IRepository<T>`.

---

## См. также

| Документ | Описание |
|----------|----------|
| [CQRS](cqrs.md) | Разделение команд и запросов |
| [Pipeline Behaviors](pipeline-behaviors.md) | Pipeline Behaviours — Logging, Validation |
| [Exception Mapping](exception-mapping.md) | Маппинг исключений (использует auto-registration для IExceptionMapper) |
| [AssemblyHelper](assembly-helper.md) | `GetAssembliesByPrefix`, `GetDerivedTypesFromAssemblies` |
