// ----------------------------------------------------------------------------------------------
// <copyright file="UpdateResponse.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dto.Responses;

namespace Shared.Application.Cqrs.Core.Abstractions.Commands.Responses;

/// <summary>
/// Ответ команды обновления сущности.
/// </summary>
/// <typeparam name="TDto">Тип данных полезной нагрузки ответа.</typeparam>
public record UpdateResponse<TDto>
    : Response<TDto>
{
    /// <summary>Ключ обновлённой сущности.</summary>
    public object Key { get; init; }
}
