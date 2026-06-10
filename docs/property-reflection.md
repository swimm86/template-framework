# Property Reflection Utilities

## Обзор

**Assembly:** `Shared.Domain.Core.dll`  
**Namespaces:** `Shared.Domain.Core.Utils`, `Shared.Domain.Core.Utils.Interfaces`, `Shared.Domain.Core.Converters`, `Shared.Domain.Core.Converters.Interfaces`

Утилиты для доступа к свойствам объектов через **compiled expression trees** вместо обычной reflection. Используются в ApiClient для сериализации multipart form data и других сценариях, где требуется частый доступ к свойствам по имени.

---

## Зачем Compiled Expressions

Обычная reflection (`PropertyInfo.GetValue`/`SetValue`) медленная при многократном вызове. `PropertyUtil` компилирует выражения один раз и кэширует их в `ConcurrentDictionary`, что даёт производительность, близкую к прямому вызову.

```
Reflection (первый вызов)  → ~100-500 ns
Compiled expression (кэш)  → ~5-10 ns
Прямой вызов               → ~1-2 ns
```

---

## PropertyUtil

**Класс:** `PropertyUtil` (`Shared.Domain.Core.Utils`)  
**Реализует:** `IPropertyGetter`, `IPropertySetter`

### Кэширование

Использует два `ConcurrentDictionary<(Type, string), Delegate>`:

| Dictionary | Value type | Назначение |
|------------|------------|------------|
| `_propertyGetters` | `Func<object, object?>` | Чтение свойства |
| `_propertySetters` | `Action<object, object?>` | Запись свойства |

Ключ — кортеж `(Type, propertyName)`. Компиляция происходит один раз при первом обращении через `GetOrAdd`.

### Методы

| Метод | Сигнатура | Описание |
|-------|-----------|----------|
| `GetProperty` | `object? GetProperty(object?, string propertyName, bool throwIfNotFound = true)` | Читает значение свойства. Если свойство не найдено и `throwIfNotFound = true` — бросает `InvalidOperationException` |
| `SetProperty` | `void SetProperty(object, string propertyName, object? value, bool throwIfNotFound = true)` | Записывает значение в свойство. Если свойство не найдено и `throwIfNotFound = true` — бросает `InvalidOperationException` |
| `GetPropertyAsString` | `string GetPropertyAsString(object, string propertyName, IObjectToStringConverter? converter = null)` | Читает свойство и конвертирует в строку через `IObjectToStringConverter` |

### Как работает компиляция

```csharp
// Для getter:
var parameter = Expression.Parameter(typeof(object), "instance");
var propertyExpression = Expression.Property(
    Expression.Convert(parameter, type), propertyInfo);
var convertExpression = Expression.Convert(propertyExpression, typeof(object));
var lambda = Expression.Lambda<Func<object, object?>>(convertExpression, parameter);
return lambda.Compile(); // Func<object, object?>

// Для setter:
var valueParameter = Expression.Parameter(typeof(object), "value");
var assignExpression = Expression.Assign(
    propertyExpression,
    Expression.Convert(valueParameter, propertyInfo.PropertyType));
var lambda = Expression.Lambda<Action<object, object?>>(assignExpression, parameter, valueParameter);
return lambda.Compile(); // Action<object, object?>
```

### Пример использования

```csharp
var util = new PropertyUtil();

var user = new User { Name = "Иван", Age = 30 };

// Чтение
var name = util.GetProperty(user, "Name");        // "Иван"
var age = util.GetProperty(user, "Age");           // 30

// Запись
util.SetProperty(user, "Name", "Пётр");

// Чтение как строка
var nameStr = util.GetPropertyAsString(user, "Name");  // "Пётр"
var ageStr = util.GetPropertyAsString(user, "Age");    // "30"

// Без выбрасывания исключения
var missing = util.GetProperty(user, "MissingProp", throwIfNotFound: false); // null
```

---

## Интерфейсы

### IPropertyGetter

**Namespace:** `Shared.Domain.Core.Utils.Interfaces`

```csharp
public interface IPropertyGetter
{
    object? GetProperty(object? obj, string propertyName, bool throwIfNotFound = true);
    string GetPropertyAsString(object obj, string propertyName, IObjectToStringConverter? converter = null);
}
```

Контракт для чтения свойств. Позволяет заменить реализацию через DI.

### IPropertySetter

**Namespace:** `Shared.Domain.Core.Utils.Interfaces`

```csharp
public interface IPropertySetter
{
    void SetProperty(object obj, string propertyName, object? value, bool throwIfNotFound = true);
}
```

Контракт для записи свойств.

---

## Object-to-String Conversion

### IObjectToStringConverter

**Namespace:** `Shared.Domain.Core.Converters.Interfaces`

```csharp
public interface IObjectToStringConverter
{
    string Convert(object? valueToConvert);
}
```

Интерфейс для конвертации произвольных объектов в строковое представление.

### DefaultObjectToStringConverter

**Класс:** `DefaultObjectToStringConverter` (`Shared.Domain.Core.Converters`)

Реализация по умолчанию, используемая в `PropertyUtil.GetPropertyAsString()`.

| Тип значения | Формат | Пример |
|--------------|--------|--------|
| `null` | `string.Empty` | `""` |
| `string` | Как есть | `"Иван"` |
| `bool` | `"Да"` / `"Нет"` | `true` → `"Да"` |
| `Guid` | Без дефисов (`"N"`) | `"a1b2c3d4e5f6..."` |
| `Enum` | `DescriptionAttribute` | `OrderStatus.Pending` → `"В обработке"` |
| `DateTime` | `yyyy-MM-dd` | `"2024-03-15"` |
| `DateOnly` | `yyyy-MM-dd` | `"2024-03-15"` |
| Остальные | `.ToString()` | Зависит от типа |

### Пример

```csharp
var converter = new DefaultObjectToStringConverter();

converter.Convert(null);              // ""
converter.Convert(true);              // "Да"
converter.Convert(Guid.NewGuid());    // "a1b2c3d4..."
converter.Convert(OrderStatus.Pending); // "В обработке"
converter.Convert(new DateTime(2024, 3, 15)); // "2024-03-15"
```

### Кастомный конвертер

```csharp
public class CustomObjectToStringConverter : IObjectToStringConverter
{
    public string Convert(object? valueToConvert)
    {
        return valueToConvert switch
        {
            decimal d => d.ToString("C", CultureInfo.GetCultureInfo("ru-RU")),
            _ => new DefaultObjectToStringConverter().Convert(valueToConvert)
        };
    }
}

// Использование
var str = util.GetPropertyAsString(order, "Total", new CustomObjectToStringConverter());
// "1 500,00 ₽"
```

---

---

## См. также

| Документ | Описание |
|----------|----------|
| [API Client](api-client.md) | Использование PropertyUtil для multipart form data сериализации |
| [Common Extensions](common-extensions.md) | EnumExtensions.Description() — используется в DefaultObjectToStringConverter |
