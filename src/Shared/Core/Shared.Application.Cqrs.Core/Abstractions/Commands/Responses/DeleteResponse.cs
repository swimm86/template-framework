// ----------------------------------------------------------------------------------------------
// <copyright file="DeleteResponse.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Cqrs.Core.Abstractions.Commands.Responses;

/// <summary>
/// Ответ команды удаления.
/// </summary>
/// <typeparam name="TKey">Тип ключа.</typeparam>
/// <typeparam name="TDto">Dto.</typeparam>
public class DeleteResponse<TKey, TDto>
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
