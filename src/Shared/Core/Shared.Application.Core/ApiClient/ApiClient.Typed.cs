// ----------------------------------------------------------------------------------------------
// <copyright file="ApiClient.Typed.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Net.Http.Json;
using Shared.Application.Core.Dto.Interfaces;
using Shared.Application.Core.Dto.Responses;

namespace Shared.Application.Core.ApiClient;

/// <summary>
/// Типизированные HTTP-методы (GET, POST, PUT, PATCH, DELETE) <see cref="ApiClient"/>.
/// </summary>
public abstract partial class ApiClient
{
    /// <summary>
    /// Выполняет GET-запрос с параметрами запроса и десериализует ответ в указанный тип.
    /// </summary>
    /// <typeparam name="TResult">Тип данных, в который будет десериализован ответ.</typeparam>
    /// <param name="uri">Относительный путь к ресурсу.</param>
    /// <param name="queryParams">Словарь параметров запроса.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Десериализованный объект типа <typeparamref name="TResult"/>.</returns>
    protected Task<TResult?> GetAsync<TResult>(
        string uri,
        Dictionary<string, string> queryParams,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAndDeserializeAsync<TResult>(
            ct => GetAsync(uri, queryParams, ct),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Выполняет типизированный HTTP POST-запрос с данными в формате JSON.
    /// </summary>
    /// <typeparam name="TResult">Тип данных, в который будет десериализован ответ.</typeparam>
    /// <param name="uri">Относительный путь к ресурсу.</param>
    /// <param name="content">Данные для отправки в теле запроса.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Десериализованный объект типа <typeparamref name="TResult"/>.</returns>
    protected Task<TResult?> PostAsync<TResult>(
        string uri,
        object? content = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAndDeserializeAsync<TResult>(
            ct => PostAsJsonAsync(uri, content, ct),
            uri,
            content,
            cancellationToken);
    }

    /// <summary>
    /// Выполняет типизированный HTTP POST-запрос без тела.
    /// </summary>
    /// <typeparam name="TResult">Тип данных, в который будет десериализован ответ.</typeparam>
    /// <param name="uri">Относительный путь к ресурсу.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Десериализованный объект типа <typeparamref name="TResult"/>.</returns>
    protected Task<TResult?> PostAsync<TResult>(
        string uri,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAndDeserializeAsync<TResult>(
            ct => PostAsync(uri, ct),
            uri,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Выполняет POST-запрос с файлом и возвращает типизированный результат.
    /// </summary>
    /// <typeparam name="TContent">Тип контента с файлом.</typeparam>
    /// <typeparam name="TResult">Тип данных, в который будет десериализован ответ.</typeparam>
    /// <param name="uri">Относительный путь к ресурсу.</param>
    /// <param name="content">Объект запроса с файлом.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Десериализованный объект типа <typeparamref name="TResult"/>.</returns>
    protected Task<TResult?> PostFileAsync<TContent, TResult>(
        string uri,
        TContent content,
        CancellationToken cancellationToken = default)
        where TContent : IWithFile
    {
        return ExecuteAndDeserializeAsync<TResult>(
            ct => PostFilesAsync(uri, content, ct),
            uri,
            content,
            cancellationToken);
    }

    /// <summary>
    /// Выполняет типизированный HTTP PUT-запрос по указанному URI.
    /// </summary>
    /// <typeparam name="TResult">Тип данных, в который будет десериализован ответ.</typeparam>
    /// <param name="uri">Относительный путь к ресурсу.</param>
    /// <param name="content">Данные, отправляемые в теле запроса.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Десериализованный объект типа <typeparamref name="TResult"/>.</returns>
    protected Task<TResult?> PutAsync<TResult>(
        string uri,
        object? content = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAndDeserializeAsync<TResult>(
            ct => PutAsync(uri, content, ct),
            uri,
            content,
            cancellationToken);
    }

    /// <summary>
    /// Выполняет типизированный HTTP PATCH-запрос по указанному URI.
    /// </summary>
    /// <typeparam name="TResult">Тип данных, в который будет десериализован ответ.</typeparam>
    /// <param name="uri">Относительный путь к ресурсу.</param>
    /// <param name="content">Данные, отправляемые в теле запроса.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Десериализованный объект типа <typeparamref name="TResult"/>.</returns>
    protected Task<TResult?> PatchAsync<TResult>(
        string uri,
        object? content = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAndDeserializeAsync<TResult>(
            ct => PatchAsync(uri, content, ct),
            uri,
            content,
            cancellationToken);
    }

    /// <summary>
    /// Выполняет типизированный HTTP DELETE-запрос по указанному URI.
    /// </summary>
    /// <typeparam name="TResult">Тип данных, в который будет десериализован ответ.</typeparam>
    /// <param name="uri">Относительный путь к ресурсу.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Десериализованный объект типа <typeparamref name="TResult"/>.</returns>
    protected Task<TResult?> DeleteAsync<TResult>(
        string uri,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAndDeserializeAsync<TResult>(
            ct => DeleteAsync(uri, ct),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Выполняет HTTP-запрос и десериализует ответ в указанный тип.
    /// </summary>
    /// <typeparam name="TResult">Тип данных, в который будет десериализован ответ.</typeparam>
    /// <param name="request">Функция выполнения HTTP-запроса.</param>
    /// <param name="logUri">URI запроса для логирования (опционально).</param>
    /// <param name="logContent">Содержимое запроса для логирования (опционально).</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Десериализованный объект типа <typeparamref name="TResult"/>.</returns>
    private async Task<TResult?> ExecuteAndDeserializeAsync<TResult>(
        Func<CancellationToken, Task<HttpResponseMessage>> request,
        string? logUri = null,
        object? logContent = null,
        CancellationToken cancellationToken = default)
    {
        return await ResponseAsJsonAsync<TResult>(
            await request(cancellationToken),
            logUri,
            logContent,
            cancellationToken);
    }

    /// <summary>
    /// Преобразует HTTP-ответ в объект указанного типа.
    /// </summary>
    /// <typeparam name="TResult">Тип данных, в который будет десериализован ответ.</typeparam>
    /// <param name="httpResponse">HTTP-ответ для десериализации.</param>
    /// <param name="logUri">URI запроса для логирования (опционально).</param>
    /// <param name="logContent">Содержимое запроса для логирования (опционально).</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Десериализованный объект типа <typeparamref name="TResult"/>.</returns>
    private async Task<TResult?> ResponseAsJsonAsync<TResult>(
        HttpResponseMessage httpResponse,
        string? logUri = null,
        object? logContent = null,
        CancellationToken cancellationToken = default)
    {
        using (httpResponse)
        {
            await _responseValidator.ValidateAsync(
                httpResponse,
                _typeName,
                logUri,
                logContent,
                cancellationToken);

            var result = await httpResponse.Content
                .ReadFromJsonAsync<TResult>(cancellationToken);
            if (result is ResponseBase response)
            {
                response.StatusCode = (int)httpResponse.StatusCode;
            }

            return result;
        }
    }
}
