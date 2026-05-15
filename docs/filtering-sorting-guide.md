# Гайд по использованию механизмов фильтрации и сортировки

## Обзор

В проекте реализованы унифицированные механизмы фильтрации и сортировки, которые позволяют создавать гибкие запросы с произвольными наборами параметров. Основные компоненты:

- **`FilterBase`** - базовый класс для фильтров
- **`PageableRequest`** - базовый класс для запросов с пагинацией и сортировкой
- **`FilterOption`** - модель для описания условий фильтрации
- **`SortOption`** - модель для описания условий сортировки

## Архитектура

### Базовые классы

#### FilterBase
```csharp
public record FilterBase
{
    public ICollection<FilterOption>? Fields { get; init; } = [];
}
```

#### PageableRequest
```csharp
public abstract record PageableRequest
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
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

#### FilterOption
```csharp
public record FilterOption
{
    public required string FieldName { get; init; }
    public required FilterOperationType OperationType { get; init; }
    public required object? Value { get; init; }
}
```

#### SortOption
```csharp
public record SortOption(string Key, OrderDirectionType DirectionType);
```

## Типы операций фильтрации

Доступные операции фильтрации (`FilterOperationType`):

| Операция | Описание | Пример использования |
|----------|----------|---------------------|
| `Equals` | Равно | `FieldName: "email", Value: "test@example.com"` |
| `NotEquals` | Не равно | `FieldName: "status", Value: "inactive"` |
| `GreaterThan` | Больше чем | `FieldName: "age", Value: 18` |
| `GreaterThanOrEqual` | Больше или равно | `FieldName: "price", Value: 100.50` |
| `LessThan` | Меньше чем | `FieldName: "createdDate", Value: "2024-01-01"` |
| `LessThanOrEqual` | Меньше или равно | `FieldName: "quantity", Value: 10` |
| `Contains` | Содержит (для строк) | `FieldName: "name", Value: "john"` |
| `StartsWith` | Начинается с | `FieldName: "title", Value: "admin"` |
| `EndsWith` | Заканчивается на | `FieldName: "filename", Value: ".pdf"` |
| `In` | Входит в список | `FieldName: "category", Value: ["cat1", "cat2"]` |
| `IsNull` | Равно null | `FieldName: "deletedAt", Value: null` |
| `IsNotNull` | Не равно null | `FieldName: "updatedAt", Value: null` |

## Направления сортировки

Доступные направления сортировки (`OrderDirectionType`):

| Направление | Описание | Строковое представление |
|-------------|----------|------------------------|
| `Ascending` | По возрастанию | `"asc"` |
| `Descending` | По убыванию | `"desc"` |

## Создание запросов

### 1. Создание фильтра

#### Простой фильтр (наследование от FilterBase)
```csharp
public record PersonListFilter : FilterBase
{
    public string? Name { get; init; }
    public string? Email { get; set; }
}
```

#### Использование универсальных фильтров
```csharp
var filter = new PersonListFilter
{
    Fields = new List<FilterOption>
    {
        new() 
        { 
            FieldName = "email", 
            OperationType = FilterOperationType.Contains, 
            Value = "gmail.com" 
        },
        new() 
        { 
            FieldName = "age", 
            OperationType = FilterOperationType.GreaterThan, 
            Value = 18 
        },
        new() 
        { 
            FieldName = "status", 
            OperationType = FilterOperationType.In, 
            Value = new[] { "active", "pending" } 
        }
    }
};
```

### 2. Создание запроса с пагинацией

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
        Fields = new List<FilterOption>
        {
            new() 
            { 
                FieldName = "email", 
                OperationType = FilterOperationType.Contains, 
                Value = "example.com" 
            }
        }
    }
};
```

## Формат сортировки

Сортировка задается в формате: `"поле.направление"`

### Примеры:
- `"name.asc"` - сортировка по имени по возрастанию
- `"email.desc"` - сортировка по email по убыванию
- `"createdDate.asc"` - сортировка по дате создания по возрастанию

### Множественная сортировка:
```csharp
SortOptions = new List<string> 
{ 
    "name.asc",      // Сначала по имени (A-Z)
    "email.desc",    // Затем по email (Z-A)
    "age.asc"        // Затем по возрасту (младшие сначала)
}
```

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
    "fields": []
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
    "fields": [
      {
        "fieldName": "email",
        "operationType": "Contains",
        "value": "gmail.com"
      }
    ]
  }
}
```

### 3. Сложный запрос с множественными фильтрами
```json
{
  "pageNumber": 2,
  "pageSize": 15,
  "sortOptions": ["createdDate.desc", "name.asc"],
  "filter": {
    "fields": [
      {
        "fieldName": "age",
        "operationType": "GreaterThanOrEqual",
        "value": 18
      },
      {
        "fieldName": "status",
        "operationType": "In",
        "value": ["active", "pending"]
      },
      {
        "fieldName": "name",
        "operationType": "StartsWith",
        "value": "John"
      },
      {
        "fieldName": "deletedAt",
        "operationType": "IsNull",
        "value": null
      }
    ]
  }
}
```

## Спецификации

Для более сложной логики фильтрации используйте спецификации:

```csharp
public record PersonSpecification(
    PersonListRequest Request,
    QueryOptions<Person>? Options = default)
    : SpecificationBase<Person>(
        Options,
        Request.ConvertSortOptions(),
        Request.Filter?.Fields)
{
    public override QueryOptions<Person> BuildOptions()
    {
        // Дополнительная логика фильтрации
        if (!string.IsNullOrWhiteSpace(Request.Filter?.Email))
        {
            Options.AddFilter(x => x.Email.ToLower().Equals(Request.Filter.Email.ToLower()));
        }

        return Options;
    }
}
```

## CQRS Handler

Для CQRS паттерна используйте базовый `ReadListQueryHandler`:

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
        var options = base.ConstructOptions(query);
        var specification = new PersonSpecification(query.Request, options);
        specification.BuildOptions();
        ApplySortOptions(query.Request.ConvertSortOptions(), options);
        return options;
    }
}
```

## Лучшие практики

### 1. Именование полей
- Используйте точные имена свойств сущности
- Учитывайте регистр (обычно camelCase для JSON, PascalCase для C#)

### 2. Валидация
- Проверяйте корректность типов данных для операций
- Валидируйте существование полей в сущности

### 3. Производительность
- Используйте индексы для часто фильтруемых полей
- Ограничивайте количество элементов в операциях `In`

### 4. Безопасность
- Проверяйте права доступа к полям
- Валидируйте входные данные

## Обработка ошибок

Система автоматически обрабатывает следующие ошибки:

- Некорректные имена полей
- Неподдерживаемые типы операций
- Ошибки преобразования типов данных

Пример ошибки:
```json
{
  "error": "Некорректный фильтр: {\"fieldName\":\"invalidField\",\"operationType\":\"Contains\",\"value\":\"test\"}"
}
```

## Расширение функциональности

### Добавление новых операций фильтрации

1. Добавьте новое значение в `FilterOperationType`
2. Реализуйте логику в `QueryOptions<TEntity>.AddFilter()`
3. Добавьте соответствующие тесты

### Добавление кастомной сортировки

1. Создайте спецификацию для сложной логики сортировки
2. Переопределите метод `ApplySortOptions()` в handler'е

## Заключение

Данные механизмы обеспечивают гибкость и универсальность при работе с фильтрацией и сортировкой данных. Они позволяют создавать сложные запросы без изменения кода серверной части, что особенно полезно для API, используемых различными клиентами.

---

## См. также

| Документ | Описание |
|----------|----------|
| [Repository Pattern](repository.md) | Доступ к данным через репозиторий |
| [Specification Pattern](specification.md) | Инкапсуляция критериев выборки |
| [Batch Request](batch-request.md) | Постраничные HTTP-запросы и батчи |
| [CQRS](cqrs.md) | Разделение команд и запросов | 