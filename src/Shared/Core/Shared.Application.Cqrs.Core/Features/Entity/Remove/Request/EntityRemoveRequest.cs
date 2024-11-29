// ----------------------------------------------------------------------------------------------
// <copyright file="EntityRemoveRequest.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Cqrs.Core.Features.Entity.Remove.Request;

/// <summary>
/// Request для удаления сущности по идентификатору.
/// </summary>
public record EntityRemoveRequest
{
    /// <summary>
    /// Идентификатор сущности, которую необходимативно удалить.
    /// </summary>
    public Guid Id { get; init; }
}