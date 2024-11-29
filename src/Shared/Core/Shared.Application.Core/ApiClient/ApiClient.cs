// ----------------------------------------------------------------------------------------------
// <copyright file="ApiClient.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Dto.Interfaces;
using Shared.Application.Core.Dto.Responses;
using Shared.Application.Core.Exceptions;
using Shared.Domain.Core.Exceptions.Models;

namespace Shared.Application.Core.ApiClient;

/// <summary>
/// API-клиент.
/// </summary>
public abstract class ApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiClient> _logger;

    /// <summary>
    /// Конструктор API-клиента
    /// </summary>
    /// <param name="clientFactory">Фабрика HTTP-клиентов.</param>
    /// <param name="logger">Логгер.</param>
    protected ApiClient(IHttpClientFactory clientFactory, ILogger<ApiClient> logger)
    {
        _httpClient = clientFactory.CreateClient(GetType().Name);
        _logger = logger;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _httpClient.Dispose();
    }

    /// <summary>
    /// Put запрос.
    /// </summary>
    /// <typeparam name="TContent"> Тип данных. </typeparam>
    /// <param name="uri"> Адрес. </param>
    /// <param name="content"> Данные. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <returns> <see cref="HttpResponseMessage"/>. </returns>
    public Task<HttpResponseMessage> PutAsync<TContent>(
        string uri,
        TContent content,
        CancellationToken cancellationToken = default)
    {
        return _httpClient.PutAsJsonAsync(new Uri(uri, UriKind.Relative), content, cancellationToken);
    }

    /// <summary>
    /// Delete запрос.
    /// </summary>
    /// <param name="uri"> Адрес. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <returns> <see cref="HttpResponseMessage"/>. </returns>
    public Task<HttpResponseMessage> DeleteAsync(
        string uri,
        CancellationToken cancellationToken = default)
    {
        return _httpClient.DeleteAsync(new Uri(uri, UriKind.Relative), cancellationToken);
    }

    /// <summary>
    /// Get запрос.
    /// </summary>
    /// <param name="uri"> Адрес. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <returns> <see cref="HttpResponseMessage"/>. </returns>
    protected Task<HttpResponseMessage> GetAsync(
        string uri,
        CancellationToken cancellationToken = default)
    {
        return _httpClient.GetAsync(new Uri(uri, UriKind.Relative), cancellationToken);
    }

    /// <summary>
    /// Типизированный Get запрос.
    /// </summary>
    /// <typeparam name="TResult"> Тип ответа. </typeparam>
    /// <param name="uri"> Адрес. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <returns> Типизированный ответ. </returns>
    protected async Task<TResult?> GetAsync<TResult>(
        string uri,
        CancellationToken cancellationToken = default) =>
        await ResponseAsJsonAsync<TResult>(
                await GetAsync(uri, cancellationToken),
                cancellationToken);

    /// <summary>
    /// Post запрос.
    /// </summary>
    /// <param name="uri"> Адрес. </param>
    /// <param name="content"> Данные. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <returns> <see cref="HttpResponseMessage"/>. </returns>
    protected Task<HttpResponseMessage> PostAsync(
        string uri,
        HttpContent content,
        CancellationToken cancellationToken = default)
    {
        return _httpClient.PostAsync(new Uri(uri, UriKind.Relative), content, cancellationToken);
    }

    /// <summary>
    /// Post запрос.
    /// </summary>
    /// <typeparam name="TContent"> Тип данных. </typeparam>
    /// <param name="uri"> Адрес. </param>
    /// <param name="content"> Данные. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <returns> <see cref="HttpResponseMessage"/>. </returns>
    protected Task<HttpResponseMessage> PostAsJsonAsync<TContent>(
        string uri,
        TContent content,
        CancellationToken cancellationToken = default)
    {
        return _httpClient.PostAsJsonAsync(new Uri(uri, UriKind.Relative), content, cancellationToken);
    }

    /// <summary>
    /// Нетипизированный Post запрос.
    /// </summary>
    /// <param name="uri"> Адрес. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <returns> <see cref="HttpResponseMessage"/>. </returns>
    protected Task<HttpResponseMessage> PostWithoutContentAsync(
        string uri,
        CancellationToken cancellationToken = default)
    {
        return _httpClient.PostAsync(new Uri(uri, UriKind.Relative), null, cancellationToken);
    }

    /// <summary>
    /// Типизированный Post запрос.
    /// </summary>
    /// <typeparam name="TContent"> Тип данных. </typeparam>
    /// <param name="uri"> Адрес. </param>
    /// <param name="content"> Данные. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <returns> <see cref="HttpResponseMessage"/>. </returns>
    protected Task<HttpResponseMessage> PostWithoutResponseAsync<TContent>(
        string uri,
        TContent content,
        CancellationToken cancellationToken = default)
    {
        return PostAsJsonAsync(uri, content, cancellationToken);
    }

    /// <summary>
    /// Типизированный Post запрос.
    /// </summary>
    /// <typeparam name="TContent"> Тип данных. </typeparam>
    /// <typeparam name="TResult"> Тип ответа. </typeparam>
    /// <param name="uri"> Адрес. </param>
    /// <param name="content"> Данные. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <returns> <see cref="HttpResponseMessage"/>. </returns>
    protected async Task<TResult?> PostAsync<TContent, TResult>(
        string uri,
        TContent content,
        CancellationToken cancellationToken = default)
    {
        var result = await ResponseAsJsonAsync<TResult>(
                await PostAsJsonAsync(uri, content, cancellationToken),
                cancellationToken,
                uri,
                content);
        return result;
    }

    /// <summary>
    /// Нетипизированный Post запрос.
    /// </summary>
    /// <typeparam name="TResult"> Тип ответа. </typeparam>
    /// <param name="uri"> Адрес. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <returns> <see cref="HttpResponseMessage"/>. </returns>
    protected async Task<TResult?> PostWithoutContentAsync<TResult>(
        string uri,
        CancellationToken cancellationToken = default)
    {
        var result = await ResponseAsJsonAsync<TResult>(
                await PostWithoutContentAsync(uri, cancellationToken),
                cancellationToken);
        return result;
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
    protected async Task<TResult?> PutAsync<TContent, TResult>(
        string uri,
        TContent content,
        CancellationToken cancellationToken = default) =>
        await ResponseAsJsonAsync<TResult>(
                await PutAsync(uri, content, cancellationToken),
                cancellationToken,
                uri,
                content);

    /// <summary>
    /// Patch запрос.
    /// </summary>
    /// <typeparam name="TContent"> Тип данных. </typeparam>
    /// <param name="uri"> Адрес. </param>
    /// <param name="content"> Данные. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <returns> <see cref="HttpResponseMessage"/>. </returns>
    protected Task<HttpResponseMessage> PatchAsync<TContent>(
        string uri,
        TContent content,
        CancellationToken cancellationToken = default)
    {
        return _httpClient.PatchAsJsonAsync(new Uri(uri, UriKind.Relative), content, cancellationToken);
    }

    /// <summary>
    /// Типизированный Patch запрос.
    /// </summary>
    /// <typeparam name="TContent"> Тип данных. </typeparam>
    /// <typeparam name="TResult"> Тип ответа. </typeparam>
    /// <param name="uri"> Адрес. </param>
    /// <param name="content"> Данные. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <returns> <see cref="HttpResponseMessage"/>. </returns>
    protected async Task<TResult?> PatchAsync<TContent, TResult>(
        string uri,
        TContent content,
        CancellationToken cancellationToken = default) =>
        await ResponseAsJsonAsync<TResult>(
                await PatchAsync(uri, content, cancellationToken),
                cancellationToken,
                uri,
                content);

    /// <summary>
    /// Типизированный Delete запрос.
    /// </summary>
    /// <typeparam name="TResult"> Тип ответа. </typeparam>
    /// <param name="uri"> Адрес. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <returns> <see cref="HttpResponseMessage"/>. </returns>
    protected async Task<TResult?> DeleteAsync<TResult>(
        string uri,
        CancellationToken cancellationToken = default) =>
        await ResponseAsJsonAsync<TResult>(
                await DeleteAsync(uri, cancellationToken),
                cancellationToken);

    /// <summary>
    /// Преобразование ответа к определенному типу данных.
    /// </summary>
    /// <typeparam name="TResult"> Тип возвращаемых данных. </typeparam>
    /// <param name="httpResponse"> Ответ на запрос. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <param name="logUri"> URI запроса для логирования, если требуется. </param>
    /// <param name="logContent"> Содержимое запроса для логирования, если требуется. </param>
    /// <returns> Типизированне данные. </returns>
    protected async Task<TResult?> ResponseAsJsonAsync<TResult>(
        HttpResponseMessage httpResponse,
        CancellationToken cancellationToken = default,
        string? logUri = null,
        object? logContent = null)
    {
        if (!httpResponse.IsSuccessStatusCode)
        {
            var isThrown = false;
            try
            {
                var absolutePath = httpResponse.RequestMessage?.RequestUri?.AbsolutePath.ToString();
                var clientName = GetType().Name;

                if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
                    isThrown = true;

                    var context = new ClientRequestContext(clientName, absolutePath);

                    throw new UnauthorizedException(context);
                }

                var problemDetails = await httpResponse.Content
                    .ReadFromJsonAsync<ProblemDetails>(cancellationToken);

                if (problemDetails is not null)
                {
                    if (logUri != null && logContent != null)
                    {
                        _logger.LogError($"Запрос по адресу {logUri} вернул ошибку: Status Code={httpResponse.StatusCode:D} с телом {JsonSerializer.Serialize(logContent)}");
                    }

                    isThrown = true;

                    SetProblemDetailsForServerError(problemDetails, absolutePath, clientName);

                    throw new ProxiedException(problemDetails, (int)httpResponse.StatusCode);
                }
            }
            finally
            {
                if (!isThrown)
                {
                    throw await CreateExceptionAsync(httpResponse, cancellationToken);
                }
            }
        }

        var result = await httpResponse.Content
            .ReadFromJsonAsync<TResult>(cancellationToken);
        if (result is ResponseBase response)
        {
            response.StatusCode = (int)httpResponse.StatusCode;
        }

        return result;
    }

    /// <summary>
    /// Создание ошибки.
    /// </summary>
    /// <param name="response"> Ответ на запрос. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <returns> Типизированне данные. </returns>
    protected virtual async Task<Exception> CreateExceptionAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        return new Exception($"Запрос к {GetType().Name} вернул ошибку: Status Code={response.StatusCode:D} {response.ReasonPhrase} Content=[{content}]");
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
                cancellationToken,
                uri,
                content);

    /// <summary>
    /// Отправить файл
    /// </summary>
    /// <param name="url">Урл.</param>
    /// <param name="request">Запрос.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns><see cref="HttpResponseMessage"/>.</returns>
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
        await request.File.CopyToAsync(memoryStream, cancellationToken);

        var byteArrayContent = new ByteArrayContent(memoryStream.ToArray());
        byteArrayContent.Headers.Add("Content-Type", request.File.ContentType);
        multipartContent.Add(byteArrayContent, "file", request.File.FileName);

        foreach (var prop in request.GetType().GetProperties().Where(x => x.Name != nameof(IWithFile.File)))
        {
            var value = prop.GetValue(request);
            if (value is not null)
            {
                multipartContent.Add(new StringContent(value.ToString()), prop.Name);
            }
        }

        return await _httpClient.PostAsync(url, multipartContent, cancellationToken);
    }

    /// <summary>
    /// Проверяет наличие ошибки сервера в массиве ошибок и устанавливает соответствующие значения Title и Detail.
    /// </summary>
    /// <param name="problemDetails">Объект <see cref="ProblemDetails"/>, который будет модифицирован при обнаружении статуса 500.</param>
    /// <param name="absolutePath">Абсолютный путь, к которому выполнялся запрос.</param>
    /// <param name="clientName">Имя клиента.</param>
    private void SetProblemDetailsForServerError(
        ProblemDetails problemDetails,
        string absolutePath,
        string clientName)
    {
        if (problemDetails.Extensions.TryGetValue("errors", out var errorsJsonElement) &&
                        errorsJsonElement is JsonElement errorsArray &&
                        errorsArray.ValueKind == JsonValueKind.Array)
        {
            var hasServerErrorStatus = errorsArray.EnumerateArray()
                .Any(errorElement => errorElement.TryGetProperty("status", out var statusCodeProperty) &&
                        statusCodeProperty.TryGetInt32(out var statusCode) &&
                        statusCode == 500);

            if (hasServerErrorStatus)
            {
                problemDetails.Title = "Ошибка во время взаимодействия с внешним сервисом.";
                problemDetails.Detail = $"Не удалось корректно обработать запрос по адресу '{absolutePath}' для клиента '{clientName}'.";
            }
        }
    }
}
