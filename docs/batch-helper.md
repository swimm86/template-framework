# Гайд: `BatchHelper` (`Shared.Common.Batch`)

**Сборка:** `Shared.Common`  
**Пространство имён:** `Shared.Common.Batch`  
**Класс:** `BatchHelper`  
**Константа по умолчанию:** `Constants.DefaultBatchSize` (100)

`BatchHelper` — низкоуровневый помощник для **порционной** (батчевой) выборки: каждый шаг — вызов `getBatchFunc(skip, take)` (или аналог с кастомным типом батча), затем проверка «пусто / стоп», при необходимости элемент попадает в **`IAsyncEnumerable`**. Подходит для **памяти в файле, коллекции в БД, in-memory списка** — везде, где есть логика «взять следующую порцию по смещению и размеру».

Для **постраничных API с `PageNumber` / `TotalPages`** удобнее высокоуровневые расширения **`HttpRequestExtensions`** (см. [гайд по постраничным запросам](batch-request.md)): они сами двигают `PageableRequest` и стыкуются с **`IHttpBatchRetryPolicy`**.

---

## Возможности API

| Метод | Возвращает | Назначение |
|--------|------------|------------|
| `ProcessBatchesAsync<TObject>(getBatchFunc, ...)` | `Task` | Упрощённый сценарий: батч — `ICollection<TObject>`, конец когда коллекция **пуста** |
| `BatchSelectAsync<TObject>(getBatchFunc, batchSize, ...)` | `IAsyncEnumerable<ICollection<TObject>>` | То же определение «пусто» и учёт `Count` для `skip` |
| `ProcessBatchesAsync<TBatch>(...)` | `Task` | Полная схема: произвольный `TBatch`, свои предикаты пустоты и остановки |
| `BatchSelectAsync<TBatch>(...)` | `IAsyncEnumerable<TBatch>` | Ядро: гибкая остановка и учёт обработанного объёма |

Во всех вариантах поддерживается **`CancellationToken`**; перечисление через расширение **`ForEachAsync`** для `IAsyncEnumerable` использует **`WithCancellation`**.

---

## Упрощённые перегрузки

### `ProcessBatchesAsync` для `ICollection<TObject>`

- **`getBatchFunc(int skip, int take)`** — вернуть очередную порцию (например, `items.Skip(skip).Take(take).ToList()`).
- Конец цикла: возвращена **пустая** коллекция.
- Внутри после каждого **непустого** батча к `skip` прибавляется **`batch.Count`**.

```csharp
using Shared.Common.Batch;

await BatchHelper.ProcessBatchesAsync(
    getBatchFunc: (skip, take) => Task.FromResult(
        allItems.Skip(skip).Take(take).ToList()),
    batchSize: Constants.DefaultBatchSize,
    processBatchAction: async batch =>
    {
        foreach (var item in batch)
        {
            await ProcessOneAsync(item, cancellationToken);
        }
    },
    cancellationToken: cancellationToken);
```

### `BatchSelectAsync` — только поток батчей

Те же правила пустоты и подсчёта, без встроенного `processBatchAction`:

```csharp
await foreach (var batch in BatchHelper.BatchSelectAsync(
    getBatchFunc: (skip, take) => LoadChunkAsync(skip, take),
    batchSize: 250,
    cancellationToken: cancellationToken))
{
    await SaveChunkAsync(batch, cancellationToken);
}
```

---

## Полная перегрузка `BatchSelectAsync<TBatch>`

Параметры:

| Параметр | Роль |
|----------|------|
| `getBatchFunc(processed, batchSize)` | Запрос батча: первый аргумент — накопленное смещение (если задан `batchSizeFunc`), второй — запрошенный размер порции |
| `isBatchEmptyFunc(batch)` | `true` — батч считается пустым, в поток **не** кладётся, цикл завершается |
| `batchSizeFunc` | Сколько прибавить к внутреннему счётчику после **непустого** батча; влияет на условие «неполный последний батч» |
| `batchSize` | Второй аргумент `getBatchFunc` и размер «окна» для проверки `processed % batchSize` |
| `isNeedToBreakFunc` | Дополнительный выход **до** следующего вызова `getBatchFunc` |

### Условие остановки по неполному последнему батчу

Если **`batchSizeFunc` задан**, после каждого непустого батча увеличивается внутренний `processed`. Перед следующей итерацией, если **`processed != 0`** и **`processed % batchSize != 0`**, цикл **прерывается** (последняя порция была короче `batchSize` — данных больше нет).

Если **`batchSizeFunc` = `null`**, этот критерий **не используется**; останов опирается только на **`isBatchEmptyFunc`** и при необходимости **`isNeedToBreakFunc`**.

### Связь с постраничным HTTP

Расширения из **`HttpRequestExtensions`** используют именно эту перегрузку с кастомным **`isNeedToBreakFunc`**, чтобы корректно завершаться по **`TotalPages`** и не терять последнюю страницу.

---

## `ProcessBatchesAsync<TBatch>` (полная)

Оборачивает тот же поток, что даёт **`BatchSelectAsync`**, и для каждого непустого батча вызывает **`processBatchAction`**. Удобно, когда не нужен явный `await foreach`.

---

## Выбор между `BatchHelper` и `HttpRequestExtensions`

| Критерий | `BatchHelper` | `HttpRequestExtensions` |
|----------|----------------|-------------------------|
| Модель данных | `skip` / `take` или свой `TBatch` | `PageableRequest` / `PageableResponse<T>` |
| Номер страницы API | Нет, только смещение и размер порции | Да, `PageNumber` + `PageSize` |
| Retry страницы | Реализуете сами внутри `getBatchFunc` | Встроенно через **`IHttpBatchRetryPolicy`** |
| Репозиторий / EF | Типичный сценарий (`GetRangeAsync`) | Реже, если не мапите на page API |

Пример для **`RepositoryExtensions`** в домене: вызов **`BatchHelper.ProcessBatchesAsync`** с **`GetRangeAsync(options, skip, take, ...)`**.

---

## Константы

- **`Shared.Common.Batch.Constants.DefaultBatchSize`** — значение по умолчанию для параметра **`batchSize`**, если не указано иное.

---

## См. также

| Документ | Описание |
|----------|----------|
| [Batch Request](batch-request.md) | Постраничные HTTP-запросы и retry |
| [Filtering & Sorting Guide](filtering-sorting-guide.md) | Фильтрация и сортировка в списках |
