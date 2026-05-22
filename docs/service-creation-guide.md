# Создание микросервиса на базе Shared Framework

## Обзор

Это пошаговое руководство по созданию нового микросервиса на базе шаблонного проекта `Template`. Руководство описывает структуру проектов по слоям Clean Architecture, настройки DI-регистрации, правила наследования от базовых классов фреймворка и порядок подключения к solution.

### Зачем следовать этому шаблону

| Преимущество | Что даёт |
|---|---|
| Единообразие | Все микросервисы имеют идентичную структуру слоёв и соглашения |
| Автоматическая DI-регистрация | `ImplementDependencies()` автоматически находит все `DependencyInjectorBase` |
| Готовая инфраструктура | DbContext, repositories, controllers, exception handling «из коробки» |
| CQRS + MediatR | Стандартизированный паттерн для команд и запросов |
| Audit-поля | `IWithCreated`, `IWithUpdated`, `IWithDeleted` — автозаполнение через репозиторий |

### Структура Template-сервиса

```
src/Services/Common/
├── Template.Domain/              # Ядро: сущности, интерфейсы
├── Template.Application/         # Use cases: CQRS-запросы/команды, DTO, валидация
├── Template.Infrastructure/      # Внешние интеграции: API-клиенты
├── Template.Infrastructure.Dal/  # БД: DbContext, конфигурации, репозитории
├── Template.Infrastructure.Mapping/ # AutoMapper-профили
└── Template.Presentation/        # Контроллеры, CORS, Swagger-настройки
```

---

## Step 1: Структура проектов

### Создание проектов

Для нового микросервиса (например, `ProductService`) заменяйте `Template` на имя сервиса. Создайте проекты командами:

```bash
# Domain — библиотека классов
dotnet new classlib -n ProductService.Domain -o src/Services/Product/ProductService.Domain

# Application — библиотека классов
dotnet new classlib -n ProductService.Application -o src/Services/Product/ProductService.Application

# Infrastructure — библиотека классов
dotnet new classlib -n ProductService.Infrastructure -o src/Services/Product/ProductService.Infrastructure

# Infrastructure.Dal — библиотека классов
dotnet new classlib -n ProductService.Infrastructure.Dal -o src/Services/Product/ProductService.Infrastructure.Dal

# Infrastructure.Mapping — библиотека классов
dotnet new classlib -n ProductService.Infrastructure.Mapping -o src/Services/Product/ProductService.Infrastructure.Mapping

# Presentation — библиотека классов
dotnet new classlib -n ProductService.Presentation -o src/Services/Product/ProductService.Presentation

# Api — веб-приложение (точка входа)
dotnet new web -n ProductService.Api -o src/Services/Product/ProductService.Api
```

### Ссылки между проектами (csproj)

Зависимости направлены **внутрь** — от Api к Domain:

```
Api ──► Presentation ──► Infrastructure.Mapping ──► Infrastructure ──► Application ──► Domain
                    │                              │
                    └──► Infrastructure.Dal ──────┘
```

#### Domain.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <CodeAnalysisRuleSet>..\..\..\ruleset.ruleset</CodeAnalysisRuleSet>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\Shared\Core\Shared.Domain.Core\Shared.Domain.Core.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="StyleCop.Analyzers" Version="1.1.118">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <AdditionalFiles Include="..\..\..\stylecop.json" />
  </ItemGroup>
</Project>
```

#### Application.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <CodeAnalysisRuleSet>..\..\..\ruleset.ruleset</CodeAnalysisRuleSet>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\Shared\Core\Shared.Application.Cqrs.Core\Shared.Application.Cqrs.Core.csproj" />
    <ProjectReference Include="..\ProductService.Domain\ProductService.Domain.csproj" />
  </ItemGroup>
  <!-- StyleCop как в Domain -->
</Project>
```

#### Infrastructure.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <CodeAnalysisRuleSet>..\..\..\ruleset.ruleset</CodeAnalysisRuleSet>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\Shared\Core\Shared.Infrastructure.Core\Shared.Infrastructure.Core.csproj" />
    <ProjectReference Include="..\..\..\Shared\Logging\Shared.Infrastructure.Logging\Shared.Infrastructure.Logging.csproj" />
    <ProjectReference Include="..\ProductService.Application\ProductService.Application.csproj" />
  </ItemGroup>
  <!-- StyleDoc как в Domain -->
</Project>
```

#### Infrastructure.Dal.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <CodeAnalysisRuleSet>..\..\..\ruleset.ruleset</CodeAnalysisRuleSet>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\Shared\Dal\Shared.Infrastructure.Dal.EFCore.Postgres\Shared.Infrastructure.Dal.EFCore.Postgres.csproj" />
    <ProjectReference Include="..\ProductService.Infrastructure\ProductService.Infrastructure.csproj" />
  </ItemGroup>
  <!-- StyleDoc как в Domain -->
</Project>
```

#### Infrastructure.Mapping.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\Shared\Mapper\Shared.Infrastructure.Mapper.AutoMapper\Shared.Infrastructure.Mapper.AutoMapper.csproj" />
    <ProjectReference Include="..\ProductService.Infrastructure\ProductService.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

#### Presentation.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <CodeAnalysisRuleSet>..\..\..\ruleset.ruleset</CodeAnalysisRuleSet>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\Shared\Core\Shared.Presentation.Core\Shared.Presentation.Core.csproj" />
    <ProjectReference Include="..\ProductService.Infrastructure.Mapping\ProductService.Infrastructure.Mapping.csproj" />
    <ProjectReference Include="..\ProductService.Infrastructure\ProductService.Infrastructure.csproj" />
  </ItemGroup>
  <!-- StyleDoc как в Domain -->
</Project>
```

#### Api.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn>
    <UserSecretsId>уникальный-guid</UserSecretsId>
    <DockerDefaultTargetOS>Linux</DockerDefaultTargetOS>
    <DockerfileContext>..\..\..\..</DockerfileContext>
    <CodeAnalysisRuleSet>..\..\..\ruleset.ruleset</CodeAnalysisRuleSet>
  </PropertyGroup>
  <ItemGroup>
    <Content Include="..\..\..\.env" Link=".env">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </Content>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\Common\ProductService.Presentation\ProductService.Presentation.csproj" />
    <!-- Или напрямую, если Common-слоя нет -->
  </ItemGroup>
  <!-- StyleDoc как в Domain -->
</Project>
```

---

## Step 2: Domain слой

**Расположение:** `ProductService.Domain/`

### Структура папок

```
ProductService.Domain/
├── Entities/         # Сущности
├── ValueObjects/     # Объекты-значения (если нужны)
├── Enums/            # Перечисления домена
└── Interfaces/       # Интерфейсы (если нужны специфичные для домена)
```

### Сущности

Все сущности наследуют `IEntity<Guid>` — базовый интерфейс из `Shared.Domain.Core.Interfaces`:

```csharp
using Shared.Domain.Core.Interfaces;

namespace ProductService.Domain.Entities;

/// <summary>
/// Сущность "Product".
/// </summary>
public class Product : IEntity<Guid>
{
    /// <inheritdoc cref="IEntity.Id"/>
    public Guid Id { get; private set; }

    /// <summary>
    /// Наименование.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Цена.
    /// </summary>
    public decimal Price { get; private set; }

    /// <summary>
    /// Создание сущности "Product".
    /// </summary>
    /// <param name="name">Наименование.</param>
    /// <param name="price">Цена.</param>
    /// <returns>Экземпляр сущности "Product".</returns>
    public static Product Create(string name, decimal price)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Price = price,
        };
    }
}
```

### Audit-интерфейсы

Фреймворк предоставляет набор интерфейсов для аудита. Подключайте их к сущности через интерфейсы:

| Интерфейс | Добавляет | Автозаполнение |
|---|---|---|
| `IWithCreated` | `CreatedByUserId`, `CreatedByUserName`, `OnCreate()` | Через `EfRepository.AddAsync()` |
| `IWithDateCreated` | `DateCreated` | Автоматически при сохранении |
| `IWithUpdated` | `UpdatedByUserId` | Через `IWithUpdated.SetUpdatedByUserId()` |
| `IWithDateUpdated` | `DateUpdated` | Автоматически при сохранении |
| `IWithDeleted` | `IsDeleted`, `DeletedByUserId`, `SetIsDeleted()`, `OnDelete()` | Через `EfRepository.RemoveAsync()` — soft delete |
| `IWithDateDeleted` | `DateDeleted` | Автоматически при soft delete |

Пример сущности с полным аудитом:

```csharp
public class Product : IEntity<Guid>, IWithCreated, IWithUpdated, IWithDeleted
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public decimal Price { get; private set; }

    // IWithCreated
    public Guid? CreatedByUserId { get; private set; }
    public string? CreatedByUserName { get; private set; }
    public void SetCreatedByUserId(Guid? id) => CreatedByUserId = id;
    public void SetCreatedByUserName(string name) => CreatedByUserName = name;
    public void OnCreate(Guid? userId, string? userName)
    {
        CreatedByUserId = userId;
        CreatedByUserName = userName;
    }

    // IWithUpdated
    public Guid? UpdatedByUserId { get; private set; }
    public void SetUpdatedByUserId(Guid? id) => UpdatedByUserId = id;

    // IWithDeleted
    public bool IsDeleted { get; private set; }
    public Guid? DeletedByUserId { get; private set; }
    public void SetIsDeleted() => IsDeleted = true;
    public void OnDelete(Guid? userId) => DeletedByUserId = userId;

    public static Product Create(string name, decimal price) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Price = price,
    };
}
```

### Действия перехвата — `IWithLifecycleActions`

Если сущности должна поддерживать lifecycle actions, реализуйте `IWithLifecycleActions`:

```csharp
public class Product : IEntity<Guid>, IWithLifecycleActions
{
    // ... свойства ...

    public string[] RequiredToSaveNavigationPropertiesNames => [];
    public bool TryGetAction(LifecycleHookType hookType, Enum key, out IEntityLifecycleAction? lifecycleAction) { /* ... */ }
    public void ResetActions() { /* ... */ }
    public ICollection<Enum> GetAllKeys(LifecycleHookType hookType) { /* ... */ }
}
```

> **Важно:** `EntityConfigurationBase` автоматически вызывает `builder.Ignore(nameof(IWithLifecycleActions.RequiredToSaveNavigationPropertiesNames))` для сущностей, реализующих `IWithLifecycleActions`.

---

## Step 3: Application слой

**Расположение:** `ProductService.Application/`

### Структура папок

```
ProductService.Application/
├── DependencyInjection/
│   └── DependencyInjector.cs
├── Abstractions/
│   └── Dto/
│       └── Product/
│           ├── Requests/
│           │   └── ProductListRequest.cs
│           └── Responses/
│               └── ProductPayload.cs
├── Features/
│   └── ProductFeature/
│       └── Cqrs/
│           ├── Commands/
│           │   ├── CreateProductCommand.cs
│           │   ├── CreateProductCommandHandler.cs
│           │   ├── UpdateProductCommand.cs
│           │   ├── UpdateProductCommandHandler.cs
│           │   ├── DeleteProductCommand.cs
│           │   └── DeleteProductCommandHandler.cs
│           └── Queries/
│               ├── ProductReadListQuery.cs
│               └── ProductReadListQueryHandler.cs
└── Interfaces/          # Сервисные интерфейсы (если не CQRS)
    └── IProductService.cs
```

### DependencyInjector

Каждый слой содержит `DependencyInjector`, унаследованный от `DependencyInjectorBase`. Метод `Process()` регистрирует сервисы:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.DependencyInjection.Base;
using ProductService.Application.Interfaces;
using ProductService.Application.Services;

namespace ProductService.Application.DependencyInjection;

/// <summary>
/// Регистрация DI-зависимостей слоя: <c>ProductService.Application</c>.
/// </summary>
/// <inheritdoc cref="DependencyInjectorBase" path="/remarks"/>
/// <param name="loggerFactory"><inheritdoc cref="DependencyInjectorBase(ILoggerFactory)" path="/param[@name='loggerFactory']"/></param>
public class DependencyInjector(
    ILoggerFactory loggerFactory)
    : DependencyInjectorBase(loggerFactory)
{
    /// <inheritdoc />
    protected override IServiceCollection Process(
        IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddScoped<IProductService, ProductService>();
    }
}
```

### CQRS — базовые типы

Фреймворк предоставляет абстракции для Command и Query:

| Класс | Назначение |
|---|---|
| `CreateCommand<TRequest, TResponse>` | Создание сущности |
| `UpdateCommand<TRequest, TResponse>` | Обновление по ключу (`Key`, `Request`) |
| `DeleteCommand` | Удаление по ключу (`Key`), ответ — `Response` |
| `ReadListQuery<TRequest, TFilter, TResponse>` | Постраничный список |

### Пример ReadListQuery

```csharp
using Shared.Application.Cqrs.Core.Abstractions.Queries.Requests;

namespace ProductService.Application.Features.ProductFeature.Cqrs.Queries;

/// <summary>
/// Запрос на получение списка продуктов.
/// </summary>
public sealed record ProductReadListQuery(
    ProductListRequest Request)
    : ReadListQuery<ProductListRequest, ProductFilter, ProductListResponse, ProductPayload>(Request);
```

### Пример ReadListQueryHandler

Наследуется от `ReadListQueryHandler<...>` — получает `IUnitOfWork` и пагинацию «из коробки»:

```csharp
using Microsoft.Extensions.Logging;
using Shared.Application.Cqrs.Core.Abstractions.Queries.Handlers;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;

namespace ProductService.Application.Features.ProductFeature.Cqrs.Queries;

/// <summary>
/// Handler запроса списка продуктов.
/// </summary>
public sealed class ProductReadListQueryHandler(
    ILoggerFactory loggerFactory,
    IUnitOfWork unitOfWork)
    : ReadListQueryHandler<ProductReadListQuery, ProductListRequest, ProductFilter,
        ProductListResponse, ProductPayload, Product>(loggerFactory, unitOfWork)
{
}
```

### Пример CreateCommand + Handler

```csharp
using Shared.Application.Cqrs.Core.Abstractions.Commands.Requests;

namespace ProductService.Application.Features.ProductFeature.Cqrs.Commands;

/// <summary>
/// Команда создания продукта.
/// </summary>
public sealed record CreateProductCommand(
    CreateProductRequest Request)
    : CreateCommand<CreateProductRequest, ProductResponse>(Request);
```

```csharp
using Microsoft.Extensions.Logging;
using Shared.Application.Cqrs.Core.Abstractions;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using ProductService.Domain.Entities;

namespace ProductService.Application.Features.ProductFeature.Cqrs.Commands;

/// <summary>
/// Handler команды создания продукта.
/// </summary>
public sealed class CreateProductCommandHandler(
    IUnitOfWork unitOfWork,
    ILoggerFactory loggerFactory)
    : EntityRequestHandler<CreateProductCommand, ProductResponse, Product>(unitOfWork, loggerFactory)
{
    /// <inheritdoc />
    public override async Task<ProductResponse> Handle(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var product = Product.Create(command.Request.Name, command.Request.Price);
        await Repository.AddAsync(product, null, null, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new ProductResponse { Payload = new ProductPayload { Id = product.Id } };
    }
}
```

### EntityRequestHandler

Базовый класс `EntityRequestHandler<TRequest, TResponse, TEntity>` предоставляет:

| Член | Описание |
|---|---|
| `Repository` | Свойство, возвращающее `IRepository<TEntity>` через `IUnitOfWork` |
| `WithTracking` | Virtual — признак отслеживания изменений (default: `false`) |
| `AsSplitQuery` | Virtual — признак split queries (default: `false`) |
| `ConstructOptions()` | Virtual — построение `QueryOptions` с фильтрацией `IWithDeleted` |
| `ProcessEntityAsync()` | Virtual — доп. операции перед сохранением |
| `ProcessResponseAsync()` | Virtual — постобработка response |

---

## Step 4: Infrastructure.Dal слой

**Расположение:** `ProductService.Infrastructure.Dal/`

### Структура папок

```
ProductService.Infrastructure.Dal/
├── Configuration/
│   ├── EntityConfigurations.cs
│   ├── SeedConfiguration.cs
│   └── OutboxEventConfiguration.cs
├── Conventions/
│   └── ColumnsNamesConvention.cs
├── Repositories/
│   └── ProductRepository.cs
├── Settings/
│   └── DbSettings.cs
├── DbContext.cs
└── DependencyInjection/
    └── DependencyInjector.cs
```

### DbContext

Наследуйте `DbContextBase` из `Shared.Infrastructure.Dal.EFCore`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Shared.Infrastructure.Dal.EFCore;
using ProductService.Infrastructure.Dal.Conventions;

namespace ProductService.Infrastructure.Dal;

/// <summary>
/// Реализация <see cref="DbContext"/> для ProductService.
/// </summary>
public class DbContext(
    DbContextOptions<DbContext> options,
    IHostEnvironment environment)
    : DbContextBase(options, environment)
{
    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Conventions.Add(_ => new ColumnsNamesConvention());
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
```

> **Важно:** `DbContextBase.OnModelCreating` автоматически вызывает `modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetCallingAssembly())`, что загружает все `IEntityTypeConfiguration<>` из сборки `Infrastructure.Dal`.

### EntityConfiguration

Наследуйте `EntityConfigurationBase<TEntity>`:

```csharp
using Shared.Infrastructure.Dal.EFCore.Configurations;
using ProductService.Domain.Entities;

namespace ProductService.Infrastructure.Dal.Configuration;

/// <summary>
/// Конфигурация сущности "Product".
/// </summary>
public class EntityConfiguration : EntityConfigurationBase<Product>
{
    /// <inheritdoc />
    protected override void ConfigureProcess(EntityTypeBuilder<Product> builder)
    {
        base.ConfigureProcess(builder);

        builder.ToTable("products", t => t.HasComment("Таблица продуктов."));
        builder.Property(x => x.Name).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Price).HasColumnType("decimal(18,2)");
    }
}
```

Базовый класс `EntityConfigurationBase<>` автоматически настраивает:

- `UseTptMappingStrategy()` — TPT-стратегию наследования
- Первичный ключ `Id` с `ValueGeneratedNever()`
- Поля аудита (`CreatedByUserId`, `DateCreated`, ...) если сущность реализует соответствующие интерфейсы
- Свойство `RequiredToSaveNavigationPropertiesNames` игнорируется для `IWithLifecycleActions`

### Конвенция имён колонок — snake_case

Каждый сервис определяет `ColumnsNamesConvention`, приводящий имена колонок к `snake_case`:

```csharp
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Shared.Common.Extensions;

namespace ProductService.Infrastructure.Dal.Conventions;

/// <summary>
/// Конвенция для приведения названия полей к snake_case.
/// </summary>
public class ColumnsNamesConvention : IModelFinalizingConvention
{
    /// <inheritdoc />
    public void ProcessModelFinalizing(
        IConventionModelBuilder modelBuilder,
        IConventionContext<IConventionModelBuilder> context)
    {
        modelBuilder.Metadata
            .GetEntityTypes()
            .ToList()
            .ForEach(entity =>
                entity.GetProperties().ForEach(prop =>
                    prop.SetColumnName(prop.GetColumnName().ToSnakeCase())));
    }
}
```

### Seed и Outbox конфигурации

```csharp
// SeedConfiguration — обязательно для каждого сервиса
using Shared.Infrastructure.Dal.EFCore.Configurations;

namespace ProductService.Infrastructure.Dal.Configuration;

/// <summary>
/// Конфигурация для сущности "Seed".
/// </summary>
public class SeedConfiguration : SeedConfigurationBase;
```

```csharp
// OutboxEventConfiguration — для Outbox-паттерна
using Shared.Domain.Core.Entities;
using Shared.Infrastructure.Dal.EFCore.Configurations;

namespace ProductService.Infrastructure.Dal.Configuration;

/// <summary>
/// Конфигурация для сущности OutboxEvent.
/// </summary>
public class OutboxEventConfiguration : EntityConfigurationBase<OutboxEvent>
{
    /// <inheritdoc />
    protected override void ConfigureProcess(EntityTypeBuilder<OutboxEvent> builder)
    {
        base.ConfigureProcess(builder);
        builder.ToTable("outbox_events", t => t.HasComment("Таблица событий Outbox."));
        // ... детальная конфигурация ...
    }
}
```

### Репозиторий

Наследуйте `EfRepository<TEntity>` из `Shared.Infrastructure.Dal.EFCore.Repository`:

```csharp
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Infrastructure.Dal.EFCore.Repository;
using ProductService.Domain.Entities;

namespace ProductService.Infrastructure.Dal.Repositories;

/// <summary>
/// Репозиторий для Product.
/// </summary>
/// <param name="dbContext"><inheritdoc /></param>
/// <param name="evaluator"><inheritdoc /></param>
public class ProductRepository(
    DbContext dbContext,
    IQueryEvaluator evaluator)
    : EfRepository<Product>(dbContext, evaluator);
```

> **См. также:** Подробнее о репозиториях и спецификациях — в [repository.md](repository.md)

### DbSettings

```csharp
using Shared.Infrastructure.Dal.EFCore.Settings;

namespace ProductService.Infrastructure.Dal.Settings;

/// <summary>
/// Настройки подключения к БД.
/// </summary>
public class DbSettings : EfDbSettingsBase<DbContext>
{
}
```

### DependencyInjector (Dal)

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Configuration.Extensions;
using Shared.Application.Core.Dal.Settings.Models.Base;
using Shared.Application.Core.DependencyInjection.Base;
using ProductService.Infrastructure.Dal.Settings;

namespace ProductService.Infrastructure.Dal.DependencyInjection;

/// <summary>
/// Регистрация DI-зависимостей слоя: <c>ProductService.Infrastructure.Dal</c>.
/// </summary>
/// <inheritdoc cref="DependencyInjectorBase" path="/remarks"/>
/// <param name="configuration">Конфигурация приложения.</param>
/// <param name="loggerFactory"><inheritdoc cref="DependencyInjectorBase(ILoggerFactory)" path="/param[@name='loggerFactory']"/></param>
public class DependencyInjector(
    IConfiguration configuration,
    ILoggerFactory loggerFactory)
    : DependencyInjectorBase(loggerFactory)
{
    /// <inheritdoc />
    protected override IServiceCollection Process(
        IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSingleton<DbSettingsBase, DbSettings>(_ =>
            {
                var result = configuration.GetOptions<DbSettings>()!;
                return result;
            });
    }
}
```

---

## Step 5: Infrastructure.Mapping слой

**Расположение:** `ProductService.Infrastructure.Mapping/`

### AutoMapper-профиль

```csharp
using AutoMapper;

namespace ProductService.Infrastructure.Mapping;

/// <summary>
/// Профиль маппинга.
/// </summary>
public class MapperProfile : Profile
{
    /// <summary>
    /// Конструктор класса. Содержит конфигурации маппингов.
    /// </summary>
    public MapperProfile()
    {
        // CreateMap<Product, ProductPayload>();
        // CreateMap<CreateProductRequest, Product>();
    }
}
```

> AutoMapper-профили автоматически регистрируются через `Shared.Infrastructure.Mapper.AutoMapper`.

---

## Step 6: Presentation слой

**Расположение:** `ProductService.Presentation/`

### Структура папок

```
ProductService.Presentation/
├── Constants.cs
├── DependencyInjection/
│   └── DependencyInjector.cs
├── Extensions/
│   └── ApplicationBuilderExtensions.cs
└── Swagger/
    └── Extensions/
        └── DependencyInjectionExtensions.cs
```

### Constants.cs

```csharp
namespace ProductService.Presentation;

/// <summary>
/// Константы.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Наименование текущего приложения.
    /// </summary>
    public const string AppName = "ProductService";

    /// <summary>
    /// Имя политики, которая используется в Cors.
    /// </summary>
    public const string CorsDefaultPolicyName = nameof(CorsDefaultPolicyName);
}
```

### DependencyInjector (Presentation)

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.DependencyInjection.Base;
using ProductService.Presentation.Swagger.Extensions;

namespace ProductService.Presentation.DependencyInjection;

/// <summary>
/// Регистрация DI-зависимостей слоя: <c>ProductService.Presentation</c>.
/// </summary>
/// <inheritdoc cref="DependencyInjectorBase" path="/remarks"/>
/// <param name="loggerFactory"><inheritdoc cref="DependencyInjectorBase(ILoggerFactory)" path="/param[@name='loggerFactory']"/></param>
public class DependencyInjector(
    IConfiguration configuration,
    ILoggerFactory loggerFactory)
    : DependencyInjectorBase(loggerFactory)
{
    /// <inheritdoc />
    protected override IServiceCollection Process(
        IServiceCollection serviceCollection)
    {
        var allowedOrigins = configuration.GetValue<string>("AllowedOrigins");
        return serviceCollection
            .ConfigureSwaggerAuth()
            .AddCors(options =>
            {
                options.AddPolicy(
                    name: Constants.CorsDefaultPolicyName,
                    policy =>
                    {
                        policy.WithOrigins(allowedOrigins ?? "*");
                        policy.AllowAnyHeader();
                        policy.AllowAnyMethod();
                    });
            });
    }
}
```

### ApplicationBuilderExtensions

```csharp
using Microsoft.AspNetCore.Builder;
using Shared.Presentation.Core.Extensions;

namespace ProductService.Presentation.Extensions;

/// <summary>
/// Класс, который содержит расширения для <see cref="IApplicationBuilder"/>.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Конфигурирует <see cref="IApplicationBuilder"/>.
    /// </summary>
    /// <param name="app"><see cref="WebApplication"/>.</param>
    /// <returns><see cref="IApplicationBuilder"/>.</returns>
    public static IApplicationBuilder UseProductPresentation(
        this WebApplication app)
    {
        return app
            .UsePresentationCore()
            .UseCors(Constants.CorsDefaultPolicyName);
    }
}
```

### Маршрутизация контроллеров

Базовый маршрут контроллеров: `api/[appName]/[controllerType]/v1/[controller]`

Атрибуты `[AppName]` и `[ControllerType]` задаются на базовом классе контроллера:

```csharp
using Shared.Presentation.Core.Attributes;
using Shared.Presentation.Core.Controllers;

namespace ProductService.Api.Controllers.Base;

/// <summary>
/// Базовый класс для контроллеров Product-сервиса.
/// </summary>
/// <param name="logger">Логгер.</param>
[AppName(Constants.AppName)]
[ControllerType("product")]
public abstract class ProductControllerBase(
    ILogger logger)
    : ControllerBase(logger);
```

Маршрут для `ProductsController`: `api/productservice/product/v1/persons`

### Контроллер

Контроллер наследует базовый класс и использует метод `Process()` для обёртки CQRS-запросов:

```csharp
using MediatR;
using ProductService.Api.Controllers.Base;
using ProductService.Application.Features.ProductFeature.Cqrs.Queries;

namespace ProductService.Api.Controllers;

/// <summary>
/// Контроллер для взаимодействия с сущностями "Product".
/// </summary>
public sealed class ProductsController(
    ISender sender,
    ILogger<ProductsController> logger)
    : ProductControllerBase(logger)
{
    /// <summary>
    /// Возвращает постраничный список продуктов.
    /// </summary>
    /// <param name="request">Тело запроса.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns>Постраничный список продуктов.</returns>
    [HttpPost("list")]
    public Task<IActionResult> GetProductsAsync(
        [FromBody] ProductListRequest request,
        CancellationToken cancellationToken = default) =>
        Process(() => sender.Send(new ProductReadListQuery(request), cancellationToken));
}
```

> Метод `Process()` из `ControllerBase` автоматически логирует выполнение и обрабатывает исключения, возвращая корректный HTTP-статус.

---

## Step 7: Api проект — Program.cs и .env

### Program.cs

```csharp
using Shared.Presentation.Core.Extensions;
using ProductService.Presentation.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.ImplementDependencies();

var app = builder.Build();
app.UseProductPresentation();

app.Run();
```

Метод `ImplementDependencies()` (из `Shared.Presentation.Core.Extensions`):
1. Инициализирует `.env`-конфигурацию через `InitializeConfiguration()`
2. Регистрирует controllers с кастомными конвенциями маршрутизации
3. Добавляет `RequestLoggingFilter`
4. **Автоматически** находит все классы-наследники `DependencyInjectorBase` в ссылочных сборках и вызывает их `Inject()`

Метод `UsePresentationCore()` (из `Shared.Presentation.Core`):
- Подключает Swagger
- Обрабатывает исключения

### .env файл

Файл `.env` располагается в `src/.env` и содержит переменные окружения:

```env
Gpn__ProductService__DbSettings__ConnectionString="Host=localhost:5432;Database=product;Username=postgres;Password=postgres;commandtimeout=0;Include Error Detail=true;Search Path=public"
Gpn__ProductService__ExceptionMapperSettings__ShouldEnrichWithTrace=true
```

> Конфигурационные ключи используют `__` (двойное подчёркивание) — ASP.NET Core конвертирует их в иерархию `:`.

---

## Step 8: Добавление в solution

```bash
# Перейдите в папку с solution
cd src

# Добавьте все проекты
dotnet sln Template.sln add Services/Product/ProductService.Domain/ProductService.Domain.csproj
dotnet sln Template.sln add Services/Product/ProductService.Application/ProductService.Application.csproj
dotnet sln Template.sln add Services/Product/ProductService.Infrastructure/ProductService.Infrastructure.csproj
dotnet sln Template.sln add Services/Product/ProductService.Infrastructure.Dal/ProductService.Infrastructure.Dal.csproj
dotnet sln Template.sln add Services/Product/ProductService.Infrastructure.Mapping/ProductService.Infrastructure.Mapping.csproj
dotnet sln Template.sln add Services/Product/ProductService.Presentation/ProductService.Presentation.csproj
dotnet sln Template.sln add Services/Product/ProductService.Api/ProductService.Api.csproj
```

---

## Полный пример: сущность Product с CRUD

Ниже — пошаговая реализация полного CRUD для сущности `Product`.

### 1. Domain — сущность

```csharp
// ProductService.Domain/Entities/Product.cs
using Shared.Domain.Core.Interfaces;

namespace ProductService.Domain.Entities;

/// <summary>
/// Сущность "Product".
/// </summary>
public class Product : IEntity<Guid>, IWithCreated, IWithDeleted
{
    /// <inheritdoc cref="IEntity.Id"/>
    public Guid Id { get; private set; }

    /// <summary>
    /// Наименование.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Цена.
    /// </summary>
    public decimal Price { get; private set; }

    // IWithCreated
    public Guid? CreatedByUserId { get; private set; }
    public string? CreatedByUserName { get; private set; }
    public void SetCreatedByUserId(Guid? id) => CreatedByUserId = id;
    public void SetCreatedByUserName(string name) => CreatedByUserName = name;
    public void OnCreate(Guid? userId, string? userName)
    {
        CreatedByUserId = userId;
        CreatedByUserName = userName ?? string.Empty;
    }

    // IWithDeleted
    public bool IsDeleted { get; private set; }
    public Guid? DeletedByUserId { get; private set; }
    public void SetIsDeleted() => IsDeleted = true;
    public void OnDelete(Guid? userId) => DeletedByUserId = userId;

    /// <summary>
    /// Создание сущности "Product".
    /// </summary>
    public static Product Create(string name, decimal price) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Price = price,
    };

    /// <summary>
    /// Обновление сущности.
    /// </summary>
    public void Update(string name, decimal price)
    {
        Name = name;
        Price = price;
    }
}
```

### 2. Application — DTO

```csharp
// ProductService.Application/Abstractions/Dto/Product/Requests/ProductListRequest.cs
using Shared.Application.Core.Dto.Requests;

namespace ProductService.Application.Abstractions.Dto.Product.Requests;

/// <summary>
/// Запрос списка продуктов.
/// </summary>
public class ProductListRequest : PageableRequest<ProductFilter>;
```

```csharp
// ProductService.Application/Abstractions/Dto/Product/Requests/ProductFilter.cs
using Shared.Application.Core.Dto.Requests;
using Shared.Domain.Core.Dal.Models;

namespace ProductService.Application.Abstractions.Dto.Product.Requests;

/// <summary>
/// Фильтр для списка продуктов.
/// </summary>
public class ProductFilter : ListFilterBase;
```

```csharp
// ProductService.Application/Abstractions/Dto/Product/Requests/CreateProductRequest.cs
namespace ProductService.Application.Abstractions.Dto.Product.Requests;

/// <summary>
/// DTO на создание продукта.
/// </summary>
public class CreateProductRequest
{
    /// <summary>
    /// Наименование.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Цена.
    /// </summary>
    public decimal Price { get; set; }
}
```

```csharp
// ProductService.Application/Abstractions/Dto/Product/Responses/ProductPayload.cs
namespace ProductService.Application.Abstractions.Dto.Product.Responses;

/// <summary>
/// DTO продукта в ответе.
/// </summary>
public class ProductPayload
{
    /// <summary>
    /// Идентификатор.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Наименование.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Цена.
    /// </summary>
    public decimal Price { get; set; }
}
```

```csharp
// ProductService.Application/Abstractions/Dto/Product/Responses/ProductListResponse.cs
using Shared.Application.Core.Dto.Responses;

namespace ProductService.Application.Abstractions.Dto.Product.Responses;

/// <summary>
/// Ответ со списком продуктов.
/// </summary>
public class ProductListResponse : PageableResponse<ICollection<ProductPayload>>;
```

```csharp
// ProductService.Application/Abstractions/Dto/Product/Responses/ProductResponse.cs
using Shared.Application.Core.Dto.Responses;

namespace ProductService.Application.Abstractions.Dto.Product.Responses;

/// <summary>
/// Ответ с одним продуктом.
/// </summary>
public class ProductResponse : Response<ProductPayload>;
```

### 3. Application — CQRS Queries

```csharp
// ProductService.Application/Features/ProductFeature/Cqrs/Queries/ProductReadListQuery.cs
using Shared.Application.Cqrs.Core.Abstractions.Queries.Requests;
using ProductService.Application.Abstractions.Dto.Product.Requests;
using ProductService.Application.Abstractions.Dto.Product.Responses;

namespace ProductService.Application.Features.ProductFeature.Cqrs.Queries;

/// <summary>
/// Запрос на получение списка продуктов.
/// </summary>
public sealed record ProductReadListQuery(
    ProductListRequest Request)
    : ReadListQuery<ProductListRequest, ProductFilter, ProductListResponse, ProductPayload>(Request);
```

```csharp
// ProductService.Application/Features/ProductFeature/Cqrs/Queries/ProductReadListQueryHandler.cs
using Microsoft.Extensions.Logging;
using Shared.Application.Cqrs.Core.Abstractions.Queries.Handlers;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using ProductService.Domain.Entities;

namespace ProductService.Application.Features.ProductFeature.Cqrs.Queries;

/// <summary>
/// Handler запроса списка продуктов.
/// </summary>
public sealed class ProductReadListQueryHandler(
    ILoggerFactory loggerFactory,
    IUnitOfWork unitOfWork)
    : ReadListQueryHandler<ProductReadListQuery, ProductListRequest, ProductFilter,
        ProductListResponse, ProductPayload, Product>(loggerFactory, unitOfWork)
{
}
```

### 4. Application — CQRS Commands

```csharp
// ProductService.Application/Features/ProductFeature/Cqrs/Commands/CreateProductCommand.cs
using Shared.Application.Cqrs.Core.Abstractions.Commands.Requests;
using ProductService.Application.Abstractions.Dto.Product.Requests;
using ProductService.Application.Abstractions.Dto.Product.Responses;

namespace ProductService.Application.Features.ProductFeature.Cqrs.Commands;

/// <summary>
/// Команда создания продукта.
/// </summary>
public sealed record CreateProductCommand(
    CreateProductRequest Request)
    : CreateCommand<CreateProductRequest, ProductResponse>(Request);
```

```csharp
// ProductService.Application/Features/ProductFeature/Cqrs/Commands/CreateProductCommandHandler.cs
using Microsoft.Extensions.Logging;
using Shared.Application.Cqrs.Core.Abstractions;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using ProductService.Application.Abstractions.Dto.Product.Responses;
using ProductService.Domain.Entities;

namespace ProductService.Application.Features.ProductFeature.Cqrs.Commands;

/// <summary>
/// Handler команды создания продукта.
/// </summary>
public sealed class CreateProductCommandHandler(
    IUnitOfWork unitOfWork,
    ILoggerFactory loggerFactory)
    : EntityRequestHandler<CreateProductCommand, ProductResponse, Product>(unitOfWork, loggerFactory)
{
    /// <inheritdoc />
    public override async Task<ProductResponse> Handle(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var product = Product.Create(command.Request.Name, command.Request.Price);
        await Repository.AddAsync(product, null, null, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new ProductResponse
        {
            Payload = new ProductPayload { Id = product.Id, Name = product.Name, Price = product.Price },
        };
    }
}
```

```csharp
// ProductService.Application/Features/ProductFeature/Cqrs/Commands/UpdateProductCommand.cs
using Shared.Application.Cqrs.Core.Abstractions.Commands.Requests;
using ProductService.Application.Abstractions.Dto.Product.Requests;

namespace ProductService.Application.Features.ProductFeature.Cqrs.Commands;

/// <summary>
/// Команда обновления продукта.
/// </summary>
public sealed record UpdateProductCommand(
    object Key,
    UpdateProductRequest Request)
    : UpdateCommand<UpdateProductRequest, ProductResponse>(Key, Request);
```

```csharp
// ProductService.Application/Features/ProductFeature/Cqrs/Commands/UpdateProductCommandHandler.cs
using Microsoft.Extensions.Logging;
using Shared.Application.Cqrs.Core.Abstractions;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using ProductService.Application.Abstractions.Dto.Product.Responses;
using ProductService.Domain.Entities;

namespace ProductService.Application.Features.ProductFeature.Cqrs.Commands;

/// <summary>
/// Handler команды обновления продукта.
/// </summary>
public sealed class UpdateProductCommandHandler(
    IUnitOfWork unitOfWork,
    ILoggerFactory loggerFactory)
    : EntityRequestHandler<UpdateProductCommand, ProductResponse, Product>(unitOfWork, loggerFactory)
{
    /// <inheritdoc />
    public override async Task<ProductResponse> Handle(
        UpdateProductCommand command,
        CancellationToken cancellationToken)
    {
        var product = await Repository.GetAsync(command.Key, cancellationToken: cancellationToken);
        if (product is null)
        {
            return new ProductResponse { StatusCode = StatusCodes.Status404NotFound };
        }

        product.Update(command.Request.Name, command.Request.Price);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ProductResponse
        {
            Payload = new ProductPayload { Id = product.Id, Name = product.Name, Price = product.Price },
        };
    }
}
```

```csharp
// ProductService.Application/Features/ProductFeature/Cqrs/Commands/DeleteProductCommand.cs
using Shared.Application.Cqrs.Core.Abstractions.Commands.Requests;
using Shared.Application.Core.Dto.Responses;

namespace ProductService.Application.Features.ProductFeature.Cqrs.Commands;

/// <summary>
/// Команда удаления продукта.
/// </summary>
public sealed record DeleteProductCommand(object Key)
    : DeleteCommand(Key);
```

```csharp
// ProductService.Application/Features/ProductFeature/Cqrs/Commands/DeleteProductCommandHandler.cs
using Microsoft.Extensions.Logging;
using Shared.Application.Cqrs.Core.Abstractions;
using Shared.Application.Core.Dto.Responses;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using ProductService.Domain.Entities;

namespace ProductService.Application.Features.ProductFeature.Cqrs.Commands;

/// <summary>
/// Handler команды удаления продукта.
/// </summary>
public sealed class DeleteProductCommandHandler(
    IUnitOfWork unitOfWork,
    ILoggerFactory loggerFactory)
    : EntityRequestHandler<DeleteProductCommand, Response, Product>(unitOfWork, loggerFactory)
{
    /// <inheritdoc />
    protected override bool WithTracking => true;

    /// <inheritdoc />
    public override async Task<Response> Handle(
        DeleteProductCommand command,
        CancellationToken cancellationToken)
    {
        var product = await Repository.GetAsync(command.Key, cancellationToken: cancellationToken);
        if (product is null)
        {
            return new Response { StatusCode = StatusCodes.Status404NotFound };
        }

        await Repository.RemoveAsync(product, null, cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new Response { StatusCode = StatusCodes.Status200OK };
    }
}
```

### 5. Infrastructure.Dal — конфигурация и репозиторий

```csharp
// ProductService.Infrastructure.Dal/Configuration/EntityConfigurations.cs
using Shared.Infrastructure.Dal.EFCore.Configurations;
using ProductService.Domain.Entities;

namespace ProductService.Infrastructure.Dal.Configuration;

/// <summary>
/// Конфигурация сущности "Product".
/// </summary>
public class EntityConfiguration : EntityConfigurationBase<Product>
{
    /// <inheritdoc />
    protected override void ConfigureProcess(EntityTypeBuilder<Product> builder)
    {
        base.ConfigureProcess(builder);

        builder.ToTable("products", t => t.HasComment("Таблица продуктов."));
        builder.Property(x => x.Name).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Price).HasColumnType("decimal(18,2)");
    }
}
```

```csharp
// ProductService.Infrastructure.Dal/Repositories/ProductRepository.cs
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Infrastructure.Dal.EFCore.Repository;
using ProductService.Domain.Entities;

namespace ProductService.Infrastructure.Dal.Repositories;

/// <summary>
/// Репозиторий для Product.
/// </summary>
/// <param name="dbContext"><inheritdoc /></param>
/// <param name="evaluator"><inheritdoc /></param>
public class ProductRepository(
    DbContext dbContext,
    IQueryEvaluator evaluator)
    : EfRepository<Product>(dbContext, evaluator);
```

### 6. Presentation — контроллер

```csharp
// ProductService.Api/Controllers/ProductsController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProductService.Api.Controllers.Base;
using ProductService.Application.Abstractions.Dto.Product.Requests;
using ProductService.Application.Features.ProductFeature.Cqrs.Commands;
using ProductService.Application.Features.ProductFeature.Cqrs.Queries;

namespace ProductService.Api.Controllers;

/// <summary>
/// Контроллер для взаимодействия с сущностями "Product".
/// </summary>
public sealed class ProductsController(
    ISender sender,
    ILogger<ProductsController> logger)
    : ProductControllerBase(logger)
{
    /// <summary>
    /// Возвращает постраничный список продуктов.
    /// </summary>
    [HttpPost("list")]
    public Task<IActionResult> GetProductsAsync(
        [FromBody] ProductListRequest request,
        CancellationToken cancellationToken = default) =>
        Process(() => sender.Send(new ProductReadListQuery(request), cancellationToken));

    /// <summary>
    /// Создаёт продукт.
    /// </summary>
    [HttpPost]
    public Task<IActionResult> CreateProductAsync(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken = default) =>
        Process(() => sender.Send(new CreateProductCommand(request), cancellationToken));

    /// <summary>
    /// Удаляет продукт.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public Task<IActionResult> DeleteProductAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Process(() => sender.Send(new DeleteProductCommand(id), cancellationToken));
}
```

### 7. Program.cs

```csharp
// ProductService.Api/Program.cs
using Shared.Presentation.Core.Extensions;
using ProductService.Presentation.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.ImplementDependencies();

var app = builder.Build();
app.UseProductPresentation();

app.Run();
```

---

## См. также

| Документ | Описание |
|----------|----------|
| [CQRS](cqrs.md) | CQRS-паттерн: команды, запросы, handlers |
| [Entity Interfaces](entity-interfaces.md) | Интерфейсы сущностей: `IEntity`, `IWithCreated`, `IWithDeleted` |
| [EF Core Internals](efcore-internals.md) | Внутреннее устройство EF Core: `DbContextBase`, конфигурации |
| [Controllers](controllers.md) | Контроллеры: базовые классы, маршрутизация, `Process()` |
| [Service Startup](service-startup.md) | Запуск сервиса: `ImplementDependencies`, `.env`, конфигурация |
| [Auto-Registration](auto-registration.md) | Автоматическая DI-регистрация через `DependencyInjectorBase` |
| [Configuration](configuration.md) | Конфигурация: `.env`, `IConfiguration`, `GetOptions<T>()` |
| [Auth Provider](auth-provider.md) | Провайдер пользователя: `IUserProvider`, аудит-поля |