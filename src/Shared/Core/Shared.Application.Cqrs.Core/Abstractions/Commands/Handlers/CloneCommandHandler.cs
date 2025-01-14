// ----------------------------------------------------------------------------------------------
// <copyright file="CloneCommandHandler.cs" company="АО ИНЛАЙН ГРУП">
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
/// <param name="userProvider">Предоставляет информацию о текущем пользователе. См. <see cref="IUserProvider"/>.</param>
public abstract class CloneCommandHandler<TCommand, TRequest, TEntity, TResponsePayload, TResponse>(
    IMapper mapper,
    IUnitOfWork unitOfWork,
    ILoggerFactory loggerFactory,
    IEnumerable<IValidator<TEntity>> validators,
    IUserProvider userProvider)
    : EntityRequestHandler<TCommand, TResponse, TEntity>(
        unitOfWork,
        loggerFactory)
    where TCommand : CloneCommand<TRequest, TResponse>
    where TResponsePayload : class
    where TResponse : CreateResponse<TResponsePayload>, new()
    where TEntity : class, IEntity
{
    /// <summary>
    /// Обработка.
    /// </summary>
    /// <param name="command"> Запрос. </param>
    /// <param name="cancellationToken"> Токен ответа. </param>
    /// <returns> Ответ. </returns>
    public override async Task<TResponse> Handle(
        TCommand command,
        CancellationToken cancellationToken)
    {
        await GuardAsync(command, cancellationToken);
        var response = await CloneAsync(command, cancellationToken);
        response.StatusCode = StatusCodes.Status201Created;
        return response;
    }

    /// <summary>
    /// Создание ответа.
    /// </summary>
    /// <param name="command"> Запрос. </param>
    /// <param name="cancellationToken"> Токен ответа. </param>
    /// <returns> Ответ. </returns>
    protected virtual async Task<TResponse> CloneAsync(
        TCommand command,
        CancellationToken cancellationToken)
    {
        var options = ConstructOptions(command);
        var entityToClone = await Repository.GetAsync(command.Key, options);
        var clone = mapper.Map<TEntity, TEntity>(entityToClone!);
        await ProcessEntityAsync(clone, command);
        await ValidateAsync(clone, validators, cancellationToken);
        await Repository.AddAsync(clone, userProvider.UserId);
        await unitOfWork.SaveChangesAsync(token: cancellationToken);
        return CreateResponseDto(clone);
    }

    /// <summary>
    /// Создание dto ответа.
    /// </summary>
    /// <param name="entity"> Сущность. </param>
    /// <returns> Dto ответа. </returns>
    protected virtual TResponse CreateResponseDto(TEntity entity) =>
        new()
        {
            Id = entity.Id,
            Payload = mapper.Map<TEntity, TResponsePayload>(entity),
            StatusCode = StatusCodes.Status201Created,
        };
}
