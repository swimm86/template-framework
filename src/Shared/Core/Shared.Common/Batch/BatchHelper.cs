// ----------------------------------------------------------------------------------------------
// <copyright file="BatchHelper.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using Shared.Common.Extensions;

namespace Shared.Common.Batch;

/// <summary>
/// Предоставляет вспомогательные методы для работы с батчами (порциями данных).
/// </summary>
/// <remarks>
/// <para>
/// Батчи позволяют обрабатывать большие коллекции данных по частям, что помогает избежать перегрузки памяти
/// и улучшает производительность при работе с большими объемами данных.
/// </para>
/// <para>
/// Основные возможности:
/// <list type="bullet">
/// <item><description>Обработка данных по частям с настраиваемым размером батча</description></item>
/// <item><description>Поддержка различных типов данных и коллекций</description></item>
/// <item><description>Асинхронная обработка с поддержкой отмены операций</description></item>
/// <item><description>Потоковая обработка данных через IAsyncEnumerable</description></item>
/// <item><description>Гибкая настройка логики обработки и проверки завершения</description></item>
/// </list>
/// </para>
/// <para>
/// Методы <c>BatchSelectAsync</c> формируют поток: получение батча, проверка «пусто/стоп», при необходимости — элемент
/// <see cref="IAsyncEnumerable{T}"/>. Методы <c>ProcessBatchesAsync</c> используют тот же поток и для каждого непустого батча вызывают пользовательский обработчик.
/// </para>
/// </remarks>
/// <example>
/// <para>Пример обработки коллекции объектов по батчам:</para>
/// <code>
/// var items = await GetLargeCollectionAsync();
///
/// await BatchHelper.ProcessBatchesAsync(
///     getBatchFunc: (skip, take) => Task.FromResult(items.Skip(skip).Take(take).ToList()),
///     batchSize: 100,
///     processBatchAction: async batch =>
///     {
///         foreach (var item in batch)
///         {
///             await ProcessItemAsync(item);
///         }
///     },
///     cancellationToken);
/// </code>
/// </example>
public static class BatchHelper
{
    /// <inheritdoc cref = "ProcessBatchesAsync{TBatch}(Func{int, int, Task{TBatch}}, Func{TBatch, bool},Func{TBatch, int},Func{TBatch, Task},int,Func{bool},CancellationToken)" />
    /// <typeparam name="TObject">Тип объекта внутри батча.</typeparam>
    public static Task ProcessBatchesAsync<TObject>(
        Func<int, int, Task<ICollection<TObject>>> getBatchFunc,
        int batchSize = Constants.DefaultBatchSize,
        Func<ICollection<TObject>, Task>? processBatchAction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(getBatchFunc);
        return BatchSelectAsync(
                getBatchFunc,
                batchSize,
                cancellationToken)
            .ForEachAsync(
                batch => processBatchAction?.Invoke(batch) ?? Task.CompletedTask,
                cancellationToken);
    }

    /// <summary>
    /// Формирует асинхронный поток батчей с полной настройкой логики выборки и остановки. Обходит их
    /// и для каждого непустого батча вызывает <paramref name="processBatchAction"/>.
    /// </summary>
    /// <returns>Задача, завершающаяся после обхода всех батчей.</returns>
    /// <inheritdoc cref = "BatchSelectAsync{TBatch}(Func{int,int,Task{TBatch}},Func{TBatch,bool},Func{TBatch,int},int,Func{bool},CancellationToken)" />
    /// <param name="getBatchFunc"/><param name="isBatchEmptyFunc"/><param name="batchSizeFunc"/><param name="batchSize"/>
    /// <param name="isNeedToBreakFunc"/><param name="cancellationToken"/>
    /// <param name="processBatchAction">
    /// Опциональная функция для обработки каждого батча. Если не указана, батчи обрабатываются без дополнительной логики.
    /// </param>
    public static Task ProcessBatchesAsync<TBatch>(
        Func<int, int, Task<TBatch>> getBatchFunc,
        Func<TBatch, bool> isBatchEmptyFunc,
        Func<TBatch, int>? batchSizeFunc = null,
        Func<TBatch, Task>? processBatchAction = null,
        int batchSize = Constants.DefaultBatchSize,
        Func<bool>? isNeedToBreakFunc = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(getBatchFunc);
        return BatchSelectAsync(
                getBatchFunc,
                isBatchEmptyFunc,
                batchSizeFunc,
                batchSize,
                isNeedToBreakFunc,
                cancellationToken)
            .ForEachAsync(
                batch => processBatchAction?.Invoke(batch) ?? Task.CompletedTask,
                cancellationToken);
    }

    /// <summary>
    /// Возвращает поток батчей <c>ICollection&lt;TObject&gt;</c> с предикатами по умолчанию: пустота — <c>!batch.Any()</c>, учёт размера — <c>batch.Count</c>.
    /// </summary>
    /// <remarks>
    /// Завершение по пустой коллекции; счётчик пропуска ведётся через <paramref name="batchSize"/>
    /// (Подробнее — <see cref="BatchSelectAsync{TBatch}(Func{int,int,Task{TBatch}},Func{TBatch,bool},Func{TBatch,int},int,Func{bool},CancellationToken)"/>).
    /// </remarks>
    /// <typeparam name="TObject">Тип объекта внутри батча.</typeparam>
    /// <inheritdoc cref="BatchSelectAsync{TBatch}(Func{int,int,Task{TBatch}},Func{TBatch,bool},Func{TBatch,int},int,Func{bool},CancellationToken)" />
    public static IAsyncEnumerable<ICollection<TObject>> BatchSelectAsync<TObject>(
        Func<int, int, Task<ICollection<TObject>>> getBatchFunc,
        int batchSize = Constants.DefaultBatchSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(getBatchFunc);
        return BatchSelectAsync(
            getBatchFunc: getBatchFunc,
            isBatchEmptyFunc: batch => !batch.Any(),
            batchSizeFunc: batch => batch.Count,
            batchSize,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Формирует асинхронный поток батчей с полной настройкой логики выборки и остановки.
    /// </summary>
    /// <typeparam name="TBatch">Тип одного батча (порции данных).</typeparam>
    /// <param name="getBatchFunc">
    /// Запрос очередного батча: первый аргумент — накопленное смещение (если задана <paramref name="batchSizeFunc"/>,
    /// к нему добавляется результат после каждого непустого батча), второй — <paramref name="batchSize"/>.
    /// </param>
    /// <param name="isBatchEmptyFunc">
    /// Возвращает <see langword="true"/>, если полученный батч считается пустым и цикл следует завершить (элемент в поток не добавляется).
    /// </param>
    /// <param name="batchSizeFunc">
    /// Сколько элементов учесть в <c>processedSkip</c> после каждого непустого батча;
    /// влияет на условие «неполного последнего батча» (см. <paramref name="batchSize"/>).
    /// </param>
    /// <param name="batchSize">
    /// Запрашиваемый размер батча (второй аргумент <paramref name="getBatchFunc"/>). По умолчанию — <see cref="Constants.DefaultBatchSize"/>.
    /// </param>
    /// <param name="isNeedToBreakFunc">
    /// Дополнительное условие выхода до следующего запроса батча.
    /// </param>
    /// <param name="cancellationToken">
    /// <see cref="CancellationToken"/> для отмены операции.
    /// </param>
    /// <returns>Ленивый поток непустых батчей.</returns>
    /// <remarks>
    /// <para>
    /// Цикл: проверка условий остановки (неполный предыдущий батч, <paramref name="isNeedToBreakFunc"/>), вызов <paramref name="getBatchFunc"/>,
    /// обновление счётчика через <paramref name="batchSizeFunc"/>, проверка <paramref name="isBatchEmptyFunc"/> — при непустом батче элемент отдаётся потребителю <see cref="IAsyncEnumerable{T}"/>.
    /// </para>
    /// <para>
    /// Перегрузки <c>ProcessBatchesAsync</c> строят тот же поток и для каждого элемента вызывают пользовательский обработчик
    /// (см. также <see cref="ProcessBatchesAsync{TObject}(Func{int,int,Task{ICollection{TObject}}},int,Func{ICollection{TObject},Task},CancellationToken)"/>).
    /// </para>
    /// <para>
    /// Этот метод предоставляет максимальную гибкость:
    /// <list type="bullet">
    /// <item><description>Любой тип <typeparamref name="TBatch"/></description></item>
    /// <item><description>Гибкая логика завершения через <paramref name="isBatchEmptyFunc"/></description></item>
    /// <item><description>Учёт обработанного объёма через <paramref name="batchSizeFunc"/></description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Если <paramref name="batchSizeFunc"/> не задан, счётчик обработанных элементов внутри цикла не увеличивается,
    /// поэтому условие остановки по «неполному последнему батчу» (<c>processed % batchSize != 0</c>) не используется.
    /// В таких сценариях завершение задают только <paramref name="isBatchEmptyFunc"/> и при необходимости <paramref name="isNeedToBreakFunc"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="getBatchFunc"/> или <paramref name="isBatchEmptyFunc"/> равны null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="batchSize"/> меньше или равен нулю.</exception>
    public static async IAsyncEnumerable<TBatch> BatchSelectAsync<TBatch>(
        Func<int, int, Task<TBatch>> getBatchFunc,
        Func<TBatch, bool> isBatchEmptyFunc,
        Func<TBatch, int>? batchSizeFunc = null,
        int batchSize = Constants.DefaultBatchSize,
        Func<bool>? isNeedToBreakFunc = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(getBatchFunc);
        ArgumentNullException.ThrowIfNull(isBatchEmptyFunc);

        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(batchSize),
                batchSize,
                "Batch size must be a positive number.");
        }

        var processed = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Если предыдущий батч был неполным (количество обработанных элементов не делится на размер батча),
            // значит все элементы уже обработаны и можно завершить цикл
            if ((processed != 0 && processed % batchSize != 0) ||
                isNeedToBreakFunc?.Invoke() == true)
            {
                break;
            }

            var batch = await getBatchFunc(processed, batchSize);
            if (batchSizeFunc != null)
            {
                processed += batchSizeFunc(batch);
            }

            var isEmptyBatch = isBatchEmptyFunc(batch);
            if (isEmptyBatch)
            {
                break;
            }

            yield return batch;
        }
    }
}
