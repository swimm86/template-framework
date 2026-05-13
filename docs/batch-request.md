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

1. **Один объект `request` на один обход.** Во время итерации меняются `PageNumber` и `PageSize`. После завершения **исходные значения не восстанавливаются**.
2. **Не используйте один и тот же `request` параллельно** из нескольких задач.
3. **`PageNumber` в начале должен быть ≥ 1.** Нумерация страниц при обходе продолжается с переданного номера (раньше API всегда начинал с 1 независимо от поля — это изменилось).
4. **Размер «батча» в параметре `request.PageSize`** — это **размер страницы** (`PageSize`), который уходит в API на каждый запрос.
5. **`processFunc` при ошибке** не оборачивается retry: повторяется только вызов **HTTP-страницы** при переданной `IHttpBatchRetryPolicy`.

---

## Практические примеры

### Загрузка всех страниц с обработкой каждой

Расширение навешивается на **делегат** запроса (метод группы или лямбда), а не на результат вызова.

```csharp
using Shared.Application.Core.Batch.Http.Extensions;

var request = new PpsObjectListRequest
{
    PageNumber = 1,
    PageSize = 100,
    Filter = new PpsObjectListFilter { PpsVersionId = versionId },
};

// Пример: метод клиента принимает CancellationToken
await client.GetPpsObjectsAsync.BatchProcessAsync(
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
            await ProcessPpsObjectAsync(item, cancellationToken);
        }
    },
    pageRetryPolicy: null,
    cancellationToken: cancellationToken);
```

Если сигнатура клиента **без** `CancellationToken` в методе запроса:

```csharp
await client.GetPpsObjectsAsync.BatchProcessAsync(
    request,
    processFunc: async response => { /* ... */ },
    cancellationToken: cancellationToken);
```

### Обойти страницы без `processFunc`

```csharp
await client.GetPpsObjectsAsync.BatchProcessAsync(
    request: request,
    cancellationToken: cancellationToken);
```

### Потоковая обработка (`IAsyncEnumerable`)

Используйте **`BatchSelectPagesAsync`**, а не `BatchProcessAsync` (он возвращает `Task`, а не поток страниц).

```csharp
await foreach (var response in client.GetPpsObjectsAsync.BatchSelectPagesAsync(
    request: request,
    pageRetryPolicy: null,
    cancellationToken: cancellationToken))
{
    foreach (var obj in response.Payload ?? Enumerable.Empty<PpsObject>())
    {
        await writer.WriteLineAsync($"{obj.Id},{obj.Name}");
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

await client.GetPpsObjectsAsync.BatchProcessAsync(
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

По умолчанию используется **`Shared.Common.Batch.Constants.DefaultBatchSize`** (100).

Ориентиры:

| Сценарий | Размер страницы |
|----------|------------------|
| Крупные DTO / чувствительная память | 50–100 |
| Универсально | 1000 (дефолт) |
| «Толстые» API и сеть выдерживают | 500–1000 |
| Лимиты стороннего API | 50–200 |

---

## Ошибки и отладка

- Оберните внешний вызов в **`try/catch`**; **`OperationCanceledException`** — штатная отмена.
- Ошибки **в `processFunc`:** логируйте и пробрасывайте дальше, если нужно остановить весь обход.
- Для диагностики логируйте **`response.PageNumber`**, **`response.TotalPages`** и размер `Payload`.

---

## Связанные документы

- [Гайд по **`BatchHelper`**](batch-helper.md) — универсальная нарезка данных по `skip`/`take`, без привязки к `PageableRequest`.
- [Фильтры и сортировка](filtering-sorting-guide.md) — модели `PageableRequest`, фильтры и сортировка в списках.

---

## Кратко

`HttpRequestExtensions` подходит для **постраничных HTTP/клиентских** сценариев с **`PageableRequest` / `PageableResponse<T>`**, с опциональными **retry на страницу** и **потоковым** API. Для произвольных источников данных по смещению используйте **`BatchHelper`** из `Shared.Common.Batch`.
