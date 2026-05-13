// ----------------------------------------------------------------------------------------------
// <copyright file="ResponseWithMessage.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;

namespace Shared.Application.Core.Dto.Responses;

/// <summary>
/// Данные с сообщением.
/// </summary>
/// <param name="Message">Сообщение.</param>
/// <param name="StatusCode">Статус ответа.</param>
public record ResponseWithMessage(string? Message = default, int StatusCode = StatusCodes.Status200OK)
    : Response(StatusCode);
