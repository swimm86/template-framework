// ----------------------------------------------------------------------------------------------
// <copyright file="CreateResponse.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Shared.Application.Core.Dto.Responses;

namespace Shared.Application.Cqrs.Core.Abstractions.Commands.Responses;

/// <summary>
/// Ответ создания.
/// </summary>
/// <typeparam name="TDto">Тип данных для ответа.</typeparam>
public record CreateResponse<TDto> : Response<TDto>
{
    /// <summary>
    /// Конструктор для класса <see cref="CreateResponse{TDto}"/>.
    /// </summary>
    public CreateResponse()
    {
    }

    /// <summary>
    /// Конструктор для класса <see cref="CreateResponse{TDto}"/>.
    /// </summary>
    /// <typeparam name="TDto">Тип данных для ответа.</typeparam>
    /// <param name="Id">Идентификатор созданной сущности.</param>
    /// <param name="Payload">Тело ответа.</param>
    /// <param name="StatusCode">Статус ответа.</param>
    public CreateResponse(
        object Id,
        TDto Payload,
        int StatusCode = StatusCodes.Status201Created)
        : base(Payload, StatusCode)
    {
        this.Id = Id;
    }

    /// <summary>Идентификатор созданной сущности.</summary>
    public object Id { get; init; }
}
