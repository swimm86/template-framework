// ----------------------------------------------------------------------------------------------
// <copyright file="ProxiedException.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Shared.Domain.Core.Exceptions.Models.Base;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.Exceptions.Models;

/// <summary>
/// Исключение, возникающее при проксировании HTTP-запросов к внешним сервисам.
/// </summary>
/// <remarks>
/// <para>
/// Содержит информацию об ошибке от удалённого сервиса, включая детали в формате
/// <see cref="ProblemDetails"/> и дополнительные данные через <see cref="IWithAdditionalData.AdditionalData"/>.
/// </para>
/// <para>
/// Дополнительные данные извлекаются из HTTP-ответа и удаляются из <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails.Extensions"/>
/// перед тем, как исключение будет брошено. Это гарантирует, что данные доступны только
/// бэкенд-потребителю через <see cref="TryGetAdditionalData{T}"/>, но не передаются фронтенду.
/// </para>
/// <example>
/// <code>
/// try
/// {
///     await apiClient.SomeMethodAsync();
/// }
/// catch (ProxiedException ex)
/// {
///     // Получение дополнительных данных
///     if (ex.TryGetAdditionalData&lt;ICollection&lt;int&gt;&gt;("notFoundCodes", out var codes))
///     {
///         // Обработка не найденных кодов
///     }
///     // Доступ к деталям ошибки
///     Console.WriteLine($"Status: {ex.StatusCode}");
///     Console.WriteLine($"Detail: {ex.ProblemDetails.Detail}");
/// }
/// </code>
/// </example>
/// </remarks>
public class ProxiedException
    : AppException
{
    /// <summary>
    /// Детали ошибки в формате RFC 7807 (ProblemDetails).
    /// </summary>
    public ProblemDetails ProblemDetails { get; }

    /// <summary>
    /// HTTP-статус код ошибки.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="ProxiedException"/>.
    /// </summary>
    /// <param name="problemDetails">Детали ошибки в формате <see cref="ProblemDetails"/>.</param>
    /// <param name="statusCode">HTTP-статус код ошибки.</param>
    /// <inheritdoc cref="AppException(string, Exception?, IReadOnlyDictionary{string, object}?)"/>
    /// <param name="innerException"/><param name="additionalData"/>
    public ProxiedException(
        ProblemDetails problemDetails,
        int statusCode,
        Exception? innerException = null,
        IReadOnlyDictionary<string, object>? additionalData = null)
    : base(string.Empty, innerException: innerException, additionalData: additionalData)
    {
        ProblemDetails = problemDetails;
        StatusCode = statusCode;
    }

    /// <summary>
    /// Возвращает типизированное значение из словаря <see cref="IWithAdditionalData.AdditionalData"/> по ключу.
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения.</typeparam>
    /// <param name="key">Ключ для поиска в словаре.</param>
    /// <param name="value">Возвращаемое значение, если операция успешна.</param>
    /// <returns>
    /// <c>true</c>, если значение найдено и успешно десериализовано в тип <typeparamref name="T"/>;
    /// в противном случае <c>false</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Метод поддерживает два сценария:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// Если значение уже имеет тип <typeparamref name="T"/>, возвращается как есть.
    /// </item>
    /// <item>
    /// Если значение является <see cref="JsonElement"/>, выполняется десериализация.
    /// </item>
    /// </list>
    /// <para>
    /// Возвращает <c>false</c>, если:
    /// </para>
    /// <list type="bullet">
    /// <item>Ключ не найден в словаре.</item>
    /// <item>Значение не может быть десериализовано в тип <typeparamref name="T"/>.</item>
    /// <item>Произошла ошибка десериализации JSON.</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// if (ex.TryGetAdditionalData&lt;ICollection&lt;int&gt;&gt;("notFoundCodes", out var codes))
    /// {
    ///     // codes содержит коллекцию не найденных кодов
    /// }
    /// </code>
    /// </example>
    public bool TryGetAdditionalData<T>(string key, out T? value)
    {
        value = default;
        if (AdditionalData?.TryGetValue(key, out var result) == true)
        {
            try
            {
                if (result is T tValue)
                {
                    value = tValue;
                    return true;
                }

                if (result is JsonElement jsonElement)
                {
                    value = jsonElement.Deserialize<T>();
                    return true;
                }
            }
            catch (JsonException)
            {
                return false;
            }
        }

        return false;
    }
}
