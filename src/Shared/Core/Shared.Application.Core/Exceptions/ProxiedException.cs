// ----------------------------------------------------------------------------------------------
// <copyright file="ProxiedException.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Shared.Domain.Core.Exceptions.Models.Base;

namespace Shared.Application.Core.Exceptions;

/// <summary>
/// Проксированная ошибка.
/// </summary>
public class ProxiedException
    : AppException
{
    /// <summary>
    /// Детали ошибки.
    /// </summary>
    public readonly ProblemDetails ProblemDetails;

    /// <summary>
    /// Статус ответа.
    /// </summary>
    public readonly int StatusCode;

    /// <summary>
    /// Инициализация <see cref="ProxiedException"/>.
    /// </summary>
    /// <param name="problemDetails">Детали ошибки.</param>
    /// <param name="statusCode">Статус ответа.</param>
    /// <param name="additionalData">Дополнительная информация.</param>
    public ProxiedException(
        ProblemDetails problemDetails,
        int statusCode,
        Dictionary<string, object>? additionalData = default)
    {
        ProblemDetails = problemDetails;
        StatusCode = statusCode;
        AdditionalData = additionalData;
    }

    /// <summary>
    /// Возвращает типизированное значение из словаря <see cref="AppException.AdditionalData"/> по ключу.
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения</typeparam>
    /// <param name="key">Ключ.</param>
    /// <param name="value">Возвращаемое значение</param>
    /// <returns>true, если значение найдено и успешно десериализовано.</returns>
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
