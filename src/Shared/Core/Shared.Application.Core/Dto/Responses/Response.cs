// ----------------------------------------------------------------------------------------------
// <copyright file="Response.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Dto.Responses;

/// <summary>
/// Данные ответа.
/// </summary>
public record Response : ResponseBase
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="Response"/>.
    /// </summary>
    /// <param name="statusCode">Статус ответа.</param>
    public Response(int statusCode)
        : base(statusCode)
    {
    }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="Response"/>.
    /// </summary>
    public Response()
    {
    }
}

/// <summary>
/// Данные ответа с полезной нагрузкой.
/// </summary>
/// <typeparam name="T">Тип полезной нагрузки.</typeparam>
public record Response<T> : Response
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="Response{T}"/>.
    /// </summary>
    /// <param name="payload">Полезная нагрузка.</param>
    /// <param name="statusCode">Статус ответа.</param>
    public Response(T? payload, int statusCode)
        : base(statusCode)
    {
        Payload = payload;
    }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="Response{T}"/>.
    /// </summary>
    public Response()
    {
    }

    /// <summary>
    /// Полезная нагрузка.
    /// </summary>
    public T? Payload { get; set; }
}
