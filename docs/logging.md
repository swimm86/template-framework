# Логирование методов: LogTask и [LogMethod]

В проекте реализованы два взаимозаменяемых механизма структурированного логирования выполнения методов. Оба используют **одни и те же шаблоны сообщений** и гарантируют идентичный формат вывода в лог.

| Подход | Файл | Когда применять |
|---|---|---|
| `LogTask` / `LogTaskAsync` | `LoggerExtensions.cs` | Явное логирование; нужен контроль над потоком выполнения |
| `[LogMethod]` | `LogMethodAttribute.cs` | Декларативное логирование; не хочется менять тело метода |

---

## Содержание

1. [Быстрый старт](#быстрый-старт)
2. [Что попадает в лог](#что-попадает-в-лог)
3. [LogTask / LogTaskAsync](#logtask--logtaskasync)
4. [Атрибут \[LogMethod\]](#атрибут-logmethod)
5. [Когда использовать что](#когда-использовать-что)
6. [Расширение поведения](#расширение-поведения)
7. [Внутреннее устройство](#внутреннее-устройство)
8. [Ограничения и подводные камни](#ограничения-и-подводные-камни)

---

## Быстрый старт

### 1. Подключение using

```csharp
using Shared.Common.Logging.Extensions;   // LogTask / LogTaskAsync
using Shared.Common.Logging.Attributes;   // [LogMethod]
```

### 2. Инициализация (один раз при старте приложения)

`[LogMethod]` требует, чтобы при старте был вызван `LoggingServiceAccessor.Configure`. Если вы используете `ConfigurePresentationCore`, это происходит автоматически:

```csharp
// Program.cs
app.ConfigurePresentationCore(); // внутри вызывает LoggingServiceAccessor.Configure(app.Services)
```

Если вы подключаете сервис без `Shared.Presentation.Core`, вызовите вручную:

```csharp
app.Lifetime.ApplicationStarted.Register(() =>
    LoggingServiceAccessor.Configure(app.Services));
```

> **Важно:** `[LogMethod]` без вызова `Configure` не выбросит исключение в Production,  
> но `Debug.Assert` сработает в Debug-сборке, предупреждая о пропущенной инициализации.

---

## Что попадает в лог

Оба механизма производят одинаковые сообщения через `ILogger.Log` с **structured logging**-шаблонами:

| Событие | Уровень | Шаблон |
|---|---|---|
| Начало | `logLevel` (по умолч. `Information`) | `{process} started.` |
| Успешное завершение | `logLevel` | `{process} completed.` |
| Ошибка | `Error` | `{process} failed.` |
| Время выполнения — успех | `logLevel` | `{process} processed time: {time}ms.` |
| Время выполнения — ошибка | `Error` | `{process} processed time: {time}ms.` |

**Пример вывода** для метода `GetPersonsAsync` класса `PersonsService`:

```
[Information] 'PersonsService.GetPersonsAsync' started.
[Information] 'PersonsService.GetPersonsAsync' completed.
[Information] 'PersonsService.GetPersonsAsync' processed time: 42ms.
```

При ошибке:

```
[Information] 'PersonsService.GetPersonsAsync' started.
[Error]       'PersonsService.GetPersonsAsync' failed. System.TimeoutException: ...
[Error]       'PersonsService.GetPersonsAsync' processed time: 5012ms.
```

> Время при ошибке намеренно логируется на `Error`, чтобы elapsed не потерялся при фильтрации `Information`-потока в Production.

---

## LogTask / LogTaskAsync

Явный подход — оборачиваете вызов лямбдой. `ILogger` берётся из DI обычным способом.

### Перегрузки

```csharp
// async с возвращаемым значением
Task<T> logger.LogTaskAsync<T>(Func<Task<T>> action, ...)

// async без возвращаемого значения (с CancellationToken)
Task logger.LogTaskAsync(Func<Task> action, CancellationToken token, ...)

// sync с возвращаемым значением
T logger.LogTask<T>(Func<T> action, ...)

// sync без возвращаемого значения
void logger.LogTask(Action action, ...)
```

Все перегрузки принимают одинаковый набор необязательных параметров:

| Параметр | Тип | По умолчанию | Описание |
|---|---|---|---|
| `methodName` | `string?` | `[CallerMemberName]` | Имя метода, автоматически определяется |
| `processDescription` | `string?` | `null` | Описание процесса; если задано, заменяет имя метода |
| `logProcessedTime` | `bool` | `true` | Логировать время выполнения |
| `logLevel` | `LogLevel` | `Information` | Уровень логирования для started/completed/elapsed |

### Примеры

**Async с возвращаемым значением:**

```csharp
public Task<PersonListResponse> GetPersonsAsync(PersonListRequest request, CancellationToken ct)
{
    return _logger.LogTaskAsync(
        () => _repository.GetPersonsAsync(request, ct),
        processDescription: "Получение списка персон");
}
```

**Async без возвращаемого значения с CancellationToken:**

```csharp
public Task ImportAsync(byte[] data, CancellationToken ct)
{
    return _logger.LogTaskAsync(
        () => _repository.ImportAsync(data),
        token: ct,
        processDescription: "Импорт данных");
}
```

**Sync:**

```csharp
public int Calculate(int x, int y)
{
    return _logger.LogTask(
        () => _service.Calculate(x, y),
        logLevel: LogLevel.Debug);
}
```

**Без явного описания** — используется имя вызывающего метода через `[CallerMemberName]`:

```csharp
public Task<PersonListResponse> GetPersonsAsync(PersonListRequest request, CancellationToken ct)
{
    // В лог попадёт: 'GetPersonsAsync' started.
    return _logger.LogTaskAsync(() => _repository.GetPersonsAsync(request, ct));
}
```

**Без логирования времени:**

```csharp
return _logger.LogTaskAsync(
    () => _repository.GetAsync(id, ct),
    logProcessedTime: false);
```

---

## Атрибут [LogMethod]

Декларативный подход — добавляете атрибут к методу, логирование встраивается в IL на этапе компиляции через Fody. Тело метода не меняется.

### Параметры

| Параметр | Тип | По умолчанию | Описание |
|---|---|---|---|
| `processDescription` | `string` | `""` | Описание процесса; если не задано, используется `'ClassName.MethodName'` |
| `logProcessedTime` | `bool` | `true` | Логировать время выполнения |
| `logLevel` | `LogLevel` | `Information` | Уровень логирования |

### Примеры

**Минимальный — всё по умолчанию:**

```csharp
[LogMethod]
public async Task<IEnumerable<PersonDto>> GetAllAsync(CancellationToken ct)
{
    return await _repository.GetAllAsync(ct);
}
// В лог: 'PersonsService.GetAllAsync' started. / completed. / processed time: 12ms.
```

**С описанием процесса:**

```csharp
[LogMethod("Импорт данных из файла")]
public void ImportData(byte[] data)
{
    _repository.Import(data);
}
// В лог: 'Импорт данных из файла' started. / completed. / processed time: 345ms.
```

**Без замера времени:**

```csharp
[LogMethod(logProcessedTime: false)]
public async Task<PersonDto> GetByIdAsync(Guid id, CancellationToken ct)
{
    return await _repository.GetByIdAsync(id, ct);
}
```

**С уровнем Debug:**

```csharp
[LogMethod(logLevel: LogLevel.Debug)]
public bool Validate(CreatePersonRequest request)
{
    return _validator.Validate(request).IsValid;
}
```

**Async Task (без результата):**

```csharp
[LogMethod("Отправка уведомления")]
public async Task NotifyAsync(Guid userId, string message, CancellationToken ct)
{
    await _notificationService.SendAsync(userId, message, ct);
}
```

### Именование процесса

Если `processDescription` не задан, атрибут автоматически формирует имя из класса и метода:

```
'PersonsService.GetAllAsync'
'ImportService.ImportData'
```

Это позволяет легко фильтровать логи по классу или методу без явного описания.

---

## Когда использовать что

### Используйте `[LogMethod]`, когда:

- метод простой и не требует изменений после добавления логирования
- нужно быстро добавить логирование к группе методов (атрибут на каждом)
- не хочется "зашумлять" тело метода инфраструктурным кодом
- логирование нужно в Application/Domain слоях, где `ILogger` не всегда доступен

```csharp
// До
public async Task<PersonListResponse> GetPersonsAsync(PersonListRequest request, CancellationToken ct)
{
    var skip = request.PageSize * request.PageNumber;
    return await _repo.GetRangeAsync(skip, request.PageSize, ct);
}

// После — ничего не поменялось в теле
[LogMethod]
public async Task<PersonListResponse> GetPersonsAsync(PersonListRequest request, CancellationToken ct)
{
    var skip = request.PageSize * request.PageNumber;
    return await _repo.GetRangeAsync(skip, request.PageSize, ct);
}
```

### Используйте `LogTask`, когда:

- нужно логировать **часть** метода, а не весь метод целиком
- необходим доступ к переменным метода внутри лямбды
- вы пишете инфраструктурный или Presentation-код, где `ILogger` уже есть в конструкторе
- нужна более тонкая управляемость (например, разные `processDescription` в зависимости от аргументов)

```csharp
public async Task<IActionResult> CreatePersonAsync(CreatePersonRequest request, CancellationToken ct)
{
    return await _logger.LogTaskAsync(
        async () =>
        {
            var result = await _sender.Send(new CreatePersonCommand(request), ct);
            return StatusCode(result.StatusCode, result) as IActionResult;
        },
        processDescription: $"Создание персоны '{request.Name}'");
}
```

---

## Расширение поведения

`LogMethodAttribute` не запечатан и предоставляет четыре `protected virtual` метода для переопределения. Это позволяет создать собственный атрибут с дополнительными structured-logging свойствами — например, добавлять `correlationId` или `tenantId` ко всем сообщениям.

```csharp
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class LogMethodWithCorrelationAttribute(
    string processDescription = "",
    bool logProcessedTime = true,
    LogLevel logLevel = LogLevel.Information)
    : LogMethodAttribute(processDescription, logProcessedTime, logLevel)
{
    protected override void OnLogStarted(ILogger? logger, string process)
    {
        using (logger?.BeginScope(new { CorrelationId = Activity.Current?.Id }))
        {
            base.OnLogStarted(logger, process);
        }
    }

    protected override void OnLogFailed(ILogger? logger, string process, Exception exception)
    {
        using (logger?.BeginScope(new { CorrelationId = Activity.Current?.Id }))
        {
            base.OnLogFailed(logger, process, exception);
        }
    }
}
```

Переопределяемые методы:

| Метод | Когда вызывается |
|---|---|
| `OnLogStarted(logger, process)` | При входе в метод |
| `OnLogCompleted(logger, process)` | При успешном выходе |
| `OnLogFailed(logger, process, exception)` | При исключении |
| `OnLogElapsed(logger, process, level, stopwatch)` | После завершения (успех или ошибка) |

---

## Внутреннее устройство

### Единые шаблоны сообщений

Оба механизма используют константы из `LogMessages` (internal):

```
"{process} started."
"{process} completed."
"{process} failed."
"{process} processed time: {time}ms."
```

Это гарантирует: если вы перешли с `LogTask` на `[LogMethod]` или наоборот, в лог-аггрегаторе (Seq, Elastic, Loki) запросы по полю `process` или по тексту сообщения останутся работать без изменений.

### Жизненный цикл [LogMethod] при AOP-вставке

Fody вставляет вызовы аспекта на уровне IL. Для метода `GetPersonsAsync` реальное поведение эквивалентно:

```
OnEntry  →  логирует "started", запускает Stopwatch, сохраняет контекст
  ↓
тело метода выполняется
  ↓
OnExit   →  если исключение уже есть — выходит (OnException уже отработал)
            если Task — оборачивает в AsyncMethodLogger.Wrap (продолжает асинхронно)
            иначе — логирует "completed" + elapsed
  ↓ (при исключении вместо OnExit)
OnException → логирует "failed" + elapsed на Error
```

### Async Task: почему нужен AsyncMethodLogger

Метод, возвращающий `Task`, с точки зрения IL завершается синхронно — он возвращает незавершённый таск. `OnExit` вызывается сразу после `return`, а не после `await`. Поэтому для корректного логирования момента фактического завершения атрибут подменяет возвращаемый `Task` на обёртку, которая вызывает колбэки после await:

```
метод возвращает Task<T>
    ↓
AsyncMethodLogger.Wrap(originalTask, onCompleted, onFailed)
    ↓ (внутри Wrap)
await originalTask
    ↓ успех          ↓ ошибка
onCompleted()      onFailed(ex)
```

Делегаты для `Task<T>` компилируются через `Expression.Lambda` и кешируются в `ConcurrentDictionary<Type, Func<...>>` — overhead рефлексии платится только один раз для каждого уникального `T`.

### Уровень логирования elapsed при ошибке

Elapsed при ошибке всегда логируется на `LogLevel.Error`. Это принципиально: если в Production отфильтровать `Information`, время выполнения упавшей операции не исчезнет из потока ошибок.

В `LogTaskAsync<T>` это реализовано через переназначение переменной в `catch`:

```csharp
catch (Exception ex)
{
    LogFailed(logger, process, ex);
    logLevel = LogLevel.Error;   // переменная видна в finally
    throw;
}
finally
{
    LogElapsed(logger, process, logLevel, stopwatch);
}
```

В `[LogMethod]` через отдельный приватный метод `DoLogFailed`, который явно передаёт `LogLevel.Error` в `OnLogElapsed`.

---

## Ограничения и подводные камни

### [LogMethod]

**1. Требуется вызов `LoggingServiceAccessor.Configure` при старте**

Без этого логгер будет `null`. Исключений в Production не будет — методы выполнятся, но логов не будет. В Debug-режиме `Debug.Assert` сигнализирует о проблеме.

**2. Не применять к `static`-методам класса без DI**

`GetLogger` использует `args.Method.DeclaringType` для создания логгера через `ILoggerFactory`. Если тип не регистрируется в DI, логгер будет создан корректно — `ILoggerFactory` создаёт логгеры для любого типа. Ограничений нет.

**3. `[AttributeUsage(Inherited = false)]`**

Атрибут не наследуется автоматически при переопределении метода в наследнике. Это сделано намеренно: логирование должно быть явным на каждом уровне.

**4. Не работает с методами-расширениями и статическими классами**

Fody не может вставить аспект в статический метод расширения, если он не является экземплярным методом класса.

**5. Атрибут нельзя применять к анонимным методам и лямбдам**

`[LogMethod]` работает только с именованными методами класса.

### LogTask / LogTaskAsync

**1. Не используйте `LogTask<T>` для горячих синхронных путей**

`LogTask<T>` использует `GetAwaiter().GetResult()` — это может вызвать deadlock в контекстах с синхронизацией (например, в legacy ASP.NET). В ASP.NET Core проблем нет.

**2. `[CallerMemberName]` снимается с места вызова `LogTaskAsync`, не с метода бизнес-логики**

Если вы вызываете `LogTaskAsync` из приватного вспомогательного метода, в лог попадёт имя этого вспомогательного метода. Передайте `processDescription` явно.

**3. `LogTaskAsync(Func<Task>)` принимает `CancellationToken` обязательно**

Это отличается от generic-перегрузки `LogTaskAsync<T>`, где `CancellationToken` не принимается (для отмены используйте токен внутри самой лямбды). Сигнатуры разные — будьте внимательны при миграции.
