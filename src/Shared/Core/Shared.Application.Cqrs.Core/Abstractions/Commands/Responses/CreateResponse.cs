// ----------------------------------------------------------------------------------------------
// <copyright file="CreateResponse.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Shared.Application.Core.Dto.Responses;

namespace Shared.Application.Cqrs.Core.Abstractions.Commands.Responses;

/// <summary>
/// Ответ команды создания сущности.
/// </summary>
/// <typeparam name="TDto">Тип данных полезной нагрузки ответа.</typeparam>
public record CreateResponse<TDto>
    : Response<TDto>
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="CreateResponse{TDto}"/>.
    /// </summary>
    public CreateResponse()
    {
    }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="CreateResponse{TDto}"/> с указанными параметрами.
    /// </summary>
    /// <param name="Id">Идентификатор созданной сущности.</param>
    /// <param name="Payload">Полезная нагрузка ответа.</param>
    /// <param name="StatusCode">HTTP-код статуса ответа.</param>
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
