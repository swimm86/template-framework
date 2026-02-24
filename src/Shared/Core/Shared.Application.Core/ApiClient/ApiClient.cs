// ----------------------------------------------------------------------------------------------
// <copyright file="ApiClient.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Dto.Interfaces;
using Shared.Application.Core.Dto.Responses;
using Shared.Application.Core.Exceptions;
using Shared.Common.Extensions;
using Shared.Common.Helpers;
using Shared.Domain.Core.Exceptions.Models;
using Shared.Domain.Core.Exceptions.Models.Base;

namespace Shared.Application.Core.ApiClient;

/// <summary>
/// Базовый класс для API-клиента.
/// Предоставляет методы для выполнения HTTP-запросов (GET, POST, PUT, PATCH, DELETE)
/// с поддержкой типизации ответов, обработки ошибок и логирования.
/// </summary>
/// <remarks>
/// <para>
/// Класс предназначен для унификации работы с внешними API. Он предоставляет следующие возможности:
/// </para>
/// <list type="bullet">
/// <item>Выполнение HTTP-запросов с поддержкой токена отмены (<see cref="CancellationToken"/>).</item>
/// <item>Типизация ответов и преобразование JSON в объекты.</item>
/// <item>Обработка ошибок и преобразование их в доменные исключения.</item>
/// <item>Логирование запросов и ответов для отладки.</item>
/// <item>Поддержка отправки файлов через multipart/form-data.</item>
/// </list>
/// <para>
/// Использует <see cref="HttpClient"/> для выполнения запросов и <see cref="ILogger"/> для логирования.
/// </para>
/// </remarks>
/// <param name="clientFactory">Фабрика HTTP-клиентов для создания экземпляра <see cref="HttpClient"/>.</param>
/// <param name="logger">Логгер для записи событий и ошибок.</param>
public abstract class ApiClient(
    IHttpClientFactory clientFactory,
    ILogger<ApiClient> logger)
{
    private readonly string _additionDataKey = nameof(AppException.AdditionalData).ToLowerFirstChar();

    /// <summary>
    /// Выполняет HTTP PUT-запрос по указанному URI.
    /// </summary>
    /// <param name="uri">
    /// Относительный путь к ресурсу, который будет обновлен.
    /// </param>
    /// <param name="content">
    /// Данные, отправляемые в теле запроса.
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>
    /// Ответ сервера в виде <see cref="HttpResponseMessage"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="uri"/> равен <see langword="null"/> или является пустой строкой.
    /// </exception>
    public async Task<HttpResponseMessage> PutAsync(
        string uri,
        object? content = default,
        CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(uri);
        client.BaseAddress = new Uri(client.BaseAddress + uri);
        return await client.PutAsJsonAsync(
            new Uri(string.Empty, UriKind.Relative),
            content,
            cancellationToken);
    }

    /// <summary>
    /// Выполняет HTTP DELETE-запрос по указанному URI.
    /// </summary>
    /// <param name="uri">
    /// Относительный путь к ресурсу, который будет удален.
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Ответ сервера в виде <see cref="HttpResponseMessage"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="uri"/> равен <see langword="null"/> или является пустой строкой.
    /// </exception>
    public async Task<HttpResponseMessage> DeleteAsync(
        string uri,
        CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(uri);
        client.BaseAddress = new Uri(client.BaseAddress + uri);
        return await client.DeleteAsync(
            new Uri(string.Empty, UriKind.Relative),
            cancellationToken);
    }

    /// <summary>
    /// Выполняет GET-запрос к указанному URI.
    /// </summary>
    /// <param name="uri">Относительный путь к ресурсу.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns><see cref="HttpResponseMessage"/> с результатом запроса.</returns>
    /// <exception cref="HttpRequestException">Выбрасывается при ошибках HTTP-запроса.</exception>
    protected async Task<HttpResponseMessage> GetAsync(
        string uri,
        CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(uri);
        client.BaseAddress = new Uri(client.BaseAddress + uri);
        return await client.GetAsync(new Uri(string.Empty, UriKind.Relative), cancellationToken);
    }

    /// <summary>
    /// Выполняет GET-запрос с параметрами запроса и десериализует ответ в указанный тип.
    /// </summary>
    /// <typeparam name="TResult">Тип данных, в который будет десериализован ответ.</typeparam>
    /// <param name="uri">Относительный путь к ресурсу.</param>
    /// <param name="queryParams">Словарь параметров запроса.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Десериализованный объект типа <typeparamref name="TResult"/>.</returns>
    /// <exception cref="HttpRequestException">Выбрасывается при ошибках HTTP-запроса.</exception>
    /// <exception cref="JsonException">Выбрасывается при ошибках десериализации JSON.</exception>
    protected async Task<TResult?> GetAsync<TResult>(
        string uri,
        Dictionary<string, string> queryParams,
        CancellationToken cancellationToken = default)
    {
        return await ResponseAsJsonAsync<TResult>(
            await GetAsync(uri, queryParams, cancellationToken),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Выполняет GET-запрос к указанному URI.
    /// </summary>
    /// <param name="uri">Относительный путь к ресурсу.</param>
    /// <param name="queryParams">Словарь параметров запроса.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns><see cref="HttpResponseMessage"/> с результатом запроса.</returns>
    /// <exception cref="HttpRequestException">Выбрасывается при ошибках HTTP-запроса.</exception>
    /// <exception cref="JsonException">Выбрасывается при ошибках десериализации JSON.</exception>
    protected async Task<HttpResponseMessage> GetAsync(
        string uri,
        Dictionary<string, string> queryParams,
        CancellationToken cancellationToken = default)
    {
        uri = AddQueryParams(uri, queryParams);
        using var client = CreateClient(uri);
        client.BaseAddress = new Uri(client.BaseAddress + uri);
        return await client.GetAsync(new Uri(string.Empty, UriKind.Relative), cancellationToken);
    }

    /// <summary>
    /// Выполняет HTTP POST-запрос с указанным содержимым.
    /// </summary>
    /// <param name="uri">
    /// Относительный путь к ресурсу, на который отправляется запрос.
    /// </param>
    /// <param name="content">
    /// Содержимое запроса в формате <see cref="HttpContent"/>.
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>ы
    /// <returns>
    /// Ответ сервера в виде <see cref="HttpResponseMessage"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="uri"/> равен <see langword="null"/> или является пустой строкой.
    /// </exception>
    protected async Task<HttpResponseMessage> PostAsync(
        string uri,
        HttpContent content,
        CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(uri);
        client.BaseAddress = new Uri(client.BaseAddress + uri);
        return await client.PostAsync(
            new Uri(string.Empty, UriKind.Relative),
            content,
            cancellationToken);
    }

    /// <summary>
    /// Выполняет HTTP POST-запрос с данными в формате JSON.
    /// </summary>
    /// <param name="uri">
    /// Относительный путь к ресурсу, на который отправляется запрос.
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>
    /// Ответ сервера в виде <see cref="HttpResponseMessage"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="uri"/> равен <see langword="null"/> или является пустой строкой.
    /// </exception>
    protected async Task<HttpResponseMessage> PostAsync(
        string uri,
        CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(uri);
        client.BaseAddress = new Uri(client.BaseAddress + uri);
        return await client.PostAsync(new Uri(string.Empty, UriKind.Relative), null, cancellationToken);
    }

    /// <summary>
    /// Выполняет HTTP POST-запрос с данными в формате JSON.
    /// </summary>
    /// <param name="uri">
    /// Относительный путь к ресурсу, на который отправляется запрос.
    /// </param>
    /// <param name="content">
    /// Данные для отправки в теле запроса.
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>
    /// Ответ сервера в виде <see cref="HttpResponseMessage"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="uri"/> равен <see langword="null"/> или является пустой строкой.
    /// </exception>
    protected async Task<HttpResponseMessage> PostAsJsonAsync(
        string uri,
        object? content = default,
        CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(uri);
        client.BaseAddress = new Uri(client.BaseAddress + uri);
        return await client.PostAsJsonAsync(
            new Uri(string.Empty, UriKind.Relative),
            content,
            cancellationToken);
    }

    /// <summary>
    /// Выполняет HTTP POST-запрос без ожидания ответа.
    /// </summary>
    /// <param name="uri">
    /// Относительный путь к ресурсу, на который отправляется запрос.
    /// </param>
    /// <param name="content">
    /// Данные для отправки в теле запроса.
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>
    /// Ответ сервера в виде <see cref="HttpResponseMessage"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="uri"/> равен <see langword="null"/> или является пустой строкой.
    /// </exception>
    protected Task<HttpResponseMessage> PostWithoutResponseAsync(
        string uri,
        object? content = default,
        CancellationToken cancellationToken = default)
    {
        return PostAsJsonAsync(uri, content, cancellationToken);
    }

    /// <summary>
    /// Выполняет типизированный HTTP POST-запрос с данными в формате JSON.
    /// </summary>
    /// <typeparam name="TResult">
    /// Тип данных, в который будет десериализован ответ.
    /// </typeparam>
    /// <param name="uri">
    /// Относительный путь к ресурсу, на который отправляется запрос.
    /// </param>
    /// <param name="content">
    /// Данные для отправки в теле запроса.
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>
    /// Десериализованный объект типа <typeparamref name="TResult"/>.
    /// Если ответ пустой или не может быть десериализован, возвращается <see langword="null"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="uri"/> равен <see langword="null"/> или является пустой строкой.
    /// </exception>
    /// <exception cref="HttpRequestException">
    /// Выбрасывается при ошибках HTTP-запроса.
    /// </exception>
    /// <exception cref="JsonException">
    /// Выбрасывается при ошибках десериализации JSON.
    /// </exception>
    protected async Task<TResult?> PostAsync<TResult>(
        string uri,
        object? content = default,
        CancellationToken cancellationToken = default)
    {
        var result = await ResponseAsJsonAsync<TResult>(
            await PostAsJsonAsync(uri, content, cancellationToken),
            uri,
            content,
            cancellationToken);
        return result;
    }

    /// <summary>
    /// Выполняет типизированный HTTP POST-запрос с данными в формате JSON.
    /// </summary>
    /// <typeparam name="TResult">
    /// Тип данных, в который будет десериализован ответ.
    /// </typeparam>
    /// <param name="uri">
    /// Относительный путь к ресурсу, на который отправляется запрос.
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>
    /// Десериализованный объект типа <typeparamref name="TResult"/>.
    /// Если ответ пустой или не может быть десериализован, возвращается <see langword="null"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="uri"/> равен <see langword="null"/> или является пустой строкой.
    /// </exception>
    /// <exception cref="HttpRequestException">
    /// Выбрасывается при ошибках HTTP-запроса.
    /// </exception>
    /// <exception cref="JsonException">
    /// Выбрасывается при ошибках десериализации JSON.
    /// </exception>
    protected async Task<TResult?> PostAsync<TResult>(
        string uri,
        CancellationToken cancellationToken = default)
    {
        var result = await ResponseAsJsonAsync<TResult>(
            await PostAsync(uri, cancellationToken),
            uri,
            cancellationToken: cancellationToken);
        return result;
    }

    /// <summary>
    /// Выполняет типизированный HTTP PUT-запрос по указанному URI.
    /// </summary>
    /// <typeparam name="TResult">
    /// Тип данных, в который будет десериализован ответ.
    /// </typeparam>
    /// <param name="uri">
    /// Относительный путь к ресурсу, который будет обновлен.
    /// </param>
    /// <param name="content">
    /// Данные, отправляемые в теле запроса.
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>
    /// Десериализованный объект типа <typeparamref name="TResult"/>.
    /// Если ответ пустой или не может быть десериализован, возвращается <see langword="null"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="uri"/> равен <see langword="null"/> или является пустой строкой.
    /// </exception>
    /// <exception cref="JsonException">
    /// Выбрасывается при ошибках десериализации JSON.
    /// </exception>
    protected async Task<TResult?> PutAsync<TResult>(
        string uri,
        object? content = default,
        CancellationToken cancellationToken = default) =>
        await ResponseAsJsonAsync<TResult>(
            await PutAsync(uri, content, cancellationToken),
            uri,
            content,
            cancellationToken);

    /// <summary>
    /// Выполняет HTTP PATCH-запрос по указанному URI.
    /// </summary>
    /// <param name="uri">
    /// Относительный путь к ресурсу, который будет обновлен.
    /// </param>
    /// <param name="content">
    /// Данные, отправляемые в теле запроса.
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>
    /// Ответ сервера в виде <see cref="HttpResponseMessage"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="uri"/> равен <see langword="null"/> или является пустой строкой.
    /// </exception>
    protected async Task<HttpResponseMessage> PatchAsync(
        string uri,
        object? content = default,
        CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(uri);
        client.BaseAddress = new Uri(client.BaseAddress + uri);
        return await client.PatchAsJsonAsync(
            new Uri(string.Empty, UriKind.Relative),
            content,
            cancellationToken);
    }

    /// <summary>
    /// Выполняет типизированный HTTP PATCH-запрос по указанному URI.
    /// </summary>
    /// <typeparam name="TResult">
    /// Тип данных, в который будет десериализован ответ.
    /// </typeparam>
    /// <param name="uri">
    /// Относительный путь к ресурсу, который будет обновлен.
    /// </param>
    /// <param name="content">
    /// Данные, отправляемые в теле запроса.
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>
    /// Десериализованный объект типа <typeparamref name="TResult"/>.
    /// Если ответ пустой или не может быть десериализован, возвращается <see langword="null"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="uri"/> равен <see langword="null"/> или является пустой строкой.
    /// </exception>
    /// <exception cref="JsonException">
    /// Выбрасывается при ошибках десериализации JSON.
    /// </exception>
    protected async Task<TResult?> PatchAsync<TResult>(
        string uri,
        object? content = default,
        CancellationToken cancellationToken = default) =>
        await ResponseAsJsonAsync<TResult>(
            await PatchAsync(uri, content, cancellationToken),
            uri,
            content,
            cancellationToken);

    /// <summary>
    /// Выполняет типизированный HTTP DELETE-запрос по указанному URI.
    /// </summary>
    /// <typeparam name="TResult">
    /// Тип данных, в который будет десериализован ответ.
    /// </typeparam>
    /// <param name="uri">
    /// Относительный путь к ресурсу, который будет удален.
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>
    /// Десериализованный объект типа <typeparamref name="TResult"/>.
    /// Если ответ пустой или не может быть десериализован, возвращается <see langword="null"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="uri"/> равен <see langword="null"/> или является пустой строкой.
    /// </exception>
    /// <exception cref="JsonException">
    /// Выбрасывается при ошибках десериализации JSON.
    /// </exception>
    protected async Task<TResult?> DeleteAsync<TResult>(
        string uri,
        CancellationToken cancellationToken = default) =>
        await ResponseAsJsonAsync<TResult>(
            await DeleteAsync(uri, cancellationToken),
            cancellationToken: cancellationToken);

    /// <summary>
    /// Преобразует HTTP-ответ в объект указанного типа.
    /// </summary>
    /// <typeparam name="TResult">Тип данных, в который будет десериализован ответ.</typeparam>
    /// <param name="httpResponse">HTTP-ответ для десериализации.</param>
    /// <param name="logUri">URI запроса для логирования (опционально).</param>
    /// <param name="logContent">Содержимое запроса для логирования (опционально).</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Десериализованный объект типа <typeparamref name="TResult"/>.</returns>
    /// <exception cref="JsonException">Выбрасывается при ошибках десериализации JSON.</exception>
    protected async Task<TResult?> ResponseAsJsonAsync<TResult>(
        HttpResponseMessage httpResponse,
        string? logUri = null,
        object? logContent = null,
        CancellationToken cancellationToken = default)
    {
        await ValidateResponseAsync(httpResponse, cancellationToken, logUri, logContent);

        var result = await httpResponse.Content
            .ReadFromJsonAsync<TResult>(cancellationToken);
        if (result is ResponseBase response)
        {
            response.StatusCode = (int)httpResponse.StatusCode;
        }

        return result;
    }

    /// <summary>
    /// Валидирует HTTP-ответ и выбрасывает исключение в случае ошибки.
    /// </summary>
    /// <param name="httpResponse">HTTP-ответ для проверки.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <param name="logUri">URI запроса для логирования (опционально).</param>
    /// <param name="logContent">Содержимое запроса для логирования (опционально).</param>
    /// <exception cref="UnauthorizedException">Выбрасывается при статусе 401 Unauthorized.</exception>
    /// <exception cref="ProxiedException">Выбрасывается при наличии детализированных ошибок в ответе.</exception>
    /// <exception cref="Exception">Выбрасывается при других ошибках HTTP.</exception>
    /// <returns>Результат выполнения асинхронной операции.</returns>
    protected async Task ValidateResponseAsync(
        HttpResponseMessage httpResponse,
        CancellationToken cancellationToken,
        string? logUri = null,
        object? logContent = null)
    {
        if (httpResponse.IsSuccessStatusCode)
        {
            return;
        }

        var isThrown = false;
        try
        {
            var absolutePath = httpResponse.RequestMessage?.RequestUri?.AbsolutePath;
            var clientName = GetType().Name;

            var response = await httpResponse.Content
                .ReadAsStringAsync(cancellationToken);
            var problemDetails = JsonHelper.TryDeserialize<ProblemDetails>(response, out var deserializedProblemDetails)
                ? deserializedProblemDetails
                : new ProblemDetails
                {
                    Status = (int)httpResponse.StatusCode,
                    Instance = absolutePath ?? logUri,
                    Detail = response,
                };

            problemDetails!.Extensions.Remove(_additionDataKey);

            if (logUri != null && logContent != null)
            {
                logger.LogError(
                    "Запрос по адресу {RequestUri} вернул ошибку: Status Code={StatusCode:D} с телом {ResponseBody}",
                    absolutePath ?? logUri,
                    httpResponse.StatusCode,
                    JsonSerializer.Serialize(logContent));
            }

            isThrown = true;

            SetProblemDetailsForServerError(problemDetails, absolutePath, clientName);

            JsonHelper.TryDeserialize<ErrorResponse>(response, out var errorResponse);
            throw new ProxiedException(problemDetails, (int)httpResponse.StatusCode, errorResponse?.AdditionalData);
        }
        finally
        {
            if (!isThrown)
            {
                throw await CreateExceptionAsync(httpResponse, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Создает исключение на основе ответа HTTP.
    /// </summary>
    /// <param name="response">Ответ HTTP (<see cref="HttpResponseMessage"/>), который содержит информацию об ошибке.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>
    /// Экземпляр <see cref="Exception"/>, содержащий детализированное сообщение об ошибке.
    /// </returns>
    /// <exception cref="ArgumentNullException">Выбрасывается, если <paramref name="response"/> равен <see langword="null"/>.</exception>
    protected virtual async Task<Exception> CreateExceptionAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        if (response == null)
        {
            throw new ArgumentNullException(nameof(response), "Ответ HTTP не может быть null.");
        }

        try
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var errorMessage = $"Запрос к {GetType().Name} завершился ошибкой: " +
                               $"Status Code={response.StatusCode:D} ({response.ReasonPhrase}), " +
                               $"Content=[{content ?? "null"}]";
            return new Exception(errorMessage);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Произошла ошибка при создании исключения для HTTP-ответа.");

            return new Exception(
                $"Не удалось прочитать содержимое ошибки HTTP. Status Code={response.StatusCode:D} ({response.ReasonPhrase})",
                ex);
        }
    }

    /// <summary>
    /// Типизированный Put запрос.
    /// </summary>
    /// <typeparam name="TContent"> Тип данных. </typeparam>
    /// <typeparam name="TResult"> Тип ответа. </typeparam>
    /// <param name="uri"> Адрес. </param>
    /// <param name="content"> Данные. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <returns> <see cref="HttpResponseMessage"/>. </returns>
    protected async Task<TResult?> PostFileAsync<TContent, TResult>(
        string uri,
        TContent content,
        CancellationToken cancellationToken = default)
        where TContent : IWithFile
        =>
        await ResponseAsJsonAsync<TResult>(
                await PostFilesAsync(uri, content, cancellationToken),
                uri,
                content,
                cancellationToken);

    /// <summary>
    /// Отправляет файл на сервер с использованием multipart/form-data.
    /// </summary>
    /// <param name="url">URL-адрес для отправки запроса.</param>
    /// <param name="request">Объект запроса, содержащий файл и дополнительные параметры.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Ответ сервера в виде <see cref="HttpResponseMessage"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="url"/> или <paramref name="request"/> равны <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если файл в <paramref name="request"/> не содержит данных.
    /// </exception>
    protected async Task<HttpResponseMessage> PostFilesAsync(
        string url,
        IWithFile request,
        CancellationToken cancellationToken = default)
    {
        var boundary = Guid.NewGuid().ToString();
        var multipartContent = new MultipartFormDataContent(boundary);
        multipartContent.Headers.Remove("Content-Type");
        multipartContent.Headers.TryAddWithoutValidation("Content-Type", "multipart/form-data; boundary=" + boundary);

        await using var memoryStream = new MemoryStream();
        await request.File!.CopyToAsync(memoryStream, cancellationToken);

        var byteArrayContent = new ByteArrayContent(memoryStream.ToArray());
        byteArrayContent.Headers.Add("Content-Type", request.File.ContentType);
        multipartContent.Add(byteArrayContent, "file", request.File.FileName);

        foreach (var prop in request.GetType().GetProperties().Where(x => x.Name != nameof(IWithFile.File)))
        {
            multipartContent.Add(new StringContent(prop.GetValue(request)?.ToString()), prop.Name);
        }

        using var client = CreateClient(url);
        client.BaseAddress = new Uri(client.BaseAddress + url);
        return await client.PostAsync(string.Empty, multipartContent, cancellationToken);
    }

    /// <summary>
    /// Проверяет наличие ошибки сервера (статус 500) в массиве ошибок <paramref name="problemDetails"/>
    /// и модифицирует объект <see cref="ProblemDetails"/>, устанавливая значения свойств <see cref="ProblemDetails.Title"/>
    /// и <see cref="ProblemDetails.Detail"/> в случае обнаружения такой ошибки.
    /// </summary>
    /// <remarks>
    /// Метод выполняет следующие действия:
    /// <list type="number">
    /// <item>Проверяет наличие ключа "errors" в коллекции <see cref="ProblemDetails.Extensions"/>.</item>
    /// <item>Если ключ существует и его значение является массивом JSON, перебирает элементы массива.</item>
    /// <item>Ищет элемент с полем "status", равным 500 (статус серверной ошибки).</item>
    /// <item>Если такая ошибка найдена, устанавливает значения <see cref="ProblemDetails.Title"/> и <see cref="ProblemDetails.Detail"/>.</item>
    /// </list>
    /// <para>
    /// Пример структуры данных в <paramref name="problemDetails"/>:
    /// <code>
    /// {
    ///     "title": "An error occurred",
    ///     "status": 500,
    ///     "detail": "An unexpected error occurred.",
    ///     "extensions": {
    ///         "errors": [
    ///             { "status": 500, "message": "Internal server error occurred." },
    ///             { "status": 400, "message": "Bad request." }
    ///         ]
    ///     }
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    /// <param name="problemDetails">
    /// Объект <see cref="ProblemDetails"/>, который будет проверен и модифицирован.
    /// Если в массиве ошибок найден статус 500, свойства <see cref="ProblemDetails.Title"/> и <see cref="ProblemDetails.Detail"/>
    /// будут обновлены соответствующими значениями.
    /// </param>
    /// <param name="absolutePath">
    /// Абсолютный путь запроса, который используется для формирования подробного описания ошибки (<see cref="ProblemDetails.Detail"/>).
    /// </param>
    /// <param name="clientName">
    /// Имя клиента, которое используется для формирования подробного описания ошибки (<see cref="ProblemDetails.Detail"/>).
    /// </param>
    private static void SetProblemDetailsForServerError(
        ProblemDetails problemDetails,
        string absolutePath,
        string clientName)
    {
        if (!problemDetails.Extensions.TryGetValue("errors", out var errorsJsonElement) ||
            errorsJsonElement is not JsonElement { ValueKind: JsonValueKind.Array } errorsArray)
        {
            return;
        }

        var hasServerErrorStatus = errorsArray
            .EnumerateArray()
            .Any(errorElement =>
                errorElement.TryGetProperty("status", out var statusCodeProperty) &&
                statusCodeProperty.TryGetInt32(out var statusCode) &&
                statusCode == StatusCodes.Status500InternalServerError);

        if (!hasServerErrorStatus)
        {
            return;
        }

        problemDetails.Title = "Ошибка во время взаимодействия с внешним сервисом.";
        problemDetails.Detail =
            $"Не удалось корректно обработать запрос по адресу '{absolutePath}' для клиента '{clientName}'.";
    }

    /// <summary>
    /// Добавляет параметры запроса к относительному пути URL.
    /// </summary>
    /// <param name="relativePath">Относительный путь URL.</param>
    /// <param name="queryParams">
    /// Словарь параметров запроса, где ключ — имя параметра, значение — значение параметра.
    /// Может быть <see langword="null"/> или пустым.
    /// </param>
    /// <returns>
    /// URL с добавленными параметрами запроса в формате "?key1=value1&amp;key2=value2".
    /// Если параметры запроса отсутствуют, возвращается исходный <paramref name="relativePath"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="relativePath"/> равен <see langword="null"/>.
    /// </exception>
    private static string AddQueryParams(
        string relativePath,
        Dictionary<string, string>? queryParams)
    {
        if (queryParams == null || queryParams.Count == 0)
        {
            return relativePath;
        }

        var queryString = HttpUtility.ParseQueryString(string.Empty);
        foreach (var param in queryParams)
        {
            queryString[param.Key] = param.Value;
        }

        var query = queryString.ToString();
        return query?.Length > 0
            ? $"{relativePath}?{query}"
            : relativePath;
    }

    private HttpClient CreateClient(string uri)
    {
        var result = clientFactory.CreateClient(GetType().Name);
        return result;
    }
}
