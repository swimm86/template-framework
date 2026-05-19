// ----------------------------------------------------------------------------------------------
// <copyright file="CloneCommandHandler.cs" company="swimm86@yandex.ru">
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
/// Базовый обработчик команды клонирования сущности.
/// </summary>
/// <typeparam name="TCommand">Тип команды клонирования.</typeparam>
/// <typeparam name="TRequest">Тип DTO с дополнительными данными для клонирования.</typeparam>
/// <typeparam name="TEntity">Тип клонируемой сущности.</typeparam>
/// <typeparam name="TResponsePayload">Тип данных полезной нагрузки ответа.</typeparam>
/// <typeparam name="TResponse">Тип ответа команды клонирования.</typeparam>
/// <param name="mapper">Сервис маппинга объектов.</param>
/// <param name="unitOfWork">Единица работы для управления транзакциями.</param>
/// <param name="loggerFactory">Фабрика для создания логгеров.</param>
/// <param name="validators">Коллекция валидаторов сущности.</param>
/// <param name="userProvider">Сервис получения информации о текущем пользователе.</param>
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
    /// <inheritdoc />
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
    /// Клонирует сущность и сохраняет копию в базе данных.
    /// </summary>
    /// <param name="command">Команда клонирования.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Ответ с данными клонированной сущности.</returns>
    protected virtual async Task<TResponse> CloneAsync(
        TCommand command,
        CancellationToken cancellationToken)
    {
        var options = ConstructOptions(command);
        var entityToClone = await Repository.GetAsync(command.Key, options, cancellationToken);
        // TODO BUG (#3): entityToClone! null-forgiving operator suppresses potential NRE — GetAsync may return null, Map will throw ArgumentNullException without a clear message
        var clone = mapper.Map<TEntity, TEntity>(entityToClone!);
        await ProcessEntityAsync(clone, command, cancellationToken);
        await ValidateAsync(clone, validators, cancellationToken);
        await Repository.AddAsync(clone, userProvider.UserId, userProvider.UserFullName, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
        return CreateResponseDto(clone);
    }

    /// <summary>
    /// Формирует ответ с данными клонированной сущности.
    /// </summary>
    /// <param name="entity">Клонированная сущность.</param>
    /// <returns>Ответ команды клонирования.</returns>
    protected virtual TResponse CreateResponseDto(TEntity entity) =>
        new()
        {
            Id = entity.Id,
            Payload = mapper.Map<TEntity, TResponsePayload>(entity),
            StatusCode = StatusCodes.Status201Created,
        };
}
