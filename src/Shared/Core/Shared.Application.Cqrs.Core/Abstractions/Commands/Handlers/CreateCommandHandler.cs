// ----------------------------------------------------------------------------------------------
// <copyright file="CreateCommandHandler.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
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
/// Базовый обработчик команды создания сущности.
/// </summary>
/// <typeparam name="TCommand">Тип команды создания.</typeparam>
/// <typeparam name="TRequest">Тип DTO с данными для создания.</typeparam>
/// <typeparam name="TEntity">Тип создаваемой сущности.</typeparam>
/// <typeparam name="TResponsePayload">Тип данных полезной нагрузки ответа.</typeparam>
/// <typeparam name="TResponse">Тип ответа команды создания.</typeparam>
/// <param name="loggerFactory">Фабрика для создания логгеров.</param>
/// <param name="mapper">Сервис маппинга объектов.</param>
/// <param name="unitOfWork">Единица работы для управления транзакциями.</param>
/// <param name="validators">Коллекция валидаторов сущности.</param>
/// <param name="userProvider">Сервис получения информации о текущем пользователе.</param>
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
    /// <inheritdoc />
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
    /// Создаёт новую сущность и сохраняет её в базе данных.
    /// </summary>
    /// <param name="command">Команда создания.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Ответ с данными созданной сущности.</returns>
    protected virtual async Task<TResponse> CreateAsync(
        TCommand command,
        CancellationToken cancellationToken)
    {
        var entity = mapper.Map<TRequest, TEntity>(command.Request);
        await ProcessEntityAsync(entity, command, cancellationToken);
        await ValidateAsync(entity, validators, cancellationToken);
        var newEntity = await Repository
            .AddAsync(entity, userProvider.UserId, userProvider.UserFullName, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
        return CreateResponseDto(newEntity);
    }

    /// <summary>
    /// Формирует ответ с данными созданной сущности.
    /// </summary>
    /// <param name="entity">Созданная сущность.</param>
    /// <returns>Ответ команды создания.</returns>
    protected virtual TResponse CreateResponseDto(TEntity entity) =>
        new()
        {
            Id = entity.Id,
            Payload = mapper.Map<TEntity, TResponsePayload>(entity),
            StatusCode = StatusCodes.Status201Created,
        };
}
