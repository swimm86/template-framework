// ----------------------------------------------------------------------------------------------
// <copyright file="UpdateCommandHandler.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Auth;
using Shared.Application.Cqrs.Core.Abstractions.Commands.Requests;
using Shared.Application.Cqrs.Core.Abstractions.Commands.Responses;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Exceptions.Models;
using Shared.Domain.Core.Interfaces;
using Shared.Domain.Core.Mapping.Interfaces;

namespace Shared.Application.Cqrs.Core.Abstractions.Commands.Handlers;

/// <summary>
/// Базовый обработчик команды обновления сущности.
/// </summary>
/// <typeparam name="TCommand">Тип команды обновления.</typeparam>
/// <typeparam name="TRequest">Тип DTO с данными для обновления.</typeparam>
/// <typeparam name="TEntity">Тип обновляемой сущности.</typeparam>
/// <typeparam name="TPayload">Тип данных полезной нагрузки ответа.</typeparam>
/// <typeparam name="TResponse">Тип ответа команды обновления.</typeparam>
/// <param name="loggerFactory">Фабрика для создания логгеров.</param>
/// <param name="mapper">Сервис маппинга объектов.</param>
/// <param name="unitOfWork">Единица работы для управления транзакциями.</param>
/// <param name="validators">Коллекция валидаторов сущности.</param>
/// <param name="userProvider">Сервис получения информации о текущем пользователе.</param>
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
    /// <inheritdoc />
    public override async Task<TResponse> Handle(
        TCommand command,
        CancellationToken cancellationToken)
    {
        await GuardAsync(command, cancellationToken);
        var entity = await FindAsync(command, cancellationToken);
        var response = await UpdateAsync(command, entity, cancellationToken);
        return response;
    }

    /// <summary>
    /// Выполняет поиск сущности по ключу.
    /// </summary>
    /// <param name="command">Команда обновления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Найденная сущность.</returns>
    /// <exception cref="NotFoundException">Выбрасывается, если сущность не найдена.</exception>
    protected virtual async Task<TEntity> FindAsync(
        TCommand command,
        CancellationToken cancellationToken)
    {
        var options = ConstructOptions(command);
        options.WithTracking = true;
        var entity = await Repository.GetAsync(
            command.Key,
            options,
            cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(typeof(TEntity), command.Key);
        }

        return entity;
    }

    /// <summary>
    /// Обновляет сущность и сохраняет изменения в базе данных.
    /// </summary>
    /// <param name="command">Команда обновления.</param>
    /// <param name="entity">Сущность для обновления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Ответ с данными обновлённой сущности.</returns>
    protected virtual async Task<TResponse> UpdateAsync(
        TCommand command,
        TEntity entity,
        CancellationToken cancellationToken)
    {
        mapper.Map(command.Request, entity);
        await ProcessEntityAsync(entity, command, cancellationToken);
        await ValidateAsync(entity, validators, cancellationToken);

        if (entity is IWithUpdated entityWithUpdated)
        {
            entityWithUpdated.SetUpdatedByUserId(userProvider.UserId);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
        return new TResponse
        {
            Key = command.Key,
            Payload = mapper.Map<TEntity, TPayload>(entity),
            StatusCode = StatusCodes.Status200OK,
        };
    }

    /// <inheritdoc />
    protected override QueryOptions<TEntity> ConstructOptions(TCommand request)
    {
        var result = base.ConstructOptions(request);
        result.WithTracking = true;
        return result;
    }
}
