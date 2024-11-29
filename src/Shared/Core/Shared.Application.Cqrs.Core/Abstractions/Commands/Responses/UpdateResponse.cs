// ----------------------------------------------------------------------------------------------
// <copyright file="UpdateResponse.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dto.Responses;

namespace Shared.Application.Cqrs.Core.Abstractions.Commands.Responses;

/// <summary>
/// Ответ команды обновления
/// </summary>
/// <typeparam name="TDto">Тип Dto.</typeparam>
public record UpdateResponse<TDto> : Response<TDto>
{
    /// <summary>
    /// Ключ
    /// </summary>
    public object Key { get; init; }
}
