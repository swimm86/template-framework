// ----------------------------------------------------------------------------------------------
// <copyright file="DeleteCommandHandler.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Auth;
using Shared.Application.Core.Dto.Responses;
using Shared.Application.Cqrs.Core.Abstractions.Commands.Requests;
using Shared.Domain.Core.Dal.Repository.Extensions;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Cqrs.Core.Abstractions.Commands.Handlers;

/// <summary>
/// Обработчик команды удаления.
/// </summary>
/// <param name="loggerFactory">Фабрика логгеров.</param>
/// <typeparam name="TCommand">Тип команды.</typeparam>
/// <typeparam name="TEntity">Тип сущности.</typeparam>
public abstract class DeleteCommandHandler<TCommand, TEntity>(
    IUnitOfWork unitOfWork,
    ILoggerFactory loggerFactory,
    IUserProvider userProvider)
    : EntityRequestHandler<TCommand, Response, TEntity>(unitOfWork, loggerFactory)
    where TCommand : DeleteCommand
    where TEntity : class, IEntity
{
    /// <inheritdoc />
    public override async Task<Response> Handle(TCommand command, CancellationToken cancellationToken)
    {
        await GuardAsync(command, cancellationToken);
        var entity = await FindAsync(command);
        var response = await DeleteAsync(entity, command);

        return response;
    }

    /// <summary>
    /// Поиск ентити.
    /// </summary>
    /// <param name="command">Запрос</param>
    /// <returns>Ентити</returns>
    protected virtual async Task<TEntity> FindAsync(TCommand command)
    {
        var options = ConstructOptions(command);
        options.WithTracking = true;
        var entity = await Repository.GetByIdOrThrowAsync(command.Key, options);
        return entity;
    }

    /// <summary>
    /// Удаление ентити.
    /// </summary>
    /// <param name="entity">Ентити</param>
    /// <param name="command">Запрос</param>
    /// <returns>Ответ</returns>
    protected virtual async Task<Response> DeleteAsync(TEntity entity, TCommand command)
    {
        await Repository.RemoveAsync(entity, userId: userProvider.UserId);
        await unitOfWork.SaveChangesAsync();
        return new Response { StatusCode = StatusCodes.Status200OK };
    }
}
