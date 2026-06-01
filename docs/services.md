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
┌─────────────┐  HTTP GET   ┌─────────────┐  HTTP GET   ┌─────────────┐
│  BFF API    │────────────▶│  Getter API │────────────▶│  Setter API │
│  (Frontend) │◀────────────│   (Read)    │◀────────────│  (Write)    │
└─────────────┘  Response   └─────────────┘  Response   └─────────────┘
```

| Поток | Клиент | Метод | Описание |
|-------|--------|-------|----------|
| BFF → Getter | `IGetterClient` | `GET /api/persons` | Получение списка персон с фильтрацией |
| BFF → Setter | `ISetterClient` | `POST /api/persons` | Создание новой персоны |
| Getter → БД | EF Core Repository | `IRepository<Person>` | Чтение через Specification |
| Setter → БД | EF Core Repository | `IRepository<Person>` | Запись через Unit of Work |

---

## 📦 Доменная модель

Все сервисы работают с единой сущностью **Person**:

```csharp
// Services/Common/Template.Domain/Entities/Person.cs
public class Person : BaseEntity
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string MiddleName { get; set; }
    public DateTime BirthDate { get; set; }
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
| **Query + Handler** | Getter | `Features/Person/Cqrs/List/PersonListQuery.cs` |
| **Command + Handler** | Setter | `Features/Person/Create/PersonCreateCommand.cs` |
| **Request/Response models** | BFF | `Person/Cqrs/List/Requests/PersonListRequest.cs` |
| **Validation pipeline** | Setter | `Features/Person/Validators/PersonValidator.cs` |

### Data Access

| Паттерн | Где показан | Файл |
|---------|-------------|------|
| **Repository** | Getter/Setter | `IRepository<Person>` через Shared |
| **Specification** | Getter | `PersonSpecification` с фильтрацией |
| **Entity Configuration** | Common | `Configuration/EntityConfigurations.cs` |
| **Unit of Work** | Setter | Автоматически через `RepositoryBase` |

### Mapping

| Паттерн | Где показан | Файл |
|---------|-------------|------|
| **AutoMapper Profile** | Common | `MapperProfile.cs` |
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
│   └── DependencyInjection/
│       └── DependencyInjector.cs        # Регистрация HTTP-клиентов
├── Template.Bff.Application/            # CQRS (запросы к другим сервисам)
│   └── Features/Person/
│       ├── Cqrs/List/                   # PersonListQuery → GetterClient
│       └── Cqrs/Create/                 # PersonCreateCommand → SetterClient
└── Template.Bff.Abstractions/           # Интерфейсы контроллеров
```

### Getter (Read Service) — 4 проекта

```
Getter/
├── Template.Getter.Api/                 # Web API
│   └── Controllers/PersonsController.cs # GET endpoints
├── Template.Getter.Application/         # CQRS Queries
│   └── Features/Person/Cqrs/List/       # PersonListQuery + Handler
├── Template.Getter.Application.Abstractions/
│   └── Interfaces/IPersonsService.cs    # Контракты
└── Template.Getter.Infrastructure/      # Specifications, Repositories
    └── Specifications/PersonSpecification.cs
```

### Setter (Write Service) — 4 проекта

```
Setter/
├── Template.Setter.Api/                 # Web API
│   └── Controllers/PersonsController.cs # POST endpoints
├── Template.Setter.Application/         # CQRS Commands
│   └── Features/Person/
│       ├── Create/                      # PersonCreateCommand + Handler
│       └── Validators/                  # PersonValidator
├── Template.Setter.Application.Abstractions/
│   └── Commands/                        # Shared command types
└── Template.Setter.Infrastructure/      # Mapping, persistence
    └── Mapping/MapperProfile.cs
```

### Common (Shared between services) — 6 проектов

| Проект | Назначение |
|--------|-----------|
| `Template.Domain` | Сущности (Person) |
| `Template.Application` | Общие сервисы, behaviors |
| `Template.Infrastructure` | HTTP clients, внешние сервисы |
| `Template.Infrastructure.Dal` | EF Core конфигурации |
| `Template.Infrastructure.Mapping` | AutoMapper profiles |
| `Template.Presentation` | Общие DTO, Swagger config |

---

## 🔗 См. также

- [Service Creation Guide](service-creation-guide.md) — пошаговое создание нового сервиса
- [CQRS](cqrs.md) — паттерн команд и запросов
- [Repository](repository.md) — доступ к данным
- [Api Client](api-client.md) — HTTP-клиенты с валидацией
- [Mapping](mapping.md) — AutoMapper интеграция
- [Service Startup](service-startup.md) — bootstrap-флоу
