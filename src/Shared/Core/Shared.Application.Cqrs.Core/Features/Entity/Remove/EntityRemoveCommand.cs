// ----------------------------------------------------------------------------------------------
// <copyright file="EntityRemoveCommand.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Cqrs.Core.Abstractions.Commands;
using Shared.Application.Cqrs.Core.Abstractions.Commands.Requests;
using Shared.Application.Cqrs.Core.Features.Entity.Remove.Request;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Cqrs.Core.Features.Entity.Remove;

/// <summary>
/// <see cref="ICommand"/> для удаления сущности.
/// </summary>
/// <param name="Request"><see cref="EntityRemoveRequest"/>.</param>
public record EntityRemoveCommand<TEntity>(EntityRemoveRequest Request)
    : DeleteCommand(Request.Id)
    where TEntity : class, IEntity<Guid>;