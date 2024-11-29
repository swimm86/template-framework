// ----------------------------------------------------------------------------------------------
// <copyright file="Response.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Dto.Responses;

/// <summary>
/// Данные ответа.
/// </summary>
public record Response : ResponseBase
{
    /// <summary>
    /// Коснтруктор.
    /// </summary>
    /// <param name="statusCode">Статус ответа.</param>
    public Response(int statusCode)
        : base(statusCode)
    {
    }

    /// <summary>
    /// Пустой конструктор.
    /// </summary>
    public Response()
    {
    }
}

/// <summary>
/// Данные ответа с Payload.
/// </summary>
/// <typeparam name="T">Тип данных для Payload.</typeparam>
public record Response<T> : Response
{
    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="payload">Payload.</param>
    /// <param name="statusCode">Статус ответа.</param>
    public Response(T? payload, int statusCode)
        : base(statusCode)
    {
        Payload = payload;
    }

    /// <summary>
    /// Пустой конструктор.
    /// </summary>
    public Response()
    {
    }

    /// <summary>
    /// Payload.
    /// </summary>
    public T? Payload { get; set; }
}
