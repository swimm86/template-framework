# Services Guide — Демонстрационные сервисы

> Services — это **учебные примеры**, а не production-код. Сервисы упрощены для наглядности и демонстрируют правильное использование Shared.

---

## ⚠️ Что это и зачем

Директория `Services/` содержит референсные реализации микросервисов, которые показывают:

- Как правильно организовать слои (Api/Application/Infrastructure)
- Как использовать Shared компоненты в реальном проекте
- Как настроить CQRS, валидацию, маппинг, репозитории
- Как сервисы взаимодействуют через HTTP-клиенты

**Не используйте этот код как-is в production** — это упрощённая демонстрация без auth, message bus, distributed tracing и других enterprise-компонентов.

---

## 🏗 Архитектура взаимодействия

```
┌─────────────┐  HTTP POST  ┌─────────────┐  HTTP POST  ┌─────────────┐
│  BFF API    │────────────▶│  Getter API │────────────▶│  Setter API │
│  (Frontend) │◀────────────│   (Read)    │◀────────────│  (Write)    │
└─────────────┘  Response   └─────────────┘  Response   └─────────────┘
```

| Поток | Клиент | Метод | Описание |
|-------|--------|-------|----------|
| BFF → Getter | `IGetterClient` | `POST /api/persons/{services\|cqrs}/list` | Получение списка персон с фильтрацией; выбор ветки — через BFF-handler на основе `UseCqrs` |
| BFF → Setter | `ISetterClient` | `POST /api/persons/create` | Создание новой персоны |
| Getter → БД | EF Core Repository | `IRepository<Person>` | Чтение через Specification |
| Setter → БД | EF Core Repository | `IRepository<Person>` | Запись через Unit of Work |

> **BFF вызывает Getter через `POST`** (а не `GET`): тело содержит `PersonListRequest` с фильтрами/пагинацией. Подробнее см. [filtering-sorting-guide.md](filtering-sorting-guide.md).

---

## 📦 Доменная модель

Все сервисы работают с единой сущностью **Person**:

```csharp
// Services/Common/Template.Domain/Entities/Person.cs
public class Person : EntityBase<Guid>
{
    public Guid Id { get; init; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public byte[] Hash { get; private set; }

    public static Person Create(string name, string email) { /* ... */ }
    public void UpdateHash() => Hash = HashHelper.ComputeSha256(Name, Email);
}
```

### DTO слоёв

| Слой | DTO | Назначение |
|------|-----|------------|
| **BFF** | `PersonCreateDto`, `PersonDto` | Адаптация для фронтенда |
| **Getter** | `PersonListResponse`, `PersonListFilter` | Ответы и фильтры чтения |
| **Setter** | `PersonCreateRequest`, `PersonCreateResponse` | Запросы и ответы записи |

---

## 🎯 Демонстрируемые паттерны

### CQRS

| Паттерн | Где показан | Файл |
|---------|-------------|------|
| **Query + Handler** | Getter | `Features/Person/Cqrs/List/PersonListQuery.cs` + `PersonReadListQueryHandler.cs` |
| **Command + Handler** | Setter | `Features/Person/Create/PersonCreateCommand.cs` + `PersonCreateCommandHandler.cs` |
| **Request/Response models** | BFF | `Features/Queries/Person/Cqrs/List/Requests/PersonListRequest.cs` |
| **Validation pipeline** | Setter | `Features/Person/Validators/PersonValidator.cs` |

### Data Access

| Паттерн | Где показан | Файл |
|---------|-------------|------|
| **Repository** | Getter/Setter | `IRepository<Person>` через Shared |
| **Specification** | Getter | `Specifications/PersonSpecification.cs` |
| **Entity Configuration** | Common | `Configuration/EntityConfigurations.cs` |
| **Unit of Work** | Setter | Автоматически через `RepositoryBase` |

### Mapping

| Паттерн | Где показан | Файл |
|---------|-------------|------|
| **AutoMapper Profile** | Common | `src/Services/Common/Template.Infrastructure.Mapping/MapperProfile.cs` |
| **DTO ↔ Entity** | Все сервисы | `CreateMap<Person, PersonDto>()` |

### HTTP Client

| Паттерн | Где показан | Файл |
|---------|-------------|------|
| **Typed HttpClient** | BFF | `HttpClients/GetterClient.cs`, `SetterClient.cs` |
| **Interface-based** | BFF | `Interfaces/HttpClients/IGetterClient.cs` |
| **AdditionalData flow** | BFF | Передача метаданных между сервисами |

---

## 🚫 Что НЕ включено (для production добавить самостоятельно)

| Компонент | Почему не включён | Что добавить |
|-----------|-------------------|--------------|
| **Authentication/Authorization** | Упрощение демо | JWT, policy-based auth, IUserProvider |
| **Message Bus** | Синхронное взаимодействие | MassTransit, RabbitMQ, Outbox pattern |
| **Distributed Tracing** | Локальная разработка | OpenTelemetry, Jaeger |
| **Rate Limiting** | Нет нагрузки | ASP.NET Core Rate Limiting |
| **Health Checks** | Базовый пример | `AddHealthChecks()`, readiness/liveness |
| **Circuit Breaker** | Нет внешних зависимостей | Polly policies |
| **Caching** | Простые CRUD | `CacheService<T>`, Redis |
| **Multi-tenancy** | Single-tenant | Tenant resolver, schema-per-tenant |
| **Audit Logging** | Минимальный аудит | Полное логирование изменений |
| **API Versioning** | Одна версия | Microsoft.AspNetCore.Mvc.Versioning |

---

## 📋 Структура каждого сервиса

### BFF (Backend For Frontend) — 3 проекта

```
Bff/
├── Template.Bff.Api/                    # Web API
│   ├── Controllers/                     # PersonController (агрегация)
│   │   ├── Base/                        # BffControllerBase
│   │   └── PersonController.cs          # POST /person/list, /person/create
│   └── DependencyInjection/
│       └── DependencyInjector.cs        # Регистрация HTTP-клиентов
├── Template.Bff.Application/            # CQRS (запросы к другим сервисам)
│   ├── Features/Queries/Person/Cqrs/List/
│   │   ├── PersonListQuery.cs
│   │   ├── PersonListQueryHandler.cs    # выбирает GetPersonsPattern по UseCqrs
│   │   └── Requests/PersonListRequest.cs
│   ├── HttpClients/                     # GetterClient, SetterClient
│   ├── Interfaces/HttpClients/          # IGetterClient, ISetterClient
│   └── HttpClients/Enums/               # GetPersonsPattern (Services | Cqrs)
└── Template.Bff.Infrastructure/         # Маппинг, конфигурация
    ├── Mapping/
    └── DependencyInjection/
```

> **Важно:** проекта `Template.Bff.Abstractions` **не существует** — абстракции DTO лежат внутри `Template.Bff.Application`. Контракты, разделяемые с другими сервисами, находятся в `Template.Getter.Application.Abstractions` / `Template.Setter.Application.Abstractions` (см. ниже).

### Getter (Read Service) — 4 проекта

```
Getter/
├── Template.Getter.Api/                          # Web API
│   ├── Controllers/PersonsController.cs          # POST endpoints (services/list, cqrs/list)
│   └── DependencyInjection/
├── Template.Getter.Application/                  # CQRS Queries, Specifications
│   ├── Features/Person/Cqrs/List/
│   │   ├── PersonListQuery.cs
│   │   ├── PersonReadListQueryHandler.cs
│   │   └── Validators/
│   ├── Specifications/PersonSpecification.cs
│   ├── Services/                                # PersonsService (для ветки Services)
│   ├── Interfaces/                              # IPersonsService
│   └── DependencyInjection/
├── Template.Getter.Application.Abstractions/     # DTO/Request/Response, Enums
│   ├── Enums/DalPattern.cs
│   └── Features/Person/List/
│       ├── Request/PersonListRequest.cs
│       ├── Request/PersonListFilter.cs
│       └── Response/PersonListResponse.cs
└── Template.Getter.Infrastructure/               # EF Core, конфигурации
    ├── Specifications/
    ├── Dal/Configuration/
    ├── Mapping/MapperProfile.cs
    └── DependencyInjection/
```

> **Важно:** `Template.Getter.Application.Abstractions` физически лежит в `Getter/Abstractions/Template.Getter.Application.Abstractions/` (отдельная корневая папка), а не в `Getter/Template.Getter.Application/Abstractions/`. Namespace при этом совпадает: `Template.Getter.Application.Abstractions`.

### Setter (Write Service) — 4 проекта

```
Setter/
├── Template.Setter.Api/                          # Web API
│   ├── Controllers/PersonsController.cs          # POST /persons/create
│   └── DependencyInjection/
├── Template.Setter.Application/                  # CQRS Commands + handlers + Lifecycle + Seeds
│   ├── Features/Person/Create/
│   │   ├── PersonCreateCommand.cs                # ICommand + CreateCommand<...>
│   │   └── PersonCreateCommandHandler.cs
│   ├── LifecycleAction/Person/                   # ILifecycleActionHandler<Person>
│   ├── Seeds/PersonSeed.cs                       # [Seed("person", 0)] : ISeed
│   ├── Validators/PersonValidator.cs
│   └── DependencyInjection/
├── Template.Setter.Application.Abstractions/     # Общие типы DTO/Request/Response
│   └── Features/Person/
│       ├── Common/Dto/                           # (пустая папка — зарезервировано)
│       ├── Create/Request/PersonCreateRequest.cs
│       └── Create/Response/PersonCreateResponse.cs
└── Template.Setter.Infrastructure/               # Mapping, persistence
    ├── Mapping/MapperProfile.cs
    ├── Dal/Configuration/PersonConfigurations.cs
    └── DependencyInjection/
```

> **Примечание:** в отличие от Getter, `Features/Person/Create/` (с прописной/строчной `Request/` / `Response/`) находится **внутри** `Template.Setter.Application`, а в `Abstractions/.../Create/` — `Request/`, `Response/` (синглтон-папки). Папки `Requests/`, `Responses/` (множественное число) **не существуют** — опечатка в старой версии документации.

### Common (Shared between services) — 6 проектов

| Проект | Назначение |
|--------|-----------|
| `Template.Domain` | Сущности (Person) |
| `Template.Application` | Общие сервисы, behaviors |
| `Template.Infrastructure` | HTTP clients, внешние сервисы |
| `Template.Infrastructure.Dal` | EF Core конфигурации |
| `Template.Infrastructure.Mapping` | AutoMapper profiles (`src/Services/Common/Template.Infrastructure.Mapping/MapperProfile.cs`) |
| `Template.Presentation` | Общие DTO, Swagger config |

> **Важно:** `MapperProfile.cs` находится именно в `src/Services/Common/Template.Infrastructure.Mapping/MapperProfile.cs`, **не** в `Common/MapperProfile.cs` (последний путь в старой версии документации был неточным).

---

## 🔗 См. также

- [Service Creation Guide](service-creation-guide.md) — пошаговое создание нового сервиса
- [CQRS](cqrs.md) — паттерн команд и запросов
- [Repository](repository.md) — доступ к данным
- [Api Client](api-client.md) — HTTP-клиенты с валидацией
- [Mapping](mapping.md) — AutoMapper интеграция
- [Service Startup](service-startup.md) — bootstrap-флоу
- [Filtering & Sorting Guide](filtering-sorting-guide.md) — BFF routing Services vs Cqrs
