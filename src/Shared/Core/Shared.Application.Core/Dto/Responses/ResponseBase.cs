// ----------------------------------------------------------------------------------------------
// <copyright file="ResponseBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace Shared.Application.Core.Dto.Responses;

/// <summary>
/// Базовый абстрактный класс для ответов.
/// </summary>
public abstract record ResponseBase
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="ResponseBase"/>.
    /// </summary>
    protected ResponseBase()
    {
    }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="ResponseBase"/>.
    /// </summary>
    /// <param name="StatusCode">Статус ответа.</param>
    protected ResponseBase(int StatusCode)
    {
        this.StatusCode = StatusCode;
    }

    /// <summary>
    /// Статус ответа.
    /// </summary>
    [JsonIgnore]
    public int StatusCode { get; set; }
}
