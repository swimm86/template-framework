# Гайд: постраничные HTTP-запросы и батчи (`HttpRequestExtensions`)

Расширения для автоматического обхода **всех страниц** пагинированного API без ручного цикла по `PageNumber`. Реализация живёт в **`Shared.Application.Core`**, опирается на **`Shared.Common.Batch.BatchHelper`** (см. отдельный [гайд по `BatchHelper`](batch-helper.md)).

**Пространство имён:** `Shared.Application.Core.Batch.Http.Extensions`  
**Класс:** `HttpRequestExtensions`

---

## Общие вопросы

### Что это и зачем

`HttpRequestExtensions` добавляет к делегату запроса методы:

| Метод | Назначение |
|--------|------------|
| `BatchProcessAsync` | Обойти все страницы и для каждой вызвать опциональный `processFunc` |
| `BatchSelectPagesAsync` | Вернуть `IAsyncEnumerable<TResponse>` — страницы по мере загрузки |

Подходят запросы **`TRequest : PageableRequest`** и ответы **`TResponse : PageableResponse<TPayload>`**.

### Когда использовать

- Нужно выгрузить или обработать **весь** результат пагинированного API.
- Важно **не держать все строки в памяти** — обрабатывайте по странице или батчу.
- Нужна **отмена** через `CancellationToken`.
- Опционально — **повторы** при временных сбоях HTTP (см. раздел про retry).

### Преимущества

- **Автоматическая пагинация:** `PageNumber` и `PageSize` на переданном `request` выставляются при каждом запросе страницы.
- **Контроль памяти:** потоковый режим через `BatchSelectPagesAsync` + `await foreach`.
- Две формы делегата запроса: **с `CancellationToken`** и **без** (токен отмены всё равно участвует в самом обходе).
- **Политика повторов** только на **запрос одной страницы** (не на `processFunc`).

---

## Обязательно прочитать: ограничения и контракт

1. **Один объект `request` на один обход.** Во время итерации меняются `PageNumber` и `PageSize`. После завершения **исходные значения не восстанавливаются** (см. [исходник `HttpRequestExtensions.cs:32`](../src/Shared/Core/Shared.Application.Core/Batch/Http/Extensions/HttpRequestExtensions.cs)).
2. **Не используйте один и тот же `request` параллельно** из нескольких задач.
3. **`PageNumber` в начале должен быть ≥ 1.** Нумерация страниц при обходе продолжается с переданного номера (`request.PageNumber - 1`); значение < 1 вызовет `ArgumentOutOfRangeException` (см. `HttpRequestExtensions.cs:158-164`).
4. **Размер «батча» в параметре `request.PageSize`** — это **размер страницы**, который уходит в API на каждый запрос. По умолчанию = `Constants.DefaultBatchSize` = **100** (см. `Shared.Common.Batch.Constants.cs:17` и `PageableRequest.cs:38`).
5. **`processFunc` при ошибке** не оборачивается retry: повторяется только вызов **HTTP-страницы** при переданной `IHttpBatchRetryPolicy`.

---

## `PageableRequest` и `SortOptions`

`PageableRequest` (см. `Shared.Application.Core.Dto.Requests.PageableRequest.cs`):

```csharp
public abstract record PageableRequest
{
    public const char ValueDelimiter = '.';

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = Constants.DefaultBatchSize; // 100
    public List<string>? SortOptions { get; init; }
}
```

**Формат `SortOptions`:** список строк формата `"<FieldPath>.<direction>"`, где:

- `<FieldPath>` — путь к свойству (вложенные через `.`, например `"Person.Name"` или просто `"Name"`);
- `<direction>` — `asc` или `desc` (значения атрибута `Description` у `OrderDirectionType`).

Пример:

```csharp
new PersonListRequest(DalPattern.Default)
{
    PageNumber = 1,
    PageSize = 50,
    SortOptions = new List<string>
    {
        "Name.asc",
        "Email.desc",
    },
};
```

Метод `PageableRequest.ConvertSortOptions()` парсит строки в `ICollection<SortOption>` (см. `PageableRequest.cs:49-67`): разделяет по `ValueDelimiter = '.'`, последний сегмент интерпретирует как `OrderDirectionType` через `GetEnumValueByDescription`, остальные склеивает обратно через `.` в `SortOption.Key`.

---

## Практические примеры

В примерах ниже используются **реальные** типы из `Template.Getter.Application.Abstractions.Features.Person.List`:

- `PersonListRequest : PageableRequest<PersonListFilter>` (см. `PersonListRequest.cs`)
- `PersonListResponse : PageableResponse<ICollection<PersonListPayload>>` (см. `PersonListResponse.cs`)
- `PersonListFilter { Name, NameContains, Email, EmailContains }` (см. `PersonListFilter.cs`)

### Загрузка всех страниц с обработкой каждой

Расширение навешивается на **делегат** запроса (метод группы или лямбда), а не на результат вызова.

```csharp
using Shared.Application.Core.Batch.Http.Extensions;

var request = new PersonListRequest(DalPattern.Default)
{
    PageNumber = 1,
    PageSize = 100,
    Filter = new PersonListFilter
    {
        NameContains = "Иванов",
    },
};

// Пример: метод клиента принимает CancellationToken
await client.GetPersonsAsync.BatchProcessAsync(
    request: request,
    processFunc: async response =>
    {
        var items = response.Payload ?? [];
        logger.LogInformation(
            "Страница {Page}/{Total}, элементов: {Count}",
            response.PageNumber,
            response.TotalPages,
            items.Count);

        foreach (var item in items)
        {
            await ProcessPersonAsync(item, cancellationToken);
        }
    },
    pageRetryPolicy: null,
    cancellationToken: cancellationToken);
```

Если сигнатура клиента **без** `CancellationToken` в методе запроса:

```csharp
await client.GetPersonsAsync.BatchProcessAsync(
    request,
    processFunc: async response => { /* ... */ },
    cancellationToken: cancellationToken);
```

### Обойти страницы без `processFunc`

```csharp
await client.GetPersonsAsync.BatchProcessAsync(
    request: request,
    cancellationToken: cancellationToken);
```

### Потоковая обработка (`IAsyncEnumerable`)

Используйте **`BatchSelectPagesAsync`**, а не `BatchProcessAsync` (он возвращает `Task`, а не поток страниц).

```csharp
await foreach (var response in client.GetPersonsAsync.BatchSelectPagesAsync(
    request: request,
    pageRetryPolicy: null,
    cancellationToken: cancellationToken))
{
    foreach (var person in response.Payload ?? Enumerable.Empty<PersonListPayload>())
    {
        await writer.WriteLineAsync($"{person.Id},{person.Name}");
    }
}
```

---

## Повторы запроса страницы (retry)

Повторы применяются **только к одному вызову** делегата, который запрашивает **одну страницу** (после этого успешный ответ отдаётся в `processFunc` или в поток). Ошибки внутри **`processFunc`** политика не повторяет.

### Компоненты

- **`IHttpBatchRetryPolicy`** — контракт (`ExecuteAsync`).
- **`DefaultHttpBatchRetryPolicy`** — реализация по **`RetryConfiguration`**:
  - **`BackoffConfiguration`:** `MaxAttempts` (включая первую попытку; `1` = без повторов), экспоненциальный backoff от `InitialDelay` с множителем 2, опционально `MaxDelay`, джиттер `UseBackoffJitter` (~0.75–1.25×).
  - **`TransientConfiguration`:** доп. признак транзиентности `IsAdditionalTransientException`, доп. нижняя граница паузы `ResolveRetryDelayAfterTransientFailure`.
- Встроенная транзиентность (упрощённо): **`HttpRequestException`** (в т.ч. без кода — как сетевой сбой), коды **408, 429, 5xx**, **`IOException`**, **`SocketException`**, **`TimeoutException`**, часть **`TaskCanceledException`** (таймаут/отмена не из вашего токена обхода).
- Подсказка паузы: ключ **`Retry-After`** в **`Exception.Data`** (строка в секундах, число или `TimeSpan`) — максимум по цепочке `InnerException`; итоговая задержка не ниже max(backoff, подсказки).

### Пример

```csharp
using Shared.Application.Core.Batch.Http.Extensions;
using Shared.Application.Core.Batch.Http.RetryPolicy;
using Shared.Application.Core.Batch.Http.RetryPolicy.Models;

var retryOptions = new RetryConfiguration
{
    Backoff = new BackoffConfiguration
    {
        MaxAttempts = 4,
        InitialDelay = TimeSpan.FromSeconds(1),
        MaxDelay = TimeSpan.FromSeconds(30),
        UseBackoffJitter = true,
    },
    Transient = new TransientConfiguration
    {
        IsAdditionalTransientException = ex =>
            ex is MyAppTransientException,
    },
};

var pageRetryPolicy = new DefaultHttpBatchRetryPolicy(retryOptions);

await client.GetPersonsAsync.BatchProcessAsync(
    request,
    processFunc: async r => { /* ... */ },
    pageRetryPolicy: pageRetryPolicy,
    cancellationToken: cancellationToken);
```

Тот же `pageRetryPolicy` передаётся в **`BatchSelectPagesAsync`**.

### Отмена

При срабатывании **`CancellationToken`** ожидание перед повтором прерывается; отмена обхода отменяет перечисление (`ForEachAsync` / `await foreach` с `WithCancellation`).

---

## Настройка размера страницы (`request.PageSize`)

По умолчанию используется **`Shared.Common.Batch.Constants.DefaultBatchSize`** = **100** (см. `Shared.Common.Batch.Constants.cs:17`).

Ориентиры:

| Сценарий | Размер страницы |
|----------|------------------|
| Крупные DTO / чувствительная память | 50–100 |
| Универсально (дефолт) | **100** |
| «Толстые» API и сеть выдерживают | 500–1000 |
| Лимиты стороннего API | 50–200 |

---

## Ошибки и отладка

- Оберните внешний вызов в **`try/catch`**; **`OperationCanceledException`** — штатная отмена.
- Ошибки **в `processFunc`:** логируйте и пробрасывайте дальше, если нужно остановить весь обход.
- Для диагностики логируйте **`response.PageNumber`**, **`response.TotalPages`** и размер `Payload`.

---

## См. также

| Документ | Описание |
|----------|----------|
| [Batch Helper](batch-helper.md) | Универсальная нарезка данных по skip/take |
| [Filtering & Sorting Guide](filtering-sorting-guide.md) | Модели `PageableRequest`, фильтры и сортировка |

---

## Кратко

`HttpRequestExtensions` подходит для **постраничных HTTP/клиентских** сценариев с **`PageableRequest` / `PageableResponse<T>`**, с опциональными **retry на страницу** и **потоковым** API. Для произвольных источников данных по смещению используйте **`BatchHelper`** из `Shared.Common.Batch`.
