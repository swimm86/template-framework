// ----------------------------------------------------------------------------------------------
// <copyright file="HttpRequestExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Batch.Http.RetryPolicy.Interfaces;
using Shared.Application.Core.Dto.Requests;
using Shared.Application.Core.Dto.Responses;
using Shared.Common.Batch;
using Shared.Common.Extensions;

namespace Shared.Application.Core.Batch.Http.Extensions;

/// <summary>
/// Расширения для обработки HTTP-запросов с поддержкой пакетной (батчевой) обработки данных.
/// </summary>
/// <remarks>
/// <para>
/// Данный класс предоставляет методы расширения для автоматической обработки постраничных запросов
/// с использованием батчевой обработки. Это особенно полезно при работе с большими объемами данных,
/// когда необходимо обработать все страницы результатов без ручного управления пагинацией.
/// </para>
/// <para>
/// Основные возможности:
/// <list type="bullet">
/// <item><description>Автоматическая обработка всех страниц постраничного запроса</description></item>
/// <item><description>Поддержка различных типов функций запросов (с CancellationToken и без)</description></item>
/// <item><description>Возможность обработки каждого батча через пользовательскую функцию</description></item>
/// <item><description>Поддержка асинхронных потоков данных (<see cref="IAsyncEnumerable{T}"/>), см. методы <c>BatchSelectPagesAsync</c></description></item>
/// <item><description>Настраиваемый размер батча (по умолчанию — <see cref="Shared.Common.Batch.Constants.DefaultBatchSize"/>)</description></item>
/// <item><description>После обхода в переданном <see cref="PageableRequest"/> сохраняются последние присвоенные во время итерации <see cref="PageableRequest.PageNumber"/> и <see cref="PageableRequest.PageSize"/> (восстановление исходных значений не выполняется)</description></item>
/// </list>
/// </para>
/// <para>
/// Все методы используют <see cref="Shared.Common.Batch.BatchHelper"/> для эффективной обработки данных по частям,
/// что помогает избежать перегрузки памяти при работе с большими коллекциями.
/// </para>
/// <para>
/// Во время итерации номер и размер страницы в переданном <see cref="PageableRequest"/> меняются;
/// не используйте один и тот же объект запроса параллельно из нескольких задач.
/// </para>
/// <para>
/// Метод <see cref="BatchSelectPagesAsync{TRequest, TResponse, TPayload}(Func{TRequest, CancellationToken, Task{TResponse}}, TRequest, IHttpBatchRetryPolicy?, CancellationToken)"/>
/// начинает нумерацию страниц с <see cref="PageableRequest.PageNumber"/> текущего запроса; значение должно быть не меньше 1.
/// </para>
/// </remarks>
public static class HttpRequestExtensions
{
    /// <summary>
    /// Обрабатывает постраничные запросы батчами.
    /// </summary>
    /// <param name="requestFunc">Функция запроса: принимает запрос, возвращает ответ.</param>
    /// <inheritdoc cref="BatchProcessAsync{TRequest,TResponse,TPayload}(Func{TRequest,CancellationToken,Task{TResponse}},TRequest,Func{TResponse,Task}?,IHttpBatchRetryPolicy?,CancellationToken)" />
    /// <param name="request"/><param name="processFunc"/><param name="pageRetryPolicy"/><param name="cancellationToken"/><returns/>
    public static Task BatchProcessAsync<TRequest, TResponse, TPayload>(
        this Func<TRequest, Task<TResponse>> requestFunc,
        TRequest request,
        Func<TResponse, Task>? processFunc = null,
        IHttpBatchRetryPolicy? pageRetryPolicy = null,
        CancellationToken cancellationToken = default)
        where TRequest : PageableRequest
        where TResponse : PageableResponse<TPayload>
    {
        ArgumentNullException.ThrowIfNull(requestFunc);
        ArgumentNullException.ThrowIfNull(request);
        return BatchProcessAsync<TRequest, TResponse, TPayload>(
            (req, _) => requestFunc(req),
            request,
            processFunc,
            pageRetryPolicy,
            cancellationToken);
    }

    /// <summary>
    /// Обрабатывает постраничные запросы батчами с поддержкой <see cref="CancellationToken"/>.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции обхода.</param>
    /// <returns>Задача, представляющая асинхронную операцию обработки всех страниц.</returns>
    /// <inheritdoc cref="BatchSelectPagesAsync{TRequest,TResponse,TPayload}(Func{TRequest,CancellationToken,Task{TResponse}},TRequest,IHttpBatchRetryPolicy?,CancellationToken)" />
    /// <param name="requestFunc"/><param name="request"/><param name="pageRetryPolicy"/>
    /// <param name="processFunc">Опциональная функция для обработки каждого батча.</param>
    public static Task BatchProcessAsync<TRequest, TResponse, TPayload>(
        this Func<TRequest, CancellationToken, Task<TResponse>> requestFunc,
        TRequest request,
        Func<TResponse, Task>? processFunc = null,
        IHttpBatchRetryPolicy? pageRetryPolicy = null,
        CancellationToken cancellationToken = default)
        where TRequest : PageableRequest
        where TResponse : PageableResponse<TPayload>
    {
        ArgumentNullException.ThrowIfNull(requestFunc);
        ArgumentNullException.ThrowIfNull(request);
        return BatchSelectPagesAsync<TRequest, TResponse, TPayload>(
                requestFunc,
                request,
                pageRetryPolicy,
                cancellationToken)
            .ForEachAsync(
                async response =>
                {
                    await (processFunc?.Invoke(response) ?? Task.CompletedTask);
                },
                cancellationToken);
    }

    /// <summary>
    /// Возвращает асинхронный поток страниц.
    /// </summary>
    /// <param name="requestFunc">Функция запроса: принимает запрос, возвращает ответ.</param>
    /// <inheritdoc cref="BatchSelectPagesAsync{TRequest,TResponse,TPayload}(Func{TRequest,CancellationToken,Task{TResponse}},TRequest,IHttpBatchRetryPolicy?,CancellationToken)" />
    /// <param name="request"/><param name="pageRetryPolicy"/><param name="cancellationToken"/>
    public static IAsyncEnumerable<TResponse> BatchSelectPagesAsync<TRequest, TResponse, TPayload>(
        this Func<TRequest, Task<TResponse>> requestFunc,
        TRequest request,
        IHttpBatchRetryPolicy? pageRetryPolicy = null,
        CancellationToken cancellationToken = default)
        where TRequest : PageableRequest
        where TResponse : PageableResponse<TPayload>
    {
        ArgumentNullException.ThrowIfNull(requestFunc);
        ArgumentNullException.ThrowIfNull(request);
        return BatchSelectPagesAsync<TRequest, TResponse, TPayload>(
            (req, _) => requestFunc(req),
            request,
            pageRetryPolicy,
            cancellationToken);
    }

    /// <summary>
    /// Возвращает асинхронный поток страниц с передачей <see cref="CancellationToken"/> в делегат запроса.
    /// </summary>
    /// <typeparam name="TRequest">Тип запроса, наследник <see cref="PageableRequest"/>.</typeparam>
    /// <typeparam name="TResponse">Тип ответа-страницы <see cref="PageableResponse{TPayload}"/>.</typeparam>
    /// <typeparam name="TPayload">Тип полезной нагрузки.</typeparam>
    /// <param name="requestFunc">Функция запроса: принимает запрос, <see cref="CancellationToken"/>, возвращает ответ.</param>
    /// <param name="request">Запрос с пагинацией.</param>
    /// <param name="pageRetryPolicy">Политика повторов для каждого запроса страницы; <see langword="null"/> — без повторов.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены перечисления.</param>
    /// <returns>Асинхронный поток страниц.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="requestFunc"/> или <paramref name="request"/> равны null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Некорректный <see cref="PageableRequest.PageSize"/> или <see cref="PageableRequest.PageNumber"/> в <paramref name="request"/>.</exception>
    /// <remarks>
    /// Имя метода подчёркивает семантику «постраничная выборка» и отличает API от потоковых методов <see cref="BatchHelper"/>,
    /// где номер страницы выводится из пар <c>skip</c>/<c>take</c>.
    /// </remarks>
    public static IAsyncEnumerable<TResponse> BatchSelectPagesAsync<TRequest, TResponse, TPayload>(
        this Func<TRequest, CancellationToken, Task<TResponse>> requestFunc,
        TRequest request,
        IHttpBatchRetryPolicy? pageRetryPolicy = null,
        CancellationToken cancellationToken = default)
        where TRequest : PageableRequest
        where TResponse : PageableResponse<TPayload>
    {
        ArgumentNullException.ThrowIfNull(requestFunc);
        ArgumentNullException.ThrowIfNull(request);

        if (request.PageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                request.PageNumber,
                "Page number must be a positive number.");
        }

        if (request.PageSize < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                request.PageNumber,
                "Page size must be a positive number.");
        }

        var pagesFetched = 0;
        var totalPages = 0;
        var currentPage = request.PageNumber > 0 ? request.PageNumber - 1 : 0;
        return BatchHelper.BatchSelectAsync<TResponse>(
            getBatchFunc: async (_, take) =>
            {
                request.PageNumber = ++currentPage;
                request.PageSize = take;
                var response = pageRetryPolicy is null
                    ? await requestFunc(request, cancellationToken)
                    : await pageRetryPolicy.ExecuteAsync(ct => requestFunc(request, ct), cancellationToken);
                totalPages = response.TotalPages;
                pagesFetched++;
                return response;
            },
            isBatchEmptyFunc: response => currentPage > response.TotalPages,
            batchSize: request.PageSize,
            // Так как currentPage обновляется при каждой выборке, предикат завершения опирается на последний известный номер страницы и TotalPages.
            isNeedToBreakFunc: () => pagesFetched > 0 && currentPage >= totalPages,
            cancellationToken: cancellationToken);
    }
}
