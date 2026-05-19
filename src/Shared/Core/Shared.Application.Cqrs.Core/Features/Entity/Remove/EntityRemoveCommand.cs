// ----------------------------------------------------------------------------------------------
// <copyright file="EntityRemoveCommand.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Cqrs.Core.Abstractions.Commands.Requests;
using Shared.Application.Cqrs.Core.Features.Entity.Remove.Request;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Cqrs.Core.Features.Entity.Remove;

/// <summary>
/// Команда удаления сущности по идентификатору.
/// </summary>
/// <typeparam name="TEntity">Тип удаляемой сущности.</typeparam>
/// <param name="Request">Запрос с идентификатором сущности.</param>
public record EntityRemoveCommand<TEntity>(EntityRemoveRequest Request)
    : DeleteCommand(Request.Id)
    where TEntity : class, IEntity<Guid>;