// ----------------------------------------------------------------------------------------------
// <copyright file="EntityRemoveRequest.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Cqrs.Core.Features.Entity.Remove.Request;

/// <summary>
/// Запрос на удаление сущности по идентификатору.
/// </summary>
public record EntityRemoveRequest
{
    /// <summary>Идентификатор сущности для удаления.</summary>
    public Guid Id { get; init; }
}