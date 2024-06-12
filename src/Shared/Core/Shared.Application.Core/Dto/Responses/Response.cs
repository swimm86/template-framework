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
public sealed record Response<T>(T Payload, [property: JsonIgnore] int StatusCode);
