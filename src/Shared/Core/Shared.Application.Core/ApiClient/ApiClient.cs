// ----------------------------------------------------------------------------------------------
// <copyright file="ApiClient.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Shared.Application.Core.ApiClient.Validators.Interfaces;
using Shared.Application.Core.Dto.Interfaces;
using Shared.Domain.Core.Utils.Interfaces;

namespace Shared.Application.Core.ApiClient;

/// <summary>
/// Базовый класс для API-клиента.
/// Предоставляет методы для выполнения HTTP-запросов (GET, POST, PUT, PATCH, DELETE)
/// с поддержкой типизации ответов, обработки ошибок и логирования.
/// </summary>
public abstract partial class ApiClient
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertiesCache = new();

    private readonly string _typeName;
    private readonly HttpClient _httpClient;
    private readonly IUriValidator _uriValidator;
    private readonly IResponseValidator _responseValidator;
    private readonly IPropertyGetter _propertyGetter;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="ApiClient"/>.
    /// </summary>
    /// <param name="clientFactory">Фабрика HTTP-клиентов для создания экземпляра <see cref="HttpClient"/>.</param>
    /// <param name="uriValidator"><see cref="IUriValidator"/>.</param>
    /// <param name="responseValidator"><see cref="IResponseValidator"/>.</param>
    /// <param name="propertyGetter">Извлекает значения свойств для multipart-запросов.</param>
    protected ApiClient(
        IHttpClientFactory clientFactory,
        IUriValidator uriValidator,
        IResponseValidator responseValidator,
        IPropertyGetter propertyGetter)
    {
        _typeName = GetType().FullName!;
        _httpClient = clientFactory.CreateClient(_typeName);
        _uriValidator = uriValidator;
        _responseValidator = responseValidator;
        _propertyGetter = propertyGetter;
    }

    /// <summary>
    /// Выполняет GET-запрос к указанному URI.
    /// </summary>
    /// <param name="uri">Относительный путь к ресурсу.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns><see cref="HttpResponseMessage"/> с результатом запроса.</returns>
    protected Task<HttpResponseMessage> GetAsync(
        string uri,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            HttpMethod.Get,
            uri,
            cancellationToken);
    }

    /// <summary>
    /// Выполняет GET-запрос к указанному URI с параметрами запроса.
    /// </summary>
    /// <param name="uri">Относительный путь к ресурсу.</param>
    /// <param name="queryParams">Словарь параметров запроса.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns><see cref="HttpResponseMessage"/> с результатом запроса.</returns>
    protected Task<HttpResponseMessage> GetAsync(
        string uri,
        Dictionary<string, string> queryParams,
        CancellationToken cancellationToken = default)
    {
        uri = AddQueryParams(uri, queryParams);
        return SendAsync(
            HttpMethod.Get,
            uri,
            cancellationToken);
    }

    /// <summary>
    /// Выполняет HTTP POST-запрос с указанным содержимым.
    /// </summary>
    /// <param name="uri">Относительный путь к ресурсу.</param>
    /// <param name="content">Содержимое запроса.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Ответ сервера в виде <see cref="HttpResponseMessage"/>.</returns>
    /// <remarks>
    /// Ответственность за утилизацию <paramref name="content"/> остаётся за вызывающим:
    /// метод не вызывает <see cref="IDisposable.Dispose"/> для переданного содержимого
    /// ни при успешной отправке, ни при возникновении исключения.
    /// </remarks>
    protected Task<HttpResponseMessage> PostAsync(
        string uri,
        HttpContent content,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            HttpMethod.Post,
            uri,
            content,
            cancellationToken);
    }

    /// <summary>
    /// Выполняет HTTP POST-запрос без тела.
    /// </summary>
    /// <param name="uri">Относительный путь к ресурсу.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Ответ сервера в виде <see cref="HttpResponseMessage"/>.</returns>
    protected Task<HttpResponseMessage> PostAsync(
        string uri,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            HttpMethod.Post,
            uri,
            cancellationToken);
    }

    /// <summary>
    /// Выполняет HTTP POST-запрос с данными в формате JSON.
    /// </summary>
    /// <param name="uri">Относительный путь к ресурсу.</param>
    /// <param name="content">Данные для отправки в теле запроса.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Ответ сервера в виде <see cref="HttpResponseMessage"/>.</returns>
    protected Task<HttpResponseMessage> PostAsJsonAsync(
        string uri,
        object? content = null,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            HttpMethod.Post,
            uri,
            content,
            cancellationToken);
    }

    /// <summary>
    /// Отправляет файл на сервер с использованием multipart/form-data.
    /// </summary>
    /// <param name="url">URL-адрес для отправки запроса.</param>
    /// <param name="request">Объект запроса, содержащий файл и дополнительные параметры.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Ответ сервера в виде <see cref="HttpResponseMessage"/>.</returns>
    protected async Task<HttpResponseMessage> PostFilesAsync(
        string url,
        IWithFile request,
        CancellationToken cancellationToken = default)
    {
        var boundary = Guid.NewGuid().ToString();
        using var multipartContent = new MultipartFormDataContent(boundary);
        multipartContent.Headers.Remove("Content-Type");
        multipartContent.Headers.TryAddWithoutValidation(
            "Content-Type",
            "multipart/form-data; boundary=" + boundary);

        var streamContent = new StreamContent(request.File!.OpenReadStream());
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(request.File.ContentType);
        multipartContent.Add(streamContent, "file", request.File.FileName);

        var properties = PropertiesCache.GetOrAdd(
            request.GetType(),
            t => t.GetProperties().Where(x => x.Name != nameof(IWithFile.File)).ToArray());
        foreach (var prop in properties)
        {
            multipartContent.Add(new StringContent(_propertyGetter.GetPropertyAsString(request, prop.Name)));
        }

        return await SendAsync(
            HttpMethod.Post,
            url,
            multipartContent,
            cancellationToken);
    }

    /// <summary>
    /// Выполняет HTTP PUT-запрос по указанному URI.
    /// </summary>
    /// <param name="uri">Относительный путь к ресурсу, который будет обновлен.</param>
    /// <param name="content">Данные, отправляемые в теле запроса.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Ответ сервера в виде <see cref="HttpResponseMessage"/>.</returns>
    protected Task<HttpResponseMessage> PutAsync(
        string uri,
        object? content = null,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            HttpMethod.Put,
            uri,
            content,
            cancellationToken);
    }

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
    protected Task<HttpResponseMessage> PatchAsync(
        string uri,
        object? content = null,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            HttpMethod.Patch,
            uri,
            content,
            cancellationToken);
    }

    /// <summary>
    /// Выполняет HTTP DELETE-запрос по указанному URI.
    /// </summary>
    /// <param name="uri">Относительный путь к ресурсу, который будет удален.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Ответ сервера в виде <see cref="HttpResponseMessage"/>.</returns>
    protected Task<HttpResponseMessage> DeleteAsync(
        string uri,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            HttpMethod.Delete,
            uri,
            cancellationToken);
    }

    /// <summary>
    /// Добавляет параметры запроса к относительному пути URL.
    /// </summary>
    /// <param name="relativePath">Относительный путь URL.</param>
    /// <param name="queryParams">Словарь параметров запроса.</param>
    /// <returns>URL с добавленными параметрами запроса.</returns>
    private static string AddQueryParams(
        string relativePath,
        Dictionary<string, string>? queryParams)
    {
        return queryParams?.Any() == true
            ? relativePath + QueryString.Create(queryParams)
            : relativePath;
    }

    private Task<HttpResponseMessage> SendAsync(
        HttpMethod httpMethod,
        string uri,
        CancellationToken cancellationToken)
    {
        return SendAsync(
            httpMethod,
            uri,
            httpContent: null,
            validateUri: true,
            cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod httpMethod,
        string uri,
        object? content,
        CancellationToken cancellationToken)
    {
        // JsonContent создаётся только после валидации uri
        _uriValidator.Validate(uri);
        using var jsonContent = JsonContent.Create(content, mediaType: null);
        return await SendAsync(
            httpMethod,
            uri,
            jsonContent,
            validateUri: false,
            cancellationToken);
    }

    private Task<HttpResponseMessage> SendAsync(
        HttpMethod httpMethod,
        string uri,
        HttpContent? httpContent,
        CancellationToken cancellationToken)
    {
        return SendAsync(
            httpMethod,
            uri,
            httpContent,
            validateUri: true,
            cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod httpMethod,
        string uri,
        HttpContent? httpContent,
        bool validateUri,
        CancellationToken cancellationToken)
    {
        if (validateUri)
        {
            _uriValidator.Validate(uri);
        }

        using var request = new HttpRequestMessage(httpMethod, uri);
        request.Content = httpContent;

        try
        {
            return await _httpClient.SendAsync(request, cancellationToken);
        }
        finally
        {
            request.Content = null;
        }
    }
}
