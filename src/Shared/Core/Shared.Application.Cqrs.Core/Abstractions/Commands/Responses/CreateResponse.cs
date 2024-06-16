// ----------------------------------------------------------------------------------------------
// <copyright file="CreateResponse.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Cqrs.Core.Abstractions.Commands.Responses;

/// <summary>
/// Ответ создания.
/// </summary>
/// <typeparam name="TDto">Тип Dto.</typeparam>
public class CreateResponse<TDto>
{
    /// <summary>
    /// Идентификатор.
    /// </summary>
    public object Id { get; set; }

    /// Ответ.
    /// </summary>
    public TDto Result { get; init; }
}
