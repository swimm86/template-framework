// ----------------------------------------------------------------------------------------------
// <copyright file="ReadListResponse.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Cqrs.Core.Abstractions.Queries.Responses;

/// <summary>
/// Результат чтения списка
/// </summary>
/// <typeparam name="TDto">Тип в результирующем списке.</typeparam>
public class ReadListResponse<TDto>
{
    /// <summary>
    /// Результат чтения.
    /// </summary>
    public IEnumerable<TDto> Result { get; init; } = [];

    /// <summary>
    /// Общее количество элеметов попадающее под запрос.
    /// </summary>
    public int TotalCount { get; init; }
}
