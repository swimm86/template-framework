// ----------------------------------------------------------------------------------------------
// <copyright file="UpdateResponse.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Cqrs.Core.Abstractions.Commands.Responses;

/// <summary>
/// Ответ команды обновления
/// </summary>
/// <typeparam name="TKey">Тип ключа.</typeparam>
/// <typeparam name="TDto">Тип Dto.</typeparam>
public class UpdateResponse<TKey, TDto>
{
    /// <summary>
    /// Ключ
    /// </summary>
    public TKey Key { get; init; }

    /// <summary>
    /// Dto
    /// </summary>
    public TDto Result { get; init; }
}
