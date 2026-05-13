# Shared — Unified .NET Application Framework

**Shared** — это унифицированный фреймворк для создания .NET приложений с готовой архитектурой, реализующий лучшие практики enterprise-разработки.

## 📋 Содержание

- [О проекте](#о-проекте)
- [Архитектурные паттерны](#архитектурные-паттерны)
- [Структура репозитория](#структура-репозитория)
- [Компоненты Shared](#компоненты-shared)
- [Services — примеры использования](#services--примеры-использования)
- [Начало работы](#начало-работы)
- [Документация](#документация)

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
| **Unit of Work** | Координация транзакций и доменных событий | [docs/unit-of-work.md](docs/unit-of-work.md) |
| **Specification** | Инкапсуляция бизнес-критериев выборки | [docs/specification.md](docs/specification.md) |
| **CQRS** | Разделение операций чтения и записи | См. ниже |
| **Auto-Registration** | Автоматическая регистрация зависимостей | См. ниже |
| **Pipeline Behaviors** | Cross-cutting concerns через MediatR | См. ниже |
| **Exception Mapping** | Преобразование исключений в Problem Details | См. ниже |

### Краткий обзор паттернов

#### CQRS Pattern
Разделение операций чтения и записи через MediatR:

- `ICommand<TResponse>` / `IQuery<TResponse>` — команды и запросы
- `ICommandHandler<>` / `IQueryHandler<>` — обработчики
- Готовые обработчики: Create, Update, Delete, Clone, Read, ReadList

#### Auto-Registration Pattern
Автоматическая регистрация зависимостей через reflection:

```csharp
services.RegisterDerivedTypeDependencies<IValidator>(
    serviceTypeAsInterface: true,
    lifetime: ServiceLifetime.Scoped);
```

#### Pipeline Behaviors
Cross-cutting concerns: Logging → Validation → Handler

#### Exception Mapping
Типобезопасное преобразование исключений в Problem Details (RFC 7807)

---

## 🗂 Структура репозитория

```
src/
├── Shared/                          # Унифицированный фреймворк
│   ├── Core/                        # Базовые компоненты ядра
│   │   ├── Shared.Common            # Общие утилиты и расширения
│   │   ├── Shared.Domain.Core       # Базовые доменные модели
│   │   ├── Shared.Application.Core  # Базовые сервисы приложения
│   │   ├── Shared.Application.Cqrs.Core  # CQRS инфраструктура
│   │   ├── Shared.Infrastructure.Core    # Базовая инфраструктура
│   │   └── Shared.Presentation.Core      # Базовые компоненты presentation-слоя
│   ├── Dal/                         # Слой доступа к данным
│   │   ├── Shared.Infrastructure.Dal.EFCore
│   │   └── Shared.Infrastructure.Dal.EFCore.Postgres
│   ├── Logging/                     # Логирование
│   ├── Mapper/                      # Маппинг (AutoMapper)
│   ├── Job/                         # Фоновые задачи (Quartz)
│   └── Utils/                       # Утилиты
│
├── Services/                        # Примеры микросервисов
│   ├── Bff/                         # BFF (Backend For Frontend)
│   ├── Getter/                      # Сервис получения данных
│   ├── Setter/                      # Сервис записи данных
│   ├── Common/                      # Общие компоненты сервисов
│   └── DatabaseUpgrade/             # Миграции БД
│
├── Tests/                           # Тесты
└── Template.sln                     # Решение Visual Studio
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
| `Shared.Utils.DatabaseUpgrade` | Утилиты для миграции БД |

---

## 🚀 Services — примеры использования

В директории **Services** представлены примеры реализации микросервисов на основе Shared:

### Архитектура микросервисов

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  BFF API    │────▶│  Getter API │────▶│  Setter API │
│  (Frontend) │     │   (Read)    │     │  (Write)    │
└─────────────┘     └─────────────┘     └─────────────┘
```

### Сервисы

| Сервис | Назначение |
|--------|------------|
| **Bff** (Backend For Frontend) | Агрегация данных от других сервисов, адаптация под нужды фронтенда |
| **Getter** | Сервис чтения данных (CQRS Query side) |
| **Setter** | Сервис записи данных (CQRS Command side) |

> **Примечание:** Services — это примеры для разработчиков, демонстрирующие best practices использования Shared в микросервисной архитектуре.

### Структура каждого сервиса

Каждый микросервис следует единой структуре:

```
Service/
├── Api/                  # Web API слой (Controllers, Middleware)
├── Application/          # Бизнес-логика (Use Cases, Handlers)
├── Infrastructure/       # Инфраструктурные реализации
└── Abstractions/         # Интерфейсы и контракты (опционально)
```

### Common компоненты

В `Services/Common` находятся переиспользуемые компоненты:
- `Template.Domain` — доменные модели
- `Template.Application` — общие сервисы приложения
- `Template.Infrastructure` — общая инфраструктура
- `Template.Infrastructure.Dal` — доступ к данным
- `Template.Infrastructure.Mapping` — конфигурация маппинга
- `Template.Presentation` — общие DTO и презентационные компоненты

---

## 🛠 Начало работы

### Требования

- .NET 8+ (или актуальная версия из `Directory.Build.props`)
- Visual Studio 2022 / JetBrains Rider
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

Приоритет конфигурации:
1. Переменные окружения OS
2. `.env.{EnvironmentName}` (например, `.env.Development`)
3. `.env` (базовый файл)
4. `appsettings.json`

### Миграции БД

Для применения миграций используйте проект `Template.DatabaseUpgrade`:

```bash
dotnet run --project Services/DatabaseUpgrade/Template.DatabaseUpgrade
```

---

## 📚 Документация

Дополнительная документация доступна в директории [`docs/`](docs/):

### Паттерны
- [Repository Pattern](docs/repository.md) — доступ к данным через репозиторий
- [Unit of Work Pattern](docs/unit-of-work.md) — координация транзакций
- [Specification Pattern](docs/specification.md) — инкапсуляция критериев выборки

### Руководства
- [Batch Helper](docs/batch-helper.md) — пакетная обработка данных
- [Batch Request](docs/batch-request.md) — массовые запросы к API
- [Filtering & Sorting](docs/filtering-sorting-guide.md) — фильтрация и сортировка
- [Logging](docs/logging.md) — руководство по логированию

---

## 🤝 Для разработчиков

Этот репозиторий предназначен в первую очередь для внутренней разработки и служит:

1. **Шаблоном** для создания новых микросервисов
2. **Библиотекой** готовых компонентов (Shared)
3. **Примером** правильной архитектуры (Services)

### Создание нового сервиса

1. Скопируйте структуру одного из существующих сервисов (Getter/Setter)
2. Обновите namespace'ы и имена проектов
3. Добавьте ссылки на необходимые компоненты Shared
4. Реализуйте бизнес-логику в слое Application
5. Настройте инфраструктуру (БД, внешние сервисы)
6. Создайте `.env` файл для конфигурации

---

## 📄 Лицензия

[Укажите лицензию вашего проекта]

---

## 📞 Поддержка

По вопросам обращайтесь к команде разработки или создавайте Issues в репозитории.