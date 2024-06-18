// ----------------------------------------------------------------------------------------------
// <copyright file="PageableResponse.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;

namespace Shared.Application.Core.Dto.Responses;

/// <summary>
/// Данные с пагинацией.
/// </summary>
/// <typeparam name="T">Тип данных.</typeparam>
/// <param name="TotalPages">Всего страниц.</param>
/// <param name="Payload">Тело ответа.</param>
/// <param name="StatusCode">Статус ответа.</param>
public sealed record PageableResponse<T>(int TotalPages, T? Payload, int StatusCode = StatusCodes.Status200OK)
    : Response<T>(Payload, StatusCode);
