// ----------------------------------------------------------------------------------------------
// <copyright file="Response.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace Shared.Application.Core.Dto.Responses;

/// <summary>
/// Данные ответа.
/// </summary>
/// <typeparam name="T">Тип данных для ответа.</typeparam>
/// <param name="Payload">Тело ответа.</param>
/// <param name="StatusCode">Статус ответа.</param>
public record Response<T>(T? Payload, int StatusCode) : ResponseBase(StatusCode);

/// <summary>
/// Базовый абстрактный класс для ответа.
/// </summary>
/// <param name="StatusCode">Статус ответа.</param>
public abstract record ResponseBase(int StatusCode)
{
    /// <summary>
    /// Статус ответа.
    /// </summary>
    [JsonIgnore]
    public int StatusCode { get; set; } = StatusCode;
}
