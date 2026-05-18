# Гайд по использованию механизмов фильтрации и сортировки

## Обзор

В проекте реализованы унифицированные механизмы фильтрации и сортировки, которые позволяют создавать гибкие запросы с произвольными наборами параметров. Основные компоненты:

- **`ListFilterBase`** - базовый record для фильтров с идентификаторами
- **`PageableRequest`** / **`PageableRequest<TFilter>`** - базовые классы для запросов с пагинацией и сортировкой
- **`SortOption`** - модель для описания условий сортировки
- **`SpecificationBase<TEntity>`** - базовый класс для инкапсуляции критериев фильтрации и сортировки
- **`QueryOptions<TEntity>`** - конфигурация запроса (фильтры, сортировки, includes)

## Архитектура

### Namespace / Assembly

| Компонент | Namespace |
|-----------|-----------|
| `ListFilterBase` | `Shared.Application.Cqrs.Core.Abstractions` |
| `IWithIdsFilter<TKey>` | `Shared.Application.Core.Dto.Interfaces` |
| `PageableRequest` / `PageableRequest<TFilter>` | `Shared.Application.Core.Dto.Requests` |
| `SortOption` | `Shared.Domain.Core.Dal.Models` |
| `OrderDirectionType` | `Shared.Domain.Core.Dal` |
| `QueryOptions<TEntity>` | `Shared.Domain.Core.Dal.Repository.Models` |
| `SpecificationBase<TEntity>` | `Shared.Domain.Core.Dal.Specification.Models` |

### Базовые классы

#### ListFilterBase

```csharp
public abstract record ListFilterBase : IWithIdsFilter<Guid>
{
    public ICollection<Guid>? Ids { get; init; }
}
```

`ListFilterBase` — абстрактный record, реализующий интерфейс `IWithIdsFilter<Guid>`. Используется в `ReadListQueryHandler.ConstructOptions()` для автоматической фильтрации по идентификаторам. Если фильтр наследуется от `ListFilterBase` и содержит `Ids`, handler автоматически добавляет фильтр `x => ids.Any(id => id.Equals(x.Id))`.

#### IWithIdsFilter<TKey>

```csharp
public interface IWithIdsFilter<TKey>
{
    public ICollection<TKey>? Ids { get; init; }
}
```

#### PageableRequest

```csharp
public abstract record PageableRequest
{
    public const char ValueDelimiter = '.';

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = Constants.DefaultBatchSize;
    public List<string>? SortOptions { get; init; }

    public ICollection<SortOption> ConvertSortOptions() { /* ... */ }
}

public abstract record PageableRequest<TFilter> : PageableRequest
    where TFilter : new()
{
    public TFilter? Filter { get; init; } = new();
}
```

### Модели данных

#### SortOption

```csharp
public class SortOption(string key, OrderDirectionType directionType)
{
    public string Key { get; } = key;
    public OrderDirectionType DirectionType { get; } = directionType;
}
```

`Key` — имя свойства сущности (регистронезависимое). `SortOption` автоматически маппится на выражение сортировки через reflection в `QueryOptions.AddOrderBy(SortOption)`.

#### OrderDirectionType

```csharp
public enum OrderDirectionType
{
    [Description("asc")]
    Ascending,

    [Description("desc")]
    Descending
}
```

## Направления сортировки

| Направление | Описание | Строковое представление (Description) |
|-------------|----------|--------------------------------------|
| `Ascending` | По возрастанию | `"asc"` |
| `Descending` | По убыванию | `"desc"` |

## Создание запросов

### 1. Создание фильтра

Фильтры — это POCO-классы с произвольными свойствами для фильтрации. Они **не** наследуют универсальный базовый класс с операциями (такого механизма нет). Вместо этого фактическая логика фильтрации реализуется в спецификации через `QueryOptions.AddFilter()`.

#### Простой фильтр (без наследования)

```csharp
public class PersonListFilter
{
    public string? Name { get; set; }
    public string? NameContains { get; set; }
    public string? Email { get; set; }
    public string? EmailContains { get; set; }
}
```

#### Фильтр с идентификаторами (наследование от ListFilterBase)

Если фильтру нужна возможность фильтрации по `Ids`, унаследуйте от `ListFilterBase`:

```csharp
public record PersonListFilter : ListFilterBase
{
    public string? Name { get; init; }
    public string? Email { get; init; }
}
```

В этом случае `ReadListQueryHandler` автоматически добавит фильтр по `Ids`, если они переданы.

### 2. Создание спецификации

Фильтрация реализуется в спецификации через `QueryOptions<TEntity>.AddFilter()` с lambda-выражениями:

```csharp
public record PersonSpecification(PersonListRequest Request)
    : SpecificationBase<Person>(Request.ConvertSortOptions())
{
    public override QueryOptions<Person> BuildOptions()
    {
        base.BuildOptions();
        if (!string.IsNullOrWhiteSpace(Request.Filter?.Email))
        {
            Options.AddFilter(x => x.Email.ToLower().Equals(Request.Filter.Email.ToLower()));
        }

        return Options;
    }
}
```

См. подробнее в [Specification Pattern](specification.md).

### 3. Создание запроса с пагинацией

```csharp
public record PersonListRequest(DalPattern DalPattern)
    : PageableRequest<PersonListFilter>;

// Использование
var request = new PersonListRequest(DalPattern.Specification)
{
    PageNumber = 1,
    PageSize = 20,
    SortOptions = new List<string>
    {
        "name.asc",
        "email.desc"
    },
    Filter = new PersonListFilter
    {
        Name = "Иван",
        Email = "example.com"
    }
};
```

## Формат сортировки

Сортировка задаётся в формате: `"поле.направление"`, где разделитель — символ `.` (точка), определённый в `PageableRequest.ValueDelimiter`.

### Примеры:
- `"name.asc"` — сортировка по имени по возрастанию
- `"email.desc"` — сортировка по email по убыванию
- `"createdDate.asc"` — сортировка по дате создания по возрастанию

### Множественная сортировка:
```csharp
SortOptions = new List<string>
{
    "name.asc",      // Сначала по имени (A-Z)
    "email.desc",    // Затем по email (Z-A)
    "age.asc"        // Затем по возрасту (младшие сначала)
}
```

### Как работает ConvertSortOptions

Метод `PageableRequest.ConvertSortOptions()` разбирает строки `"key.direction"` разделённые точкой, при этом ключ может содержать точки, а направление — это последнее значение. Направление маппится через `Description`-атрибут enum'а `OrderDirectionType` (`"asc"` → `Ascending`, `"desc"` → `Descending`). При некорректном направлении выбрасывается `ValidationException`.

## Реализация в контроллерах

### Пример: Getter, два входа для одного ресурса Person

Базовый шаблон маршрута см. в `Shared.Presentation.Core` (`api/[appName]/[controllerType]/v1/[controller]`).
Для `PersonsController` в Getter заданы два POST с **одинаковым телом** `PersonListRequest`:

| Относительный путь | Обработка |
|--------------------|-----------|
| `persons/services/list` | Слой приложения (`IPersonsService.GetPersonsAsync`) |
| `persons/cqrs/list` | CQRS через MediatR (`PersonReadListQuery`) |

BFF выбирает ветку через перечисление `GetPersonsPattern` при вызове `IGetterClient.GetPersonsAsync`.

```csharp
[HttpPost("services/list")]
public Task<IActionResult> GetPersonsByServicesAsync(
    [FromBody] PersonListRequest request,
    CancellationToken cancellationToken = default) =>
    Process(() => personsService.GetPersonsAsync(request, cancellationToken));

[HttpPost("cqrs/list")]
public Task<IActionResult> GetPersonsByCqrsAsync(
    [FromBody] PersonListRequest request,
    CancellationToken cancellationToken = default) =>
    Process(() => sender.Send(new PersonReadListQuery(request), cancellationToken));
```

## Примеры JSON запросов

### 1. Простой запрос с пагинацией
```json
{
  "pageNumber": 1,
  "pageSize": 10,
  "sortOptions": ["name.asc"],
  "filter": {
    "name": "Иван"
  }
}
```

### 2. Запрос с фильтрацией по email
```json
{
  "pageNumber": 1,
  "pageSize": 20,
  "sortOptions": ["email.asc", "name.desc"],
  "filter": {
    "email": "gmail.com"
  }
}
```

### 3. Запрос с фильтрацией по идентификаторам (ListFilterBase)
```json
{
  "pageNumber": 1,
  "pageSize": 50,
  "sortOptions": ["name.asc"],
  "filter": {
    "ids": ["3fa85f64-5717-4562-b3fc-2c963f66afa6", "7c9e6679-7425-40de-944b-4d8b6c5e2f1a"],
    "name": "Иван"
  }
}
```

### 4. Множественная сортировка без фильтра
```json
{
  "pageNumber": 2,
  "pageSize": 15,
  "sortOptions": ["createdDate.desc", "name.asc"]
}
```

## Спецификации

Для реализации логики фильтрации используйте спецификации, наследуясь от `SpecificationBase<TEntity>` и вызывая `AddFilter()` с lambda-выражениями:

```csharp
public record PersonSpecification(PersonListRequest Request)
    : SpecificationBase<Person>(Request.ConvertSortOptions())
{
    public override QueryOptions<Person> BuildOptions()
    {
        base.BuildOptions();
        if (!string.IsNullOrWhiteSpace(Request.Filter?.Email))
        {
            Options.AddFilter(x => x.Email.ToLower().Equals(Request.Filter.Email.ToLower()));
        }

        return Options;
    }
}
```

См. подробнее в [Specification Pattern](specification.md).

## CQRS Handler

Для CQRS-паттерна используйте базовый `ReadListQueryHandler`:

```csharp
public class PersonReadListQueryHandler(
    ILoggerFactory loggerFactory,
    IUnitOfWork unitOfWork)
    : ReadListQueryHandler<PersonReadListQuery, PersonListRequest, PersonListFilter, PersonListResponse, PersonListPayload, Person>(
        loggerFactory,
        unitOfWork)
{
    protected override QueryOptions<Person> ConstructOptions(PersonReadListQuery query)
    {
        var specification = new PersonSpecification(query.Request);
        var options = specification.BuildOptions();
        ApplySortOptions(query.Request.ConvertSortOptions(), options);
        return options;
    }
}
```

### Автоматическая фильтрация по Ids

Если `TFilter` наследуется от `ListFilterBase`, базовый `ReadListQueryHandler.ConstructOptions()` автоматически добавит фильтр по `Ids`:

```csharp
protected override QueryOptions<TEntity> ConstructOptions(TQuery request)
{
    var options = base.ConstructOptions(request);
    // base автоматически проверяет: if (request.Filter is ListFilterBase baseFilter && baseFilter.Ids?.Any())
    // и добавляет: options.AddFilter(x => baseFilter.Ids.Any(id => id.Equals(x.Id)));
    return options;
}
```

## QueryOptions<TEntity> - прямое использование

`QueryOptions<TEntity>` предоставляет fluent API для формирования запросов:

```csharp
var options = new QueryOptions<User>()
    .AddFilter(u => u.IsActive)
    .AddFilter(u => u.Age >= 18)
    .AddOrderBy(u => u.Name, OrderDirectionType.Ascending);
```

### Основные методы

| Метод | Возвращает | Описание |
|-------|-----------|----------|
| `AddFilter(Expression)` | `QueryOptions<TEntity>` | Добавляет фильтр |
| `AddFilterIf(bool, Expression)` | `QueryOptions<TEntity>` | Добавляет фильтр при условии |
| `AddOrderBy(Expression, OrderDirectionType, int?)` | `QueryOptions<TEntity>` | Добавляет сортировку |
| `AddOrderBy(SortOption)` | `void` | Добавляет сортировку из SortOption (через reflection) |
| `AddOrderByIf(bool, Expression, OrderDirectionType, int?)` | `QueryOptions<TEntity>` | Добавляет сортировку при условии |
| `AddInclude<TProperty>(Expression)` | `Includable<TEntity, TProperty>` | Добавляет навигационное свойство |

## Лучшие практики

### 1. Именование полей фильтра
- Используйте PascalCase для свойств фильтра (C# конвенция)
- JSON-сериализатор автоматически конвертирует в camelCase

### 2. Создание спецификаций
- Одна спецификация — одна логическая группа фильтров
- Используйте `AddFilter()` с lambda-выражениями вместо строковых имён полей
- Вызывайте `AddFilterIf()` для условных фильтров

### 3. Производительность
- Используйте индексы для часто фильтруемых полей
- Ограничивайте `PageSize` (значение по умолчанию: `Constants.DefaultBatchSize`)
- Для больших коллекций по `Ids` ensure уникальность идентификаторов

### 4. Безопасность
- Спецификации формируют `Expression<Func<TEntity, bool>>`, что предотвращает SQL-инъекции
- Валидируйте входные данные на уровне контроллера/validator'а

## Обработка ошибок

Система автоматически обрабатывает следующие ошибки:

- Некорректные имена полей в `SortOption` — свойство игнорируется, если не найдено в сущности
- Некорректное направление сортировки — выбрасывается `ValidationException`
- Для `bool`-свойств направление автоматически инвертируется при сортировке

Пример ошибки невалидного направления:
```json
{
  "error": "Invalid sort direction: 'invalid'"
}
```

## Расширение функциональности

### Добавление нового фильтра

1. Создайте класс фильтра (с наследованием от `ListFilterBase` при необходимости фильтрации по Ids)
2. Создайте спецификацию с `AddFilter()` для каждого свойства фильтра
3. Свяжите через `PageableRequest<TFilter>`

### Добавление кастомной сортировки

1. Создайте спецификацию для сложной логики сортировки
2. Переопределите `BuildOptions()` в спецификации или `ApplySortOptions()` в handler'е

## Заключение

Механизмы фильтрации и сортировки строятся на трёх ключевых абстракциях:

1. **`PageableRequest<TFilter>`** — входная модель запроса с пагинацией, сортировкой (`SortOptions`) и типизированным фильтром
2. **`SpecificationBase<TEntity>`** — инкапсуляция критериев фильтрации и сортировки через `QueryOptions<TEntity>.AddFilter()` / `AddOrderBy()`
3. **`QueryOptions<TEntity>`** — итоговая конфигурация запроса, передаваемая в репозиторий

Фильтрация реализуется lambda-выражениями в спецификации, а не через декларативные `FilterOption`/`FilterOperationType` — такой подход обеспечивает типобезопасность и защиту от SQL-инъекций.

---

## См. также

| Документ | Описание |
|----------|----------|
| [Repository Pattern](repository.md) | Доступ к данным через репозиторий |
| [Specification Pattern](specification.md) | Инкапсуляция критериев выборки |
| [Batch Request](batch-request.md) | Постраничные HTTP-запросы и батчи |
| [CQRS](cqrs.md) | Разделение команд и запросов |