// ----------------------------------------------------------------------------------------------
// <copyright file="EntityRemoveCommandHandler.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Contour.Admin.Auth.Sdk.Context;
using Microsoft.Extensions.Logging;
using Shared.Application.Cqrs.Core.Abstractions.Commands.Handlers;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Cqrs.Core.Features.Entity.Remove;

/// <inheritdoc />
public class EntityRemoveCommandHandler<TEntity>(
    IUnitOfWork unitOfWork,
    ILoggerFactory loggerFactory,
    IUserProvider userProvider)
    : DeleteCommandHandler<EntityRemoveCommand<TEntity>, TEntity>(unitOfWork, loggerFactory, userProvider)
    where TEntity : class, IEntity<Guid>;