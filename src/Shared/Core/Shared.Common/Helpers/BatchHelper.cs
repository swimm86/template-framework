// ----------------------------------------------------------------------------------------------
// <copyright file="BatchHelper.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Shared.Common.Helpers;

/// <summary>
/// Предоставляет вспомогательные методы для работы с батчами.
/// </summary>
public static class BatchHelper
{
    /// <summary>
    /// Обрабатывает батчи коллекций с типом <typeparamref name="TObject"/>.
    /// </summary>
    /// <typeparam name="TObject">Тип элементов коллекции.</typeparam>
    /// <param name="getBatchFunc">Функция для получения батча.</param>
    /// <param name="batchSize">Размер батча.</param>
    /// <param name="processBatchAction">Обработчик батча.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns>Результат операции.</returns>
    public static Task ProcessBatchesAsync<TObject>(
        Func<int, int, Task<ICollection<TObject>>> getBatchFunc,
        int batchSize = Const.DefaultBatchSize,
        Func<ICollection<TObject>, Task>? processBatchAction = default,
        CancellationToken cancellationToken = default)
        where TObject : class
    {
        return ProcessBatchesAsync<TObject, ICollection<TObject>, Task>(
            getBatchFunc,
            batch => !batch.Any(),
            batch => batch.Count,
            processBatchAction,
            batchSize,
            cancellationToken);
    }

    /// <summary>
    /// Обрабатывает батчи коллекций с типом <typeparamref name="TObject"/>.
    /// </summary>
    /// <typeparam name="TObject">Тип элементов коллекции.</typeparam>
    /// <typeparam name="TBatch">Тип батча.</typeparam>
    /// <typeparam name="TResult">Тип результата обработки батча.</typeparam>
    /// <param name="getBatchFunc">Функция для получения батча.</param>
    /// <param name="isBatchEmptyFunc">Функция, которая возвращает true, если батч пуст.</param>
    /// <param name="batchSizeFunc">Функция, которая возвращает размер батча.</param>
    /// <param name="processBatchAction">Обработчик батча.</param>
    /// <param name="batchSize">Размер батча.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns>Результат обработки батча.</returns>
    public static async Task ProcessBatchesAsync<TObject, TBatch, TResult>(
        Func<int, int, Task<TBatch>> getBatchFunc,
        Func<TBatch, bool> isBatchEmptyFunc,
        Func<TBatch, int>? batchSizeFunc = default,
        Func<TBatch, Task<TResult>>? processBatchAction = default,
        int batchSize = Const.DefaultBatchSize,
        CancellationToken cancellationToken = default)
        where TObject : class
    {
        var processed = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var batch = await getBatchFunc(processed, batchSize);
            if (batchSizeFunc != default)
            {
                processed += batchSizeFunc(batch);
            }

            if (isBatchEmptyFunc(batch))
            {
                break;
            }

            if (processBatchAction != default)
            {
                await processBatchAction(batch);
            }
        }
    }

    /// <summary>
    /// Обрабатывает батчи коллекций с типом <typeparamref name="TObject"/>.
    /// </summary>
    /// <typeparam name="TObject">Тип элементов коллекции.</typeparam>
    /// <typeparam name="TBatch">Тип батча.</typeparam>
    /// <typeparam name="TResult">Тип результата обработки батча.</typeparam>
    /// <param name="getBatchFunc">Функция для получения батча.</param>
    /// <param name="isBatchEmptyFunc">Функция, которая возвращает true, если батч пуст.</param>
    /// <param name="batchSizeFunc">Функция, которая возвращает размер батча.</param>
    /// <param name="processBatchAction">Обработчик батча.</param>
    /// <param name="batchSize">Размер батча.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns>Результат обработки батча.</returns>
    public static async Task ProcessBatchesAsync<TObject, TBatch, TResult>(
        Func<int, int, Task<TBatch>> getBatchFunc,
        Func<TBatch, bool> isBatchEmptyFunc,
        Func<TBatch, int>? batchSizeFunc = default,
        Func<TBatch, Task>? processBatchAction = default,
        int batchSize = Const.DefaultBatchSize,
        CancellationToken cancellationToken = default)
        where TObject : class
    {
        var processed = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var batch = await getBatchFunc(processed, batchSize);
            if (batchSizeFunc != default)
            {
                processed += batchSizeFunc(batch);
            }

            if (isBatchEmptyFunc(batch))
            {
                break;
            }

            if (processBatchAction != default)
            {
                await processBatchAction(batch);
            }
        }
    }

    /// <summary>
    /// Обрабатывает батчи коллекций с типом <typeparamref name="TObject"/>.
    /// </summary>
    /// <typeparam name="TObject">Тип элементов коллекции.</typeparam>
    /// <typeparam name="TResult">Тип результата обработки батча.</typeparam>
    /// <param name="getBatchFunc">Функция для получения батча.</param>
    /// <param name="processBatchAction">Обработчик батча.</param>
    /// <param name="batchSize">Размер батча.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns>Результат обработки батча.</returns>
    public static async IAsyncEnumerable<TResult> ProcessBatchesAsync<TObject, TResult>(
        Func<int, int, Task<ICollection<TObject>>> getBatchFunc,
        Func<ICollection<TObject>, Task<TResult>> processBatchAction,
        int batchSize = Const.DefaultBatchSize,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TObject : class
    {
        var processed = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var batch = await getBatchFunc(processed, batchSize);
            processed += batch.Count;

            if (!batch.Any())
            {
                break;
            }

            yield return await processBatchAction(batch);
        }
    }

    /// <summary>
    /// Обрабатывает батчи коллекций с типом <typeparamref name="TObject"/>.
    /// </summary>
    /// <typeparam name="TObject">Тип элементов коллекции.</typeparam>
    /// <param name="getBatchFunc">Функция для получения батча.</param>
    /// <param name="batchSize">Размер батча.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns>Результат обработки батча.</returns>
    public static IAsyncEnumerable<ICollection<TObject>> ProcessBatchesAsync<TObject>(
        Func<int, int, Task<ICollection<TObject>>> getBatchFunc,
        int batchSize = Const.DefaultBatchSize,
        CancellationToken cancellationToken = default)
        where TObject : class
    {
        return ProcessBatchesAsync(
            getBatchFunc,
            Task.FromResult,
            batchSize,
            cancellationToken);
    }

    /// <summary>
    /// Обрабатывает батчи коллекций с типом <typeparamref name="TObject"/>.
    /// </summary>
    /// <typeparam name="TObject">Тип элементов коллекции.</typeparam>
    /// <typeparam name="TResult">Тип результата обработки батча.</typeparam>
    /// <param name="getBatchFunc">Функция для получения батча.</param>
    /// <param name="isBatchEmptyFunc">Функция, которая возвращает true, если батч пуст.</param>
    /// <param name="processBatchAction">Обработчик батча.</param>
    /// <param name="batchSizeFunc">Функция, которая возвращает размер батча.</param>
    /// <param name="batchSize">Размер батча.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns>Результат обработки батча.</returns>
    public static IAsyncEnumerable<TResult> ProcessBatchesAsync<TObject, TResult>(
        Func<int, int, Task<TResult>> getBatchFunc,
        Func<TResult, bool> isBatchEmptyFunc,
        Func<TResult, Task<TResult>>? processBatchAction = default,
        Func<TResult, int>? batchSizeFunc = default,
        int batchSize = Const.DefaultBatchSize,
        CancellationToken cancellationToken = default)
        where TObject : class
    {
        return ProcessBatchesAsync<TObject, TResult, TResult>(
            getBatchFunc,
            processBatchAction ?? Task.FromResult,
            isBatchEmptyFunc,
            batchSizeFunc,
            batchSize,
            cancellationToken);
    }

    /// <summary>
    /// Обрабатывает батчи коллекций с типом <typeparamref name="TObject"/>.
    /// </summary>
    /// <typeparam name="TObject">Тип элементов коллекции.</typeparam>
    /// <typeparam name="TBatch">Тип батча.</typeparam>
    /// <typeparam name="TResult">Тип результата обработки батча.</typeparam>
    /// <param name="getBatchFunc">Функция для получения батча.</param>
    /// <param name="processBatchAction">Обработчик батча.</param>
    /// <param name="isBatchEmptyFunc">Функция, которая возвращает true, если батч пуст.</param>
    /// <param name="batchSizeFunc">Функция, которая возвращает размер батча.</param>
    /// <param name="batchSize">Размер батча.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns>Результат обработки батча.</returns>
    public static async IAsyncEnumerable<TResult> ProcessBatchesAsync<TObject, TBatch, TResult>(
        Func<int, int, Task<TBatch>> getBatchFunc,
        Func<TBatch, Task<TResult>> processBatchAction,
        Func<TBatch, bool> isBatchEmptyFunc,
        Func<TBatch, int>? batchSizeFunc = default,
        int batchSize = Const.DefaultBatchSize,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TObject : class
    {
        var processed = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var batch = await getBatchFunc(processed, batchSize);
            if (batchSizeFunc != default)
            {
                processed += batchSizeFunc(batch);
            }

            if (isBatchEmptyFunc(batch))
            {
                break;
            }

            yield return await processBatchAction(batch);
        }
    }
}
