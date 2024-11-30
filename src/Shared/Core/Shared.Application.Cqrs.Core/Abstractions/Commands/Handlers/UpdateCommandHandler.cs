// ----------------------------------------------------------------------------------------------
// <copyright file="UpdateCommandHandler.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using FluentValidation;
using Gpn.Contour.Admin.Auth.Sdk.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Application.Cqrs.Core.Abstractions.Commands.Requests;
using Shared.Application.Cqrs.Core.Abstractions.Commands.Responses;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Exceptions.Models;
using Shared.Domain.Core.Interfaces;
using Shared.Domain.Core.Mapping.Interfaces;

namespace Shared.Application.Cqrs.Core.Abstractions.Commands.Handlers;

/// <summary>
/// Обработчик изменения.
/// </summary>
/// <typeparam name="TCommand">Тип команды.</typeparam>
/// <typeparam name="TRequest">Request.</typeparam>
/// <typeparam name="TEntity">Тип сущности.</typeparam>
/// <typeparam name="TPayload">Тип dto изменения.</typeparam>
/// <typeparam name="TResponse">Тип ответа.</typeparam>
/// <param name="loggerFactory">Логгер.</param>
/// <param name="userProvider">Предоставляет информацию о текущем пользователе. См. <see cref="IUserProvider"/>.</param>
public abstract class UpdateCommandHandler<TCommand, TRequest, TEntity, TPayload, TResponse>(
    ILoggerFactory loggerFactory,
    IMapper mapper,
    IUnitOfWork unitOfWork,
    IEnumerable<IValidator<TEntity>> validators,
    IUserProvider userProvider)
    : EntityRequestHandler<TCommand, TResponse, TEntity>(unitOfWork, loggerFactory)
    where TCommand : UpdateCommand<TRequest, TResponse>
    where TEntity : class, IEntity
    where TResponse : UpdateResponse<TPayload>, new()
{
    /// <summary>
    /// Обработчик.
    /// </summary>
    /// <param name="command"> Запрос. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <returns> Ответ. </returns>
    public override async Task<TResponse> Handle(TCommand command, CancellationToken cancellationToken)
    {
        await GuardAsync(command, cancellationToken);
        var entity = await FindAsync(command, cancellationToken);
        var response = await UpdateAsync(command, entity, cancellationToken);
        return response;
    }

    /// <summary>
    /// Поиск сущности.
    /// </summary>
    /// <param name="command">  Запрос. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <returns> Сущность. </returns>
    /// <exception cref="NullReferenceException"> Вызывается, когда сущность не найдена. </exception>
    protected virtual async Task<TEntity> FindAsync(TCommand command, CancellationToken cancellationToken)
    {
        var options = ConstructOptions(command);
        options.WithTracking = true;
        var entity = await Repository.GetAsync(command.Key, options);
        if (entity is null)
        {
            throw new NotFoundException(typeof(TEntity), command.Key);
        }

        return entity;
    }

    /// <summary>
    /// Обновление сущности.
    /// </summary>
    /// <param name="command">Команда.</param>
    /// <param name="entity">Сущность.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns>Ответ.</returns>
    protected virtual async Task<TResponse> UpdateAsync(
        TCommand command,
        TEntity entity,
        CancellationToken cancellationToken)
    {
        mapper.Map(command.Request, entity);
        await ProcessEntityAsync(entity, command);
        await ValidateAsync(entity, validators, cancellationToken);

        if (entity is IWithUpdated entityWithUpdated)
        {
            entityWithUpdated.SetUpdatedByUserId(userProvider.GetUserId());
        }

        await unitOfWork.SaveChangesAsync(token: cancellationToken);
        return new TResponse
        {
            Key = command.Key,
            Payload = mapper.Map<TEntity, TPayload>(entity),
            StatusCode = StatusCodes.Status200OK,
        };
    }
}
