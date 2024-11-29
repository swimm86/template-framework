// ----------------------------------------------------------------------------------------------
// <copyright file="ResponseBase.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
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
    /// Пустой конструктор.
    /// </summary>
    protected ResponseBase()
    {
    }

    /// <summary>
    /// Базовый абстрактный класс для ответов.
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
