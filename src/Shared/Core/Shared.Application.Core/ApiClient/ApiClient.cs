// ----------------------------------------------------------------------------------------------
// <copyright file="ApiClient.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Net.Http.Json;

namespace Shared.Application.Core.ApiClient;

/// <summary>
/// API-клиент.
/// </summary>
public abstract class ApiClient : IDisposable
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Конструктор API-клиента
    /// </summary>
    /// <param name="clientFactory">Фабрика HTTP-клиентов.</param>
    protected ApiClient(IHttpClientFactory clientFactory)
    {
        _httpClient = clientFactory.CreateClient(GetType().Name);
    }

    /// <summary>
    /// Get запрос.
    /// </summary>
    /// <param name="uri"> Адрес. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <returns> <see cref="HttpResponseMessage"/>. </returns>
    protected  Task<HttpResponseMessage> GetAsync(
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
                await GetAsync(uri, cancellationToken).ConfigureAwait(false),
                cancellationToken)
            .ConfigureAwait(false);

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
                await PostAsJsonAsync(uri, content, cancellationToken).ConfigureAwait(false),
                cancellationToken)
            .ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Put запрос.
    /// </summary>
    /// <typeparam name="TContent"> Тип данных. </typeparam>
    /// <param name="uri"> Адрес. </param>
    /// <param name="content"> Данные. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <returns> <see cref="HttpResponseMessage"/>. </returns>
    protected Task<HttpResponseMessage> PutAsync<TContent>(
        string uri,
        TContent content,
        CancellationToken cancellationToken = default)
    {
        return _httpClient.PutAsJsonAsync(new Uri(uri, UriKind.Relative), content, cancellationToken);
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
                await PutAsync(uri, content, cancellationToken).ConfigureAwait(false),
                cancellationToken)
            .ConfigureAwait(false);

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
                await PatchAsync(uri, content, cancellationToken).ConfigureAwait(false),
                cancellationToken)
            .ConfigureAwait(false);

    /// <summary>
    /// Delete запрос.
    /// </summary>
    /// <param name="uri"> Адрес. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <returns> <see cref="HttpResponseMessage"/>. </returns>
    protected Task<HttpResponseMessage> DeleteAsync(
        string uri,
        CancellationToken cancellationToken = default)
    {
        return _httpClient.DeleteAsync(new Uri(uri, UriKind.Relative), cancellationToken);
    }

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
                await DeleteAsync(uri, cancellationToken).ConfigureAwait(false),
                cancellationToken)
            .ConfigureAwait(false);

    /// <summary>
    /// Преобразование ответа к определенному типу данных.
    /// </summary>
    /// <typeparam name="TResult"> Тип возвращаемых данных. </typeparam>
    /// <param name="httpResponse"> Ответ на запрос. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <returns> Типизированне данные. </returns>
    protected async Task<TResult?> ResponseAsJsonAsync<TResult>(
        HttpResponseMessage httpResponse,
        CancellationToken cancellationToken = default)
    {
        if (httpResponse.IsSuccessStatusCode)
            return await httpResponse.Content.ReadFromJsonAsync<TResult>(cancellationToken)
                .ConfigureAwait(false);

        throw await CreateExceptionAsync(httpResponse, cancellationToken).ConfigureAwait(false);
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
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        return new Exception($"Запрос к {GetType().Name} вернул ошибку: Status Code={response.StatusCode:D} {response.ReasonPhrase} Content=[{content}]");
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
