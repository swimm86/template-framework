# Гайд: `BatchHelper` (`Shared.Common.Batch`)

**Сборка:** `Shared.Common`  
**Пространство имён:** `Shared.Common.Batch`  
**Класс:** `BatchHelper`  
**Константа по умолчанию:** `Constants.DefaultBatchSize` = **100** (см. `Shared.Common.Batch.Constants.cs:17`)

`BatchHelper` — низкоуровневый помощник для **порционной** (батчевой) выборки: каждый шаг — вызов `getBatchFunc(skip, take)` (или аналог с кастомным типом батча), затем проверка «пусто / стоп», при необходимости элемент попадает в **`IAsyncEnumerable`**. Подходит для **памяти в файле, коллекции в БД, in-memory списка** — везде, где есть логика «взять следующую порцию по смещению и размеру».

Для **постраничных API с `PageNumber` / `TotalPages`** удобнее высокоуровневые расширения **`HttpRequestExtensions`** (см. [гайд по постраничным запросам](batch-request.md)): они сами двигают `PageableRequest` и стыкуются с **`IHttpBatchRetryPolicy`**.

---

## Возможности API

| Метод | Возвращает | Назначение |
|--------|------------|------------|
| `ProcessBatchesAsync<TObject>(getBatchFunc, batchSize, processBatchAction, ct)` | `Task` | Упрощённый сценарий: батч — `ICollection<TObject>`, конец когда коллекция **пуста**. `CancellationToken` **не** передаётся в `getBatchFunc`. |
| `BatchSelectAsync<TObject>(getBatchFunc, batchSize, ct)` | `IAsyncEnumerable<ICollection<TObject>>` | То же определение «пусто» и учёт `Count` для `skip`. |
| `ProcessBatchesAsync<TBatch>(getBatchFunc, isBatchEmptyFunc, batchSizeFunc, processBatchAction, batchSize, isNeedToBreakFunc, ct)` | `Task` | Полная схема: произвольный `TBatch`, свои предикаты пустоты и остановки. `CancellationToken` **не** передаётся в `getBatchFunc`; для пробрасывания используйте замыкание. |
| `BatchSelectAsync<TBatch>(getBatchFunc, isBatchEmptyFunc, batchSizeFunc, batchSize, isNeedToBreakFunc, ct)` | `IAsyncEnumerable<TBatch>` | Ядро: гибкая остановка и учёт обработанного объёма. `CancellationToken` помечен `[EnumeratorCancellation]` и пробрасывается в `getBatchFunc`-замыкание явно (см. замечание ниже). |

> **⚠️ Особенность проброса `CancellationToken` в `getBatchFunc`:**  
> В `Shared.Common.Batch.BatchHelper` параметр `cancellationToken` **не** передаётся в `getBatchFunc` напрямую — обёртки `ProcessBatchesAsync` и `BatchSelectAsync` принимают его как отдельный параметр и пробрасывают через `ForEachAsync`/`EnumeratorCancellation`. Если источник (например, репозиторий) требует `CancellationToken` внутри выборки, захватите его в замыкание:  
> `getBatchFunc: (skip, take) => repo.GetRangeAsync(options, skip, take, ct)`.  
> Сравните: `Shared.Domain.Core.Dal.Repository.Extensions.RepositoryExtensions.ProcessBatchesAsync` уже делает это «из коробки» (`RepositoryExtensions.cs:289-294`).

Во всех вариантах перечисление через расширение **`ForEachAsync`** для `IAsyncEnumerable` корректно работает с `WithCancellation`.

---

## Упрощённые перегрузки

### `ProcessBatchesAsync` для `ICollection<TObject>`

- **`getBatchFunc(int skip, int take)`** — вернуть очередную порцию (например, `items.Skip(skip).Take(take).ToList()`).
- Конец цикла: возвращена **пустая** коллекция.
- Внутри, **после** вызова `getBatchFunc`, `isBatchEmptyFunc` проверяет результат и завершает цикл при пустой коллекции. При непустой — `batchSizeFunc(batch) = batch.Count` прибавляется к `processed`.

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

### Сигнатура

```csharp
public static IAsyncEnumerable<TBatch> BatchSelectAsync<TBatch>(
    Func<int, int, Task<TBatch>> getBatchFunc,
    Func<TBatch, bool> isBatchEmptyFunc,
    Func<TBatch, int>? batchSizeFunc = null,
    int batchSize = Constants.DefaultBatchSize,
    Func<bool>? isNeedToBreakFunc = null,
    [EnumeratorCancellation] CancellationToken cancellationToken = default);
```

### Параметры

| Параметр | Роль |
|----------|------|
| `getBatchFunc(processed, batchSize)` | Запрос батча: первый аргумент — накопленное смещение (если задан `batchSizeFunc`), второй — запрошенный размер порции |
| `isBatchEmptyFunc(batch)` | `true` — батч считается пустым, в поток **не** кладётся, цикл завершается |
| `batchSizeFunc` | Сколько прибавить к внутреннему счётчику `processed` **после** каждого вызова `getBatchFunc`; влияет на условие «неполный последний батч» |
| `batchSize` | Второй аргумент `getBatchFunc` и размер «окна» для проверки `processed % batchSize` |
| `isNeedToBreakFunc` | Дополнительный выход **до** следующего вызова `getBatchFunc` |

### Условие остановки по неполному последнему батчу

Алгоритм цикла (см. `BatchHelper.cs:196-222`):

1. Проверка условий остановки: `(processed != 0 && processed % batchSize != 0) || isNeedToBreakFunc?.Invoke() == true` → `break`.
2. Вызов `getBatchFunc(processed, batchSize)`.
3. **Если** `batchSizeFunc != null`: `processed += batchSizeFunc(batch)`. (Обратите внимание: инкремент происходит **до** проверки `isBatchEmptyFunc`, поэтому пустой батч всё равно «учитывается» в счётчике.)
4. Проверка `isBatchEmptyFunc(batch)`: если `true` → `break`. Иначе — `yield return batch`.

> Если **`batchSizeFunc` = `null`**, `processed` не увеличивается, поэтому условие остановки по «неполному последнему батчу» (`processed % batchSize != 0`) **никогда** не сработает. В этом случае завершение опирается только на **`isBatchEmptyFunc`** и при необходимости **`isNeedToBreakFunc`**.

### Связь с постраничным HTTP

Расширения из **`HttpRequestExtensions`** используют именно эту перегрузку с кастомным **`isNeedToBreakFunc`**, чтобы корректно завершаться по **`TotalPages`** и не терять последнюю страницу (см. `HttpRequestExtensions.cs:177-193`).

---

## `ProcessBatchesAsync<TBatch>` (полная)

Оборачивает тот же поток, что даёт **`BatchSelectAsync`**, и для каждого непустого батча вызывает **`processBatchAction`**. Удобно, когда не нужен явный `await foreach`.

```csharp
public static Task ProcessBatchesAsync<TBatch>(
    Func<int, int, Task<TBatch>> getBatchFunc,
    Func<TBatch, bool> isBatchEmptyFunc,
    Func<TBatch, int>? batchSizeFunc = null,
    Func<TBatch, Task>? processBatchAction = null,
    int batchSize = Constants.DefaultBatchSize,
    Func<bool>? isNeedToBreakFunc = null,
    CancellationToken cancellationToken = default);
```

---

## Выбор между `BatchHelper` и `HttpRequestExtensions`

| Критерий | `BatchHelper` | `HttpRequestExtensions` |
|----------|----------------|-------------------------|
| Модель данных | `skip` / `take` или свой `TBatch` | `PageableRequest` / `PageableResponse<T>` |
| Номер страницы API | Нет, только смещение и размер порции | Да, `PageNumber` + `PageSize` |
| Retry страницы | Реализуете сами внутри `getBatchFunc` | Встроенно через **`IHttpBatchRetryPolicy`** |
| Проброс `CancellationToken` в `getBatchFunc` | **Нет** автоматически — захватывайте через замыкание | Да, передаётся в делегат запроса |
| Репозиторий / EF | Типичный сценарий (`GetRangeAsync`) | Реже, если не мапите на page API |

> Готовый пример для EF: `Shared.Domain.Core.Dal.Repository.Extensions.RepositoryExtensions.ProcessBatchesAsync` (см. `RepositoryExtensions.cs:281-294`) оборачивает `GetRangeAsync(options, skip, take, cancellationToken)` и **сам** пробрасывает `CancellationToken` в `getBatchFunc`.

---

## Константы

- **`Shared.Common.Batch.Constants.DefaultBatchSize`** = **100** — значение по умолчанию для параметра **`batchSize`**, если не указано иное. Используется также в `PageableRequest.PageSize` по умолчанию (см. `Shared.Application.Core.Dto.Requests.PageableRequest.cs:38`).

---

## См. также

| Документ | Описание |
|----------|----------|
| [Batch Request](batch-request.md) | Постраничные HTTP-запросы и retry |
| [Filtering & Sorting Guide](filtering-sorting-guide.md) | Фильтрация и сортировка в списках |
| [Repository Pattern](repository.md) | `IRepository<T>` и `RepositoryExtensions.ProcessBatchesAsync` |
