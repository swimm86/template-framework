// ----------------------------------------------------------------------------------------------
// <copyright file="CreateCommandHandler.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Auth;
using Shared.Application.Cqrs.Core.Abstractions.Commands.Requests;
using Shared.Application.Cqrs.Core.Abstractions.Commands.Responses;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Interfaces;
using Shared.Domain.Core.Mapping.Interfaces;

namespace Shared.Application.Cqrs.Core.Abstractions.Commands.Handlers;

/// <summary>
/// Создание обработчика.
/// </summary>
/// <typeparam name="TCommand">Тип команды.</typeparam>
/// <typeparam name="TRequest">Тип запроса.</typeparam>
/// <typeparam name="TEntity">Тип сущности.</typeparam>
/// <typeparam name="TResponsePayload">Payload.</typeparam>
/// <typeparam name="TResponse">Тип Dto при создании.</typeparam>
/// <param name="loggerFactory">Фабрика логгирования.</param>
public abstract class CreateCommandHandler<TCommand, TRequest, TEntity, TResponsePayload, TResponse>(
    ILoggerFactory loggerFactory,
    IMapper mapper,
    IUnitOfWork unitOfWork,
    IEnumerable<IValidator<TEntity>> validators,
    IUserProvider userProvider)
    : EntityRequestHandler<TCommand, TResponse, TEntity>(unitOfWork, loggerFactory)
    where TResponsePayload : class
    where TResponse : CreateResponse<TResponsePayload>, new()
    where TCommand : CreateCommand<TRequest, TResponse>
    where TEntity : class, IEntity
{
    /// <summary>
    /// Обработка.
    /// </summary>
    /// <param name="command"><see cref="TCommand"/>.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns><see cref="TResponse"/>.</returns>
    public override async Task<TResponse> Handle(
        TCommand command,
        CancellationToken cancellationToken)
    {
        await GuardAsync(command, cancellationToken);
        var response = await CreateAsync(command, cancellationToken);
        response.StatusCode = StatusCodes.Status201Created;
        return response;
    }

    /// <summary>
    /// Создание ответа.
    /// </summary>
    /// <param name="command"><see cref="TCommand"/>.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns><see cref="TResponse"/>.</returns>
    protected virtual async Task<TResponse> CreateAsync(
        TCommand command,
        CancellationToken cancellationToken)
    {
        var entity = mapper.Map<TRequest, TEntity>(command.Request);
        await ProcessEntityAsync(entity, command);
        await ValidateAsync(entity, validators, cancellationToken);
        var newEntity = await Repository
            .AddAsync(entity, userProvider.UserId, userProvider.UserFullName, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
        return CreateResponseDto(newEntity);
    }

    /// <summary>
    /// Создание dto ответа.
    /// </summary>
    /// <param name="entity"><see cref="TEntity"/>.</param>
    /// <returns><see cref="TResponse"/>.</returns>
    protected virtual TResponse CreateResponseDto(TEntity entity) =>
        new()
        {
            Id = entity.Id,
            Payload = mapper.Map<TEntity, TResponsePayload>(entity),
            StatusCode = StatusCodes.Status201Created,
        };
}
