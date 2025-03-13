// ----------------------------------------------------------------------------------------------
// <copyright file="HttpRequestExtensions.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dto.Requests;
using Shared.Application.Core.Dto.Responses;
using Shared.Common;
using Shared.Common.Helpers;

namespace Shared.Application.Core.Extensions;

/// <summary>
/// Расширение для обработки запросов батчами.
/// </summary>
public static class HttpRequestExtensions
{
    /// <summary>
    /// Обрабатывает запросы батчами.
    /// </summary>
    /// <typeparam name="TRequest">Тип запроса.</typeparam>
    /// <typeparam name="TFilter">Тип фильтра запроса.</typeparam>
    /// <typeparam name="TResponse">Тип ответа.</typeparam>
    /// <typeparam name="TPayload">Тип полезной нагрузки.</typeparam>
    /// <param name="requestFunc">Функция запроса.</param>
    /// <param name="request">Запрос.</param>
    /// <param name="batchSize">Размер батча.</param>
    /// <param name="processFunc">Функция обработки батча.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns><see cref="Task"/>.</returns>
    public static Task BatchProcessAsync<TRequest, TFilter, TResponse, TPayload>(
        this Func<TRequest, CancellationToken, Task<TResponse>> requestFunc,
        TRequest request,
        int batchSize = Const.DefaultBatchSize,
        Func<TResponse, Task>? processFunc = default,
        CancellationToken cancellationToken = default)
        where TRequest : PageableRequest<TFilter>
        where TResponse : PageableResponse<TPayload>
        where TFilter : new()
    {
        var currentPage = 1;
        return BatchHelper.ProcessBatchesAsync<TResponse, TResponse, TResponse>(
            async (_, take) =>
            {
                request.PageNumber = currentPage++;
                request.PageSize = take;
                var page = await requestFunc(request, cancellationToken);
                return page;
            },
            batch => currentPage > batch.TotalPages,
            processBatchAction: processFunc,
            batchSize: batchSize,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Обрабатывает запросы батчами.
    /// </summary>
    /// <typeparam name="TRequest">Тип запроса.</typeparam>
    /// <typeparam name="TFilter">Тип фильтра запроса.</typeparam>
    /// <typeparam name="TResponse">Тип ответа.</typeparam>
    /// <typeparam name="TPayload">Тип полезной нагрузки.</typeparam>
    /// <param name="requestFunc">Функция запроса.</param>
    /// <param name="request">Запрос.</param>
    /// <param name="batchSize">Размер батча.</param>
    /// <param name="processFunc">Функция обработки батча.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns><see cref="Task"/>.</returns>
    public static Task BatchProcessAsync<TRequest, TFilter, TResponse, TPayload>(
        this Func<TRequest, Task<TResponse>> requestFunc,
        TRequest request,
        int batchSize = Const.DefaultBatchSize,
        Func<TResponse, Task>? processFunc = default,
        CancellationToken cancellationToken = default)
        where TRequest : PageableRequest<TFilter>
        where TResponse : PageableResponse<TPayload>
        where TFilter : new()
    {
        var currentPage = 1;
        return BatchHelper.ProcessBatchesAsync<TResponse, TResponse, TResponse>(
            async (_, take) =>
            {
                request.PageNumber = currentPage++;
                request.PageSize = take;
                var page = await requestFunc(request);
                return page;
            },
            batch => currentPage > batch.TotalPages,
            processBatchAction: processFunc,
            batchSize: batchSize,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Обрабатывает запросы батчами.
    /// </summary>
    /// <typeparam name="TRequest">Тип запроса.</typeparam>
    /// <typeparam name="TFilter">Тип фильтра запроса.</typeparam>
    /// <typeparam name="TResponse">Тип ответа.</typeparam>
    /// <typeparam name="TPayload">Тип полезной нагрузки.</typeparam>
    /// <param name="requestFunc">Функция запроса.</param>
    /// <param name="request">Запрос.</param>
    /// <param name="batchSize">Размер батча.</param>
    /// <param name="processFunc">Функция обработки батча.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns><see cref="Task"/>.</returns>
    public static IAsyncEnumerable<TResponse> BatchProcessAsync<TRequest, TFilter, TResponse, TPayload>(
        this Func<TRequest, Task<TResponse>> requestFunc,
        TRequest request,
        int batchSize = Const.DefaultBatchSize,
        Func<TResponse, Task<TResponse>>? processFunc = default,
        CancellationToken cancellationToken = default)
        where TRequest : PageableRequest<TFilter>
        where TResponse : PageableResponse<TPayload>
        where TFilter : new()
    {
        var currentPage = 1;
        return BatchHelper.ProcessBatchesAsync<TResponse, TResponse>(
            async (_, take) =>
            {
                request.PageNumber = currentPage++;
                request.PageSize = take;
                var page = await requestFunc(request);
                return page;
            },
            batch => currentPage > batch.TotalPages,
            processFunc,
            batchSize: batchSize,
            cancellationToken: cancellationToken);
    }
}
