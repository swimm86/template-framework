// ----------------------------------------------------------------------------------------------
// <copyright file="PageableResponse.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Dto.Responses;

/// <summary>
/// Данные с пагинацией.
/// </summary>
/// <typeparam name="T">Тип данных.</typeparam>
/// <param name="PageNumber">Номер страницы.</param>
/// <param name="PageSize">Размер страницы.</param>
/// <param name="TotalPages">Всего страниц.</param>
/// <param name="Payload">Данные.</param>
public sealed record PageableResponse<T>(int PageNumber, int PageSize, int TotalPages, T? Payload);