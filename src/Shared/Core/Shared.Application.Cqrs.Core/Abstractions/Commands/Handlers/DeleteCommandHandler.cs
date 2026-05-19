// ----------------------------------------------------------------------------------------------
// <copyright file="DeleteCommandHandler.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
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
/// Базовый обработчик команды удаления сущности.
/// </summary>
/// <typeparam name="TCommand">Тип команды удаления.</typeparam>
/// <typeparam name="TEntity">Тип удаляемой сущности.</typeparam>
/// <param name="unitOfWork">Единица работы для управления транзакциями.</param>
/// <param name="loggerFactory">Фабрика для создания логгеров.</param>
/// <param name="userProvider">Сервис получения информации о текущем пользователе.</param>
public abstract class DeleteCommandHandler<TCommand, TEntity>(
    IUnitOfWork unitOfWork,
    ILoggerFactory loggerFactory,
    IUserProvider userProvider)
    : EntityRequestHandler<TCommand, Response, TEntity>(unitOfWork, loggerFactory)
    where TCommand : DeleteCommand
    where TEntity : class, IEntity
{
    /// <inheritdoc />
    public override async Task<Response> Handle(
        TCommand command,
        CancellationToken cancellationToken)
    {
        await GuardAsync(command, cancellationToken);
        var entity = await FindAsync(command, cancellationToken);
        var response = await DeleteAsync(entity, command, cancellationToken);

        return response;
    }

    /// <summary>
    /// Выполняет поиск сущности по ключу.
    /// </summary>
    /// <param name="command">Команда удаления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Найденная сущность.</returns>
    protected virtual async Task<TEntity> FindAsync(
        TCommand command,
        CancellationToken cancellationToken)
    {
        var options = ConstructOptions(command);
        options.WithTracking = true;
        var entity = await Repository.GetByIdOrThrowAsync(
            command.Key,
            options,
            cancellationToken: cancellationToken);
        return entity;
    }

    /// <summary>
    /// Удаляет сущность из базы данных.
    /// </summary>
    /// <param name="entity">Сущность для удаления.</param>
    /// <param name="command">Команда удаления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Ответ команды удаления.</returns>
    protected virtual async Task<Response> DeleteAsync(
        TEntity entity,
        TCommand command,
        CancellationToken cancellationToken)
    {
        await Repository.RemoveAsync(
            entity,
            userId: userProvider.UserId,
            cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new Response { StatusCode = StatusCodes.Status200OK };
    }
}
