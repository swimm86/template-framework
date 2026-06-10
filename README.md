# Shared — Unified .NET Application Framework

**Shared** — это унифицированный фреймворк для создания .NET приложений с готовой архитектурой, реализующий лучшие практики enterprise-разработки.

## 📋 Содержание

- [О проекте](#-о-проекте)
- [Архитектурные паттерны](#-архитектурные-паттерны)
- [Структура репозитория](#-структура-репозитория)
- [Компоненты Shared](#-компоненты-shared)
- [Services — примеры использования](#-services--примеры-использования)
- [Начало работы](#-начало-работы)
- [Документация](#-документация)

---

## 📖 О проекте

Данный проект предоставляет:

- **Shared** — производственный фреймворк с реализацией паттернов Repository, Unit of Work, Specification, CQRS
- **Services** — референсные реализации микросервисов (BFF, Getter, Setter) демонстрирующие best practices

Фреймворк следует принципам **Clean Architecture** и **Domain-Driven Design**, обеспечивая:
- Разделение ответственности между слоями
- Тестируемость через абстракции
- Масштабируемость за счет модульности
- Консистентность кодовой базы

---

## 🏛 Архитектурные паттерны

Фреймворк реализует следующие паттерны (подробная документация в [`docs/`](docs/)):

| Паттерн | Описание | Документация |
|---------|----------|--------------|
| **Repository** | Централизованный доступ к данным через `IRepository<TEntity>` | [docs/repository.md](docs/repository.md) |
| **Unit of Work** | Координация транзакций и действий перехвата | [docs/unit-of-work.md](docs/unit-of-work.md) |
| **Specification** | Инкапсуляция бизнес-критериев выборки | [docs/specification.md](docs/specification.md) |
| **CQRS** | Разделение операций чтения и записи | [docs/cqrs.md](docs/cqrs.md) |
| **Auto-Registration** | Автоматическая регистрация зависимостей | [docs/auto-registration.md](docs/auto-registration.md) |
| **Pipeline Behaviors** | Cross-cutting concerns через MediatR | [docs/pipeline-behaviors.md](docs/pipeline-behaviors.md) |
| **Exception Mapping** | Преобразование исключений в Problem Details | [docs/exception-mapping.md](docs/exception-mapping.md) |
| **Job Scheduler** | `IScheduledJob`, `IJobScheduler`, middleware pipeline, Quartz/Hangfire adapters | [docs/job-scheduler.md](docs/job-scheduler.md) |

---

## 🗂 Структура репозитория

```
src/
├── Template.sln                         # Solution
│
├── Shared/                              # Унифицированный фреймворк
│   ├── Core/                            # Базовые компоненты ядра
│   │   ├── Shared.Common
│   │   ├── Shared.Domain.Core
│   │   ├── Shared.Application.Core
│   │   ├── Shared.Application.Cqrs.Core
│   │   ├── Shared.Infrastructure.Core
│   │   └── Shared.Presentation.Core
│   ├── Dal/                             # Слой доступа к данным
│   │   ├── Shared.Infrastructure.Dal.EFCore
│   │   └── Shared.Infrastructure.Dal.EFCore.Postgres
│   ├── Logging/                         # Логирование
│   ├── Mapper/                          # Маппинг (AutoMapper)
│   ├── Job/                             # Фоновые задачи
│   └── Utils/                           # Утилиты
│
├── Services/                            # Примеры микросервисов
│   ├── Bff/                             # BFF (Backend For Frontend)
│   ├── Getter/                          # Сервис чтения данных
│   ├── Setter/                          # Сервис записи данных
│   ├── Common/                          # Общие компоненты сервисов
│   └── DatabaseUpgrade/                 # Миграции БД
│
└── Tests/                               # Тесты
    ├── test.runsettings                  # Конфигурация запуска (покрытие, фильтры)
    └── Shared/
        ├── Shared.Testing                # Библиотека тестовых helpers (FakeMapper, FakeRepository, и т.д.)
        ├── Core/
        │   ├── Shared.Common.Tests
        │   ├── Shared.Domain.Core.Tests
        │   ├── Shared.Application.Core.Tests
        │   ├── Shared.Application.Cqrs.Core.Tests
        │   ├── Shared.Presentation.Core.Tests
        │   └── Shared.Infrastructure.Core.Tests
        ├── Dal/
        │   └── Shared.Infrastructure.Dal.EFCore.Tests
        ├── Job/
        │   ├── Shared.Infrastructure.Job.Quartz.Tests
        │   └── Shared.Infrastructure.Job.Hangfire.Tests
        ├── Logging/
        │   └── Shared.Infrastructure.Logging.Tests
        ├── Mapper/
        │   └── Shared.Infrastructure.Mapper.AutoMapper.Tests
        └── Utils/
            └── Shared.Utils.DatabaseUpgrade.Tests
```

---

## 🔧 Компоненты Shared

**Shared** — это набор библиотек, которые можно использовать в любом .NET приложении.

### Core
| Компонент | Описание |
|-----------|----------|
| `Shared.Common` | Общие утилиты, расширения LINQ, работа со строками, Enum, JSON, пагинация, batch-обработка |
| `Shared.Domain.Core` | Базовые доменные сущности и интерфейсы |
| `Shared.Application.Core` | Базовые сервисы приложения, валидация, **авто-регистрация зависимостей**, конфигурация (.env + appsettings) |
| `Shared.Application.Cqrs.Core` | CQRS паттерны (команды, запросы, обработчики), MediatR pipeline behaviors |
| `Shared.Infrastructure.Core` | Базовая инфраструктура, общие сервисы, API Client pipeline |
| `Shared.Presentation.Core` | Базовые контроллеры, DTO, фильтры, Swagger, обработка исключений |

### Data Access
| Компонент | Описание |
|-----------|----------|
| `Shared.Infrastructure.Dal.EFCore` | Базовая реализация репозиториев на EF Core, Unit of Work |
| `Shared.Infrastructure.Dal.EFCore.Postgres` | Расширения для работы с PostgreSQL |

### Инфраструктура
| Компонент | Описание |
|-----------|----------|
| `Shared.Infrastructure.Logging` | Централизованное логирование |
| `Shared.Infrastructure.Mapper.AutoMapper` | Настройка AutoMapper |
| `Shared.Infrastructure.Job.Quartz` | Планировщик задач Quartz.NET |
| `Shared.Infrastructure.Job.Hangfire` | Планировщик задач Hangfire |
| `Shared.Utils.DatabaseUpgrade` | Утилиты для миграции БД |

---

## 🚀 Services — примеры использования

В директории **Services** представлены примеры реализации микросервисов на основе Shared:

### Архитектура микросервисов

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  Getter API │◀────│  BFF API    │────▶│  Setter API │
│  (Read)     │     │   (Frontend)│     │  (Write)    │
└─────────────┘     └─────────────┘     └─────────────┘
```

### Сервисы

| Сервис | Назначение |
|--------|------------|
| **Bff** | Backend For Frontend — агрегация данных, адаптация под фронтенд |
| **Getter** | Сервис чтения данных (CQRS Query side) |
| **Setter** | Сервис записи данных (CQRS Command side) |
| **Common** | Общие переиспользуемые компоненты всех сервисов |
| **DatabaseUpgrade** | Миграции и обновление схемы БД |

> **Примечание:** Services — это примеры для разработчиков, демонстрирующие best practices использования Shared в микросервисной архитектуре. **Это не production-код** — сервисы упрощены для наглядности и не включают auth, message bus, distributed tracing и другие enterprise-компоненты.

> **Планировщик задач:** сервисы могут подключать либо `Shared.Infrastructure.Job.Quartz`, либо `Shared.Infrastructure.Job.Hangfire` — без правок бизнес-кода (см. [Job Scheduler](docs/job-scheduler.md)).

### Взаимодействие сервисов

BFF общается с Getter и Setter через HTTP-клиенты:

```
┌─────────────┐  HTTP POST  ┌─────────────┐  HTTP POST  ┌─────────────┐
│  Getter API │────────────▶│  BFF API    │────────────▶│  Setter API │
│  (Read)     │◀────────────│   (Frontend)│◀────────────│  (Write)    │
└─────────────┘  Response   └─────────────┘  Response   └─────────────┘
```

| Поток | Описание |
|-------|----------|
| BFF → Getter | Запрос списка через `IGetterClient` |
| BFF → Setter | Создание/обновление данных через `ISetterClient` |
| Getter & Setter | Чтение данных из общей БД (через EF Core Repository) |

### Структура каждого сервиса

Каждый микросервис следует единой структуре:

```
Service (Bff):
Service/
├── Api/                       # Web API слой (Controllers, Middleware, Program.cs)
├── Application/               # Бизнес-логика (Commands, Queries, Handlers, Validators)
└── Infrastructure/            # Инфраструктурные реализации (Repositories, External Services)

Service (Getter/Setter):
Service/
├── Api/                       # Web API слой
├── Application/               # Бизнес-логика
├── Application.Abstractions/  # Общие для других сервисов (например, для Bff) интерфейсы и контракты (dto, request, response, контракты CQRS)
└── Infrastructure/            # Инфраструктурные реализации
```

> **Примечание:** `Abstractions` присутствует только в Getter/Setter. Bff использует 3-проектную структуру (без Abstractions, тк никто не знает о Bff, но Bff знает обо всех). В `Services/Common` дополнительно выделены `Domain`, `Infrastructure.Dal`, `Infrastructure.Mapping` и `Presentation`.

### Common компоненты

В `Services/Common` находятся переиспользуемые компоненты:

| Проект | Описание |
|--------|----------|
| `Template.Domain` | Доменные модели (Entities, ValueObjects, Enums) |
| `Template.Application` | Общие сервисы приложения, валидаторы, behaviors |
| `Template.Infrastructure` | Общая инфраструктура, HTTP clients |
| `Template.Infrastructure.Dal` | EF Core конфигурации, репозитории |
| `Template.Infrastructure.Mapping` | AutoMapper profiles |
| `Template.Presentation` | Общие DTO, Response models, Swagger config |

---

## 🧪 Тестирование

Проект использует **xUnit** как основной фреймворк для тестирования.

### Тестовые проекты

| Проект | Описание |
|--------|----------|
| `Shared.Common.Tests` | Unit-тесты для Common (утилиты, расширения, helpers, пагинация) |
| `Shared.Domain.Core.Tests` | Unit-тесты для Domain.Core (EntityBase, IEntity, audit-интерфейсы, спецификации) |
| `Shared.Application.Core.Tests` | Unit-тесты для Application.Core (CQRS handlers, validators, behaviors) |
| `Shared.Application.Cqrs.Core.Tests` | Unit-тесты для CQRS Core (команды, запросы, pipeline) |
| `Shared.Presentation.Core.Tests` | Unit-тесты для Presentation.Core (ExceptionHandler, мапперы исключений) |
| `Shared.Infrastructure.Core.Tests` | Unit-тесты для Infrastructure.Core (ApiClient, DI, сервисы) |
| `Shared.Infrastructure.Dal.EFCore.Tests` | Unit-тесты для EF Core (Repository, UnitOfWork) |
| `Shared.Infrastructure.Job.Quartz.Tests` | Unit-тесты для Quartz (планировщик, JobContext) |
| `Shared.Infrastructure.Job.Hangfire.Tests` | Unit-тесты для Hangfire (планировщик, адаптер) |
| `Shared.Infrastructure.Logging.Tests` | Unit-тесты для Logging (LogTask, логирование) |
| `Shared.Infrastructure.Mapper.AutoMapper.Tests` | Unit-тесты для AutoMapper (конфигурация, IMapper) |
| `Shared.Utils.DatabaseUpgrade.Tests` | Тесты для DatabaseUpgrade утилит (включая интеграционные) |
| `Shared.Testing` | Библиотека helpers (FakeMapper, FakeRepository, FakeLogger, FakeUnitOfWork, TestEntity, ServiceProviderBuilder) |

### Запуск тестов

```bash
# Все тесты
dotnet test src/Template.sln

# Конкретный проект
dotnet test src/Tests/Shared/Core/Shared.Application.Core.Tests

# С покрытием (через test.runsettings)
dotnet test src/Template.sln --settings src/Tests/test.runsettings --collect:"XPlat Code Coverage"

# Только unit-тесты (интеграционные исключаются по умолчанию)
dotnet test --settings src/Tests/test.runsettings

# Фильтрация по имени
dotnet test src/Template.sln --filter "DisplayName~Cqrs"

# Запуск интеграционных тестов
dotnet test src/Tests/Shared/Utils/Shared.Utils.DatabaseUpgrade.Tests --filter "Category=Integration"

# Verbose output
dotnet test src/Template.sln -v n
```

### Соглашения по тестам

- **Naming:** `MethodUnderTest_State_ExpectedBehavior`
- **Pattern:** AAA (Arrange-Act-Assert)
- **Фреймворки:** xUnit, FluentAssertions 7.x, coverlet 10.0.0; **без Moq/AutoFixture** — ручные Fake/Stubs в `Shared.Testing`
- **Покрытие:** coverlet (cobertura + lcov), настройка в `src/Tests/test.runsettings`
- **Фильтрация:** интеграционные тесты помечаются `[Trait("Category", "Integration")]` и исключаются из PR gate
- **Shared.Testing** предоставляет helpers: FakeMapper, FakeRepository, FakeLogger, FakeUnitOfWork, TestEntity, ServiceProviderBuilder, TestConfigurationBuilder

---

## 🛠 Начало работы

### Требования

- .NET 8+
- Visual Studio / JetBrains Rider
- PostgreSQL (для работы с БД)

### Сборка проекта

```bash
cd src
dotnet restore
dotnet build
```

### Запуск сервисов

```bash
# Запуск BFF
cd src
dotnet run --project Services/Bff/Template.Bff.Api

# Запуск Getter
dotnet run --project Services/Getter/Template.Getter.Api

# Запуск Setter
dotnet run --project Services/Setter/Template.Setter.Api
```

### Конфигурация

Фреймворк поддерживает два способа хранения конфигурации:

1. **appsettings.json** — стандартный способ .NET
2. **.env файлы** — централизованное хранение настроек (рекомендуется для Services)

Пример `.env`:
```env
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=template;Username=postgres;Password=yourpassword
Logging__LogLevel__Default=Information
```

Приоритет конфигурации (от высшего к низшему):
1. Переменные окружения OS
2. `.env` (базовый файл)
3. `.env.{EnvironmentName}` (например, `.env.Development`) — загружается при наличии (перезаписывает значения из `.env`, если таковой имеется)
4. `appsettings.json`

### Миграции БД

Для применения миграций используйте проект `Template.DatabaseUpgrade`:

```bash
cd src
dotnet run --project Services/DatabaseUpgrade/Template.DatabaseUpgrade
```

---

## 📚 Документация

Дополнительная документация доступна в директории [`docs/`](docs/):

### Архитектура и паттерны
- [Repository Pattern](docs/repository.md) — доступ к данным через репозиторий
- [Unit of Work Pattern](docs/unit-of-work.md) — координация транзакций
- [Specification Pattern](docs/specification.md) — инкапсуляция критериев выборки
- [CQRS](docs/cqrs.md) — разделение команд и запросов через MediatR
- [Auto-Registration](docs/auto-registration.md) — автоматическая регистрация DI зависимостей
- [Pipeline Behaviors](docs/pipeline-behaviors.md) — cross-cutting concerns (logging, validation)
- [Exception Mapping](docs/exception-mapping.md) — преобразование исключений в Problem Details

### Domain Layer
- [Domain Modeling](docs/domain-modeling.md) — BaseEntity, исключения, атрибуты сущностей
- [Lifecycle Actions](docs/lifecycle-actions.md) — `ILifecycleActionHandler`, `LifecycleActionHandlerBase`, `ILifecycleActionOrchestrator`, `LifecycleActionOrchestrator`, `ILifecycleActionGate`, `LifecycleActionGate`, `ILifecycleEntityRegistry`, `LifecycleEntityRegistry`, `LifecycleActionValidator`, `EntityKey`
- [Entity Interfaces](docs/entity-interfaces.md) — IEntity, audit interfaces, soft delete
- [Auth Provider](docs/auth-provider.md) — IUserProvider, аудит-поля, авто-заполнение CreatedBy/UpdatedBy

### Data Access
- [EF Core Internals](docs/efcore-internals.md) — DbContextBase, EntityConfigurationBase, EfQueryEvaluator
- [Db Seeder](docs/db-seeder.md) — [Seed] атрибут, ISeed, DbSeeder
- [Database Upgrade](docs/database-upgrade.md) — DbUp SQL migration runner

### Presentation Layer
- [Controllers](docs/controllers.md) — ControllerBase, routing conventions, app configuration
- [Response Types](docs/response-types.md) — Response<T>, PageableResponse, ErrorResponse
- [Swagger](docs/swagger.md) — OpenAPI schema filters, nullability sync
- [Request Logging](docs/request-logging.md) — RequestLoggingFilter, [DoNotLog] attribute

### Infrastructure
- [Api Client](docs/api-client.md) — HTTP-клиент, валидаторы, delegating handlers, AdditionalData flow
- [Cache](docs/cache.md) — CacheService<T>, ScopedMemoryCache, thundering herd prevention
- [Configuration](docs/configuration.md) — .env support, GetOptions<TOptions>(), module-based resolution
- [Correlation ID](docs/correlation-id.md) — distributed tracing, JobCorrelationContext
- [Mapping](docs/mapping.md) — IMapper abstraction, AutoMapper, ConfigureCollection diff-merge
- [Logging](docs/logging.md) — LogTask, [LogMethod] attribute, Fody weaving
- [NLog Configuration](docs/nlog-configuration.md) — NlogSettings, correlation layout renderers
- [FluentValidation Integration](docs/fluent-validation-integration.md) — auto-discovery, pipeline validation

### Job Scheduler
- [Job Scheduler](docs/job-scheduler.md) — обзор, `IScheduledJob`, `IJobScheduler`, middleware pipeline
- [Architecture](docs/job-scheduler/architecture.md) — слои, DIP/SOLID, поток выполнения
- [Pipeline](docs/job-scheduler/pipeline.md) — `IScheduledJobMiddleware`, Logging/Correlation/Retry
- [Quartz Adapter](docs/job-scheduler/quartz-adapter.md) — `QuartzJobScheduler`, `QuartzScheduledJobAdapter`
- [Hangfire Adapter](docs/job-scheduler/hangfire-adapter.md) — `HangfireJobScheduler`, `HangfireScheduledJobAdapter`
- [Zero-Touch Proof](docs/job-scheduler/zero-touch-proof.md) — смена Quartz ↔ Hangfire = 0 правок

### Руководства
- [Service Startup](docs/service-startup.md) — полный bootstrap-флоу, middleware pipeline, DependencyInjector
- [Service Creation Guide](docs/service-creation-guide.md) — пошаговое руководство по созданию микросервиса
- [Services Guide](docs/services.md) — демонстрационные сервисы: паттерны, доменная модель, ограничения
- [Batch Helper](docs/batch-helper.md) — пакетная обработка данных
- [Batch Request](docs/batch-request.md) — массовые запросы к API
- [Filtering & Sorting](docs/filtering-sorting-guide.md) — фильтрация и сортировка
- [Testing](docs/testing.md) — стек, соглашения и команды тестирования
- [Common Extensions](docs/common-extensions.md) — LINQ, Expression, String, Enum extensions
- [Property Reflection](docs/property-reflection.md) — compiled expression cache, IPropertyGetter

### Таблица перекрёстных ссылок

| Документ | Описание | Связанные |
|----------|----------|-----------|
| [Repository](docs/repository.md) | IRepository<T>, базовые CRUD | [Unit of Work](docs/unit-of-work.md), [Specification](docs/specification.md), [EF Core](docs/efcore-internals.md) |
| [Unit of Work](docs/unit-of-work.md) | IUnitOfWork, транзакции | [Repository](docs/repository.md), [Lifecycle Actions](docs/lifecycle-actions.md) |
| [Specification](docs/specification.md) | SpecificationBase, Include, Where | [Repository](docs/repository.md), [Filtering](docs/filtering-sorting-guide.md) |
| [CQRS](docs/cqrs.md) | ICommand, IQuery, handlers | [Pipeline Behaviors](docs/pipeline-behaviors.md), [Exception Mapping](docs/exception-mapping.md), [FluentValidation](docs/fluent-validation-integration.md) |
| [Auto-Registration](docs/auto-registration.md) | RegisterDerivedTypeDependencies | [CQRS](docs/cqrs.md), [Controllers](docs/controllers.md) |
| [Pipeline Behaviors](docs/pipeline-behaviors.md) | Logging, Validation pipeline | [CQRS](docs/cqrs.md), [Logging](docs/logging.md) |
| [Exception Mapping](docs/exception-mapping.md) | IExceptionMapper, Problem Details | [CQRS](docs/cqrs.md), [Response Types](docs/response-types.md) |
| [Domain Modeling](docs/domain-modeling.md) | BaseEntity, исключения | [Lifecycle Actions](docs/lifecycle-actions.md), [Entity Interfaces](docs/entity-interfaces.md) |
| [Lifecycle Actions](docs/lifecycle-actions.md) | `ILifecycleActionHandler`, `ILifecycleActionOrchestrator`, `ILifecycleActionGate`, `ILifecycleEntityRegistry` | [Domain Modeling](docs/domain-modeling.md), [Unit of Work](docs/unit-of-work.md) |
| [Entity Interfaces](docs/entity-interfaces.md) | IEntity, audit, soft delete | [Domain Modeling](docs/domain-modeling.md), [EF Core](docs/efcore-internals.md) |
| [EF Core Internals](docs/efcore-internals.md) | DbContextBase, EfQueryEvaluator | [Repository](docs/repository.md), [Db Seeder](docs/db-seeder.md) |
| [Db Seeder](docs/db-seeder.md) | [Seed], ISeed, DbSeeder | [EF Core](docs/efcore-internals.md), [Database Upgrade](docs/database-upgrade.md) |
| [Controllers](docs/controllers.md) | ControllerBase, routing | [CQRS](docs/cqrs.md), [Swagger](docs/swagger.md), [Request Logging](docs/request-logging.md) |
| [Response Types](docs/response-types.md) | Response<T>, ErrorResponse | [Controllers](docs/controllers.md), [Exception Mapping](docs/exception-mapping.md) |
| [Swagger](docs/swagger.md) | OpenAPI schema filters | [Controllers](docs/controllers.md), [Response Types](docs/response-types.md) |
| [Request Logging](docs/request-logging.md) | RequestLoggingFilter, [DoNotLog] | [Controllers](docs/controllers.md), [Logging](docs/logging.md) |
| [Api Client](docs/api-client.md) | HTTP-клиент, handlers | [Correlation ID](docs/correlation-id.md), [Exception Mapping](docs/exception-mapping.md) |
| [Cache](docs/cache.md) | CacheService<T>, ScopedMemoryCache | [Job Scheduler](docs/job-scheduler.md), [CQRS](docs/cqrs.md) |
| [Configuration](docs/configuration.md) | .env, GetOptions<TOptions> | [Api Client](docs/api-client.md), [Controllers](docs/controllers.md) |
| [Correlation ID](docs/correlation-id.md) | distributed tracing | [Api Client](docs/api-client.md), [Logging](docs/logging.md), [Job Scheduler](docs/job-scheduler.md) |
| [Mapping](docs/mapping.md) | IMapper, ConfigureCollection | [CQRS](docs/cqrs.md), [EF Core](docs/efcore-internals.md) |
| [Job Scheduler](docs/job-scheduler.md) | `IScheduledJob`, `IJobScheduler`, middleware pipeline | [Cache](docs/cache.md), [Correlation ID](docs/correlation-id.md) |
| [Logging](docs/logging.md) | LogTask, [LogMethod] | [Pipeline Behaviors](docs/pipeline-behaviors.md), [NLog](docs/nlog-configuration.md) |
| [NLog Configuration](docs/nlog-configuration.md) | NlogSettings, layout renderers | [Logging](docs/logging.md), [Correlation ID](docs/correlation-id.md) |
| [FluentValidation](docs/fluent-validation-integration.md) | auto-discovery, pipeline | [CQRS](docs/cqrs.md), [Pipeline Behaviors](docs/pipeline-behaviors.md) |
| [Batch Helper](docs/batch-helper.md) | пакетная обработка | [Batch Request](docs/batch-request.md), [Common Extensions](docs/common-extensions.md) |
| [Batch Request](docs/batch-request.md) | массовые API запросы | [Batch Helper](docs/batch-helper.md), [Filtering](docs/filtering-sorting-guide.md) |
| [Filtering & Sorting](docs/filtering-sorting-guide.md) | фильтрация, сортировка | [Specification](docs/specification.md), [Batch Request](docs/batch-request.md) |
| [Testing](docs/testing.md) | xUnit, покрытие, PR gate | [CQRS](docs/cqrs.md), [FluentValidation](docs/fluent-validation-integration.md) |
| [Auth Provider](docs/auth-provider.md) | IUserProvider, аудит-поля | [Entity Interfaces](docs/entity-interfaces.md), [EF Core](docs/efcore-internals.md) |
| [Service Startup](docs/service-startup.md) | bootstrap-флоу, middleware pipeline | [Auto-Registration](docs/auto-registration.md), [Configuration](docs/configuration.md), [Controllers](docs/controllers.md) |
| [Service Creation](docs/service-creation-guide.md) | пошаговое руководство (Bff / Getter-Setter / Common) | [CQRS](docs/cqrs.md), [Entity Interfaces](docs/entity-interfaces.md), [EF Core](docs/efcore-internals.md) |
| [Services Guide](docs/services.md) | демо-сервисы, паттерны, ограничения | [Service Creation](docs/service-creation-guide.md), [CQRS](docs/cqrs.md), [Api Client](docs/api-client.md) |
| [Common Extensions](docs/common-extensions.md) | LINQ, Expression, String | [Specification](docs/specification.md), [Batch Helper](docs/batch-helper.md) |
| [Property Reflection](docs/property-reflection.md) | compiled expression cache | [Api Client](docs/api-client.md), [Common Extensions](docs/common-extensions.md) |
| [Database Upgrade](docs/database-upgrade.md) | DbUp SQL migrations | [EF Core](docs/efcore-internals.md), [Db Seeder](docs/db-seeder.md) |

---

## 🤝 Для разработчиков

Этот репозиторий предназначен в первую очередь для внутренней разработки и служит:

1. **Шаблоном** для создания новых микросервисов
2. **Библиотекой** готовых компонентов (Shared)
3. **Примером** правильной архитектуры (Services)

### Создание нового сервиса

1. **Скопируйте структуру** из существующего сервиса. В репозитории два шаблона:
   - **Bff-стиль** — копируйте из `Services/Bff`:
   ```
   Services/MyNewService/
   ├── Template.MyNewService.Api/                      # Presentation layer (Controllers, Program.cs)
   ├── Template.MyNewService.Application/              # Application layer (CQRS handlers, validators)
   └── Template.MyNewService.Infrastructure/           # Infrastructure (Repositories, HTTP clients)
   ```
   - **Getter/Setter-стиль** — копируйте из `Services/Getter` или `Services/Setter`:
   ```
   Services/MyNewService/
   ├── Template.MyNewService.Api/                       # Presentation layer
   ├── Template.MyNewService.Application/               # Application layer (CQRS)
   ├── Template.MyNewService.Application.Abstractions/  # Контракты (IClient, CQRS interfaces)
   └── Template.MyNewService.Infrastructure/            # Infrastructure
   ```
   - **Common-стиль** — копируйте из `Services/Common` (используется для переиспользуемых компонентов, включает `Domain`, `Infrastructure.Dal`, `Infrastructure.Mapping`, `Presentation`).

2. **Обновите namespace'ы** — замените `Template.MyNewService` на ваш namespace

3. **Добавьте ссылки** на необходимые Shared компоненты:
   - `Shared.Application.Core` — базовые сервисы, auto-registration
   - `Shared.Application.Cqrs.Core` — CQRS инфраструктура
   - `Shared.Infrastructure.Dal.EFCore` — репозитории, Unit of Work
   - `Shared.Presentation.Core` — базовые контроллеры, Swagger

4. **Реализуйте бизнес-логику** в слое Application:
   - Commands/Queries в `Application/Commands/` и `Application/Queries/`
   - Handlers с `ICommandHandler<T>` / `IQueryHandler<T>`
   - Validators с FluentValidation

5. **Настройте инфраструктуру**:
   - DbContext наследуется от `DbContextBase<TSettings, TContext>` в `Infrastructure/Persistence/`
   - UnitOfWork наследуются от `EfUnitOfWork`
   - External HTTP clients на базе `ApiClientBase` / `ApiClient`

6. **Создайте `.env` файл** для конфигурации

7. **Добавьте проекты в solution** (пример для Bff-стиля):
   ```bash
   dotnet sln src/Template.sln add \
     Services/MyNewService/Template.MyNewService.Api/Template.MyNewService.Api.csproj \
     Services/MyNewService/Template.MyNewService.Application/Template.MyNewService.Application.csproj \
     Services/MyNewService/Template.MyNewService.Infrastructure/Template.MyNewService.Infrastructure.csproj
   ```

---

## 📄 Лицензия

[Укажите лицензию вашего проекта]

---

## 📞 Поддержка

По вопросам обращайтесь к команде разработки или создавайте Issues в репозитории.
