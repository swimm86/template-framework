# Common Extensions & Helpers

## Обзор

**Assembly:** `Shared.Common.dll`  
**Namespaces:** `Shared.Common.Extensions`, `Shared.Common.Helpers`, `Shared.Common.JsonConverters`

Утилиты общего назначения: расширения для LINQ, Expression, String, Enum, Type, IQueryable, а также хелперы для пагинации, дат, JSON и булевых значений. Используются повсеместно во всех слоях фреймворка.

---

## LINQ Extensions

**Класс:** `LinqExtension` (`Shared.Common.Extensions`)

| Метод | Сигнатура | Описание |
|-------|-----------|----------|
| `ForEach<T>` | `void ForEach<T>(this IEnumerable<T>, Action<T>)` | Синхронный обход коллекции с выполнением действия |
| `ForeachAsync<T>` | `Task ForeachAsync<T>(this IEnumerable<T>, Func<T, Task>, CancellationToken)` | Асинхронный обход `IEnumerable<T>` |
| `ForEachAsync<T>` | `Task ForEachAsync<T>(this IAsyncEnumerable<T>, Func<T, Task>, CancellationToken)` | Асинхронный обход `IAsyncEnumerable<T>` |

### Примеры

```csharp
// Синхронный ForEach
items.ForEach(item => Console.WriteLine(item.Name));

// Асинхронный ForEach для IEnumerable
await items.ForeachAsync(
    async item => await _repository.UpdateAsync(item),
    cancellationToken);

// Асинхронный ForEach для IAsyncEnumerable
await asyncItems.ForEachAsync(
    async item => await ProcessAsync(item),
    cancellationToken);
```

Все методы проверяют `CancellationToken.ThrowIfCancellationRequested()` перед каждой итерацией.

---

## Expression Extensions

**Класс:** `ExpressionExtensions` (`Shared.Common.Extensions`)

| Метод | Сигнатура | Описание |
|-------|-----------|----------|
| `GetPropertyName` | `string GetPropertyName<TPrev, TProp>(this Expression<Func<TPrev, TProp>>)` | Извлекает имя свойства из лямбда-выражения |
| `And` | `Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>>, Expression<Func<T, bool>>)` | Комбинирует предикаты через `&&` |
| `Or` | `Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>>, Expression<Func<T, bool>>)` | Комбинирует предикаты через `\|\|` |
| `GetPropertyAccessAndType` | `(Expression? AccessExpr, Type? PropertyType) GetPropertyAccessAndType<T>(this ParameterExpression, string path)` | Строит выражение доступа к свойству по строковому пути (поддерживает вложенные свойства через `.`) |

### ParameterReplacer

Внутренний `ExpressionVisitor`, который заменяет параметр одного выражения на параметр другого. Используется в `And`/`Or` для объединения выражений с разными параметрами.

### Примеры

```csharp
// Извлечение имени свойства
Expression<Func<User, string>> expr = u => u.Email;
var name = expr.GetPropertyName(); // "Email"

// Комбинирование предикатов
Expression<Func<User, bool>> isActive = u => u.IsActive;
Expression<Func<User, bool>> isNotDeleted = u => !u.IsDeleted;
var combined = isActive.And(isNotDeleted);
// u => u.IsActive && !u.IsDeleted

var either = isActive.Or(isNotDeleted);
// u => u.IsActive || !u.IsDeleted

// Глубокий доступ к свойству
var param = Expression.Parameter(typeof(Order), "x");
var (access, type) = param.GetPropertyAccessAndType<Order>("Customer.Address.City");
// access: x.Customer.Address.City
// type: typeof(string)
```

---

## String Extensions

**Класс:** `StringExtensions` (`Shared.Common.Extensions`)

| Метод | Описание | Пример |
|-------|----------|--------|
| `ToKebabCase` | Конвертация в kebab-case | `"UserName"` → `"user-name"` |
| `ToSnakeCase` | Конвертация в snake_case | `"UserName"` → `"user_name"` |
| `ToCamelCase` | Конвертация в camelCase | `"UserName"` → `"userName"` |
| `ToUpperFirstChar` | Первая буква заглавная | `"userName"` → `"UserName"` |
| `RemoveWhiteSpaces` | Удаление всех пробелов | `"a b c"` → `"abc"` |

Используют встроенные `JsonNamingPolicy` для kebab/snake/camel case.

---

## Enum Extensions

**Класс:** `EnumExtensions` (`Shared.Common.Extensions`)

| Метод | Описание |
|-------|----------|
| `Description(this Enum)` | Возвращает `DescriptionAttribute` значения enum |
| `GetEnumValueByDescription<TEnum>(string?, TEnum?)` | Получает enum по описанию (case-insensitive) |
| `GetEnumValueByDescription(string?, Type, Enum?)` | То же, с указанием типа через `Type` |
| `GetEnumValueByPartOfDescription(string?, Type)` | Поиск enum по частичному совпадению описания |
| `With(this Enum, Enum)` | Объединение флагов (bitwise OR) |
| `Without(this Enum, Enum)` | Исключение флагов (bitwise AND NOT) |

### Пример

```csharp
public enum OrderStatus
{
    [Description("В обработке")]
    Pending,
    [Description("Выполнен")]
    Completed
}

var desc = OrderStatus.Pending.Description(); // "В обработке"
var status = "Выполнен".GetEnumValueByDescription<OrderStatus>(); // OrderStatus.Completed

// Flag enum
var flags = OrderFlags.Read.With(OrderFlags.Write);
var readOnly = flags.Without(OrderFlags.Write);
```

---

## Type Extensions

**Класс:** `TypeExtensions` (`Shared.Common.Extensions`)

| Метод | Описание |
|-------|----------|
| `ImplementsIEnumerable(this Type)` | Проверяет, реализует ли тип `IEnumerable` или `IEnumerable<T>` (рекурсивно, включая массивы и nullable) |
| `GetPropertyIgnoreCase(this Type, string)` | Получает `PropertyInfo` без учёта регистра |

---

## Queryable Extensions

**Класс:** `QueryableExtensions` (`Shared.Common.Extensions`)

| Метод | Описание |
|-------|----------|
| `GetRange<TEntity>(IQueryable<TEntity>, int? skip, int? take)` | Применяет `Skip`/`Take` к `IQueryable`. Параметры опциональны — если не указаны, соответствующий оператор не применяется |

---

## Helpers

| Хелпер | Namespace | Методы | Использование |
|--------|-----------|--------|---------------|
| **PaginationHelper** | `Shared.Common.Helpers` | `GetTotalPages(int?, int?)` — расчёт количества страниц<br>`CalculatePagination(int?, int?)` — расчёт `(skip, take)` | Пагинация в CQRS queries и контроллерах |
| **BoolHelper** | `Shared.Common.Helpers` | `GetBooleanValueByString(string, bool strong)` — парсинг "да"/"нет" в `bool?` | Импорт данных из пользовательского ввода |
| **DateParserHelper** | `Shared.Common.Helpers` | `TryParseDateOnlyIgnoringTime(string?)` — парсинг даты в `DateOnly`<br>`TryParseDateTime(string?)` — универсальный парсинг `DateTime` (поддержка 20+ форматов, включая OADate) | Парсинг дат из строк пользовательского ввода |
| **ExpressionHelper** | `Shared.Common.Helpers` | `GetPropExpression<TObj>(string propertyName, char delimiter)` — строит `Expression<Func<TObj, object>>` по строковому пути свойства | Динамический доступ к свойствам |
| **JsonHelper** | `Shared.Common.Helpers` | `TryDeserialize<T>(string, out T?, JsonSerializerOptions?)` — безопасная десериализация без исключений | Работа с JSON без try/catch |
| **HashHelper** | `Shared.Common.Helpers` | `ComputeSha256(params string?[] parts)` — детерминированный SHA-256 хэш 32 байта для набора строк (нормализация через `Trim` + `ToLowerInvariant`, разделитель `\|`) | Дедупликация сущностей (например, хэш персоны по `(Name, Email)`); **не предназначен** для хранения паролей |

### PaginationHelper — пример

```csharp
// Расчёт skip/take из номера страницы
var (skip, take) = PaginationHelper.CalculatePagination(pageNumber: 2, pageSize: 20);
// skip = 20, take = 20

// Расчёт общего количества страниц
var totalPages = PaginationHelper.GetTotalPages(totalCount: 100, pageSize: 20);
// totalPages = 5
```

### DateParserHelper — пример

```csharp
// Поддерживаемые форматы: dd.MM.yyyy, MM/dd/yyyy, yyyy-MM-dd, и др.
var date = DateParserHelper.TryParseDateOnlyIgnoringTime("15.03.2024");
// DateOnly(2024, 3, 15)

var dt = DateParserHelper.TryParseDateTime("2024-03-15 14:30:00.000");
// DateTime(2024, 3, 15, 14, 30, 0)
```

### HashHelper — пример

```csharp
// Дедупликация сущности: одинаковые (Name, Email) дают идентичный хэш
var hash1 = HashHelper.ComputeSha256("Иванов", "ivanov@example.com");
var hash2 = HashHelper.ComputeSha256("  иванов  ", "IVANOV@example.com");
// hash1 и hash2 идентичны (Trim + ToLowerInvariant)

// Добавление в Hash свойства индекса с уникальностью
builder.HasIndex(x => x.Hash).IsUnique(unique: true);
```

**Гарантии:**

- Возвращает `byte[32]` (256 бит) — фиксированная длина, удобная для индексов.
- Детерминированность: одинаковый набор нормализованных компонентов → идентичный массив байт.
- Разделитель `|` (вертикальная черта) предотвращает коллизии вида `"a|b"+""` vs `"a"+"|b"`.
- **Не использовать для криптографической защиты** (паролей, токенов) — для этого применять медленные солёные алгоритмы (bcrypt, Argon2).

---

## JSON Converters

**Namespace:** `Shared.Common.JsonConverters`

| Конвертер | Тип | Формат |
|-----------|-----|--------|
| `DateOnlyConverter` | `JsonConverter<DateOnly>` | `dd.MM.yyyy` |
| `NullableDateOnlyConverter` | `JsonConverter<DateOnly?>` | `dd.MM.yyyy` (или `null`) |

### Регистрация

```csharp
var options = new JsonSerializerOptions();
options.Converters.Add(new DateOnlyConverter());
options.Converters.Add(new NullableDateOnlyConverter());
```

### Пример

```csharp
// Сериализация
var json = JsonSerializer.Serialize(DateOnly.FromDateTime(new(2024, 3, 15)), options);
// "15.03.2024"

// Десериализация
var date = JsonSerializer.Deserialize<DateOnly>("\"15.03.2024\"", options);
// DateOnly(2024, 3, 15)
```

---

### Константы форматов

`FormatsConstants` централизует строковые форматы дат:

| Константа | Значение | Описание |
|-----------|----------|----------|
| `DateOnlyFormat` | `"dd.MM.yyyy"` | Формат для DateOnly |
| `DateTimeFormat` | `"dd.MM.yyyy HH:mm"` | Формат для DateTime |

Используется `DateOnlyConverter`, `DefaultObjectToStringConverter` и другими компонентами фреймворка.

---

## См. также

| Документ | Описание |
|----------|----------|
| [Filtering & Sorting Guide](filtering-sorting-guide.md) | Использование QueryableExtensions.GetRange для пагинации |
| [Specification Pattern](specification.md) | Комбинирование предикатов через ExpressionExtensions.And/Or |
| [Batch Helper](batch-helper.md) | Вспомогательные утилиты для батч-операций |
