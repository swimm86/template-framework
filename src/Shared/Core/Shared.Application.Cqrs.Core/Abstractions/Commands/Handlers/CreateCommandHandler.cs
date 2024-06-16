// ----------------------------------------------------------------------------------------------
// <copyright file="CreateCommandHandler.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Dal.Repository.Interfaces;
using Shared.Application.Core.Mapping.Interfaces;
using Shared.Application.Cqrs.Core.Abstractions.Commands.Requests;
using Shared.Application.Cqrs.Core.Abstractions.Commands.Responses;
using Shared.Application.Cqrs.Core.Utils.Mappers.EntityMapper;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Cqrs.Core.Abstractions.Commands.Handlers;

/// <summary>
/// Создание обработчика.
/// </summary>
/// <typeparam name="TRequest"> Тип запроса. </typeparam>
/// <typeparam name="TEntity"> Тип сущности. </typeparam>
/// <typeparam name="TKey"> Тип ключа. </typeparam>
/// <typeparam name="TCreateDto"> Тип Dto при создании. </typeparam>
/// <typeparam name="TResponse"> Тип ответа. </typeparam>
/// <param name="loggerFactory"> Фабрика логгирования. </param>
public abstract class CreateCommandHandler<TRequest, TEntity, TKey, TCreateDto, TResponse>(
    ILoggerFactory loggerFactory,
    IEntityMapper<TCreateDto, TEntity> entityMapper,
    IMapper mapperService,
    IRepository<TEntity> repository,
    IEnumerable<IValidator<TEntity>> validators)
    : RequestHandler<TRequest, TResponse>(loggerFactory)
    where TRequest : CreateCommand<TCreateDto, TResponse>
    where TEntity : class, IEntity
    where TResponse : CreateResponse<TCreateDto>, new()
{
    /// <summary>
    /// Обработка.
    /// </summary>
    /// <param name="request"> Запрос. </param>
    /// <param name="cancellationToken"> Токен ответа. </param>
    /// <returns> Ответ. </returns>
    public override async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
    {
        await GuardAsync(request, cancellationToken).ConfigureAwait(false);
        var entity = ConstructEntity();
        var response = await CreateAsync(request, entity, cancellationToken).ConfigureAwait(false);

        return response;
    }

    /// <summary>
    /// Создание сущности.
    /// </summary>
    /// <returns> Сущность. </returns>
    protected abstract TEntity ConstructEntity();

    /// <summary>
    /// Создание ответа.
    /// </summary>
    /// <param name="request"> Запрос. </param>
    /// <param name="entity"> Сущность. </param>
    /// <param name="cancellationToken"> Токен ответа. </param>
    /// <returns> Ответ. </returns>
    protected virtual async Task<TResponse> CreateAsync(
        TRequest request,
        TEntity entity,
        CancellationToken cancellationToken)
    {
        entityMapper.Map(request.Dto, entity);

        await ValidateAsync(entity, validators, cancellationToken).ConfigureAwait(false);
        var result = await repository.AddAsync(entity).ConfigureAwait(false);
        return new TResponse { Id = result.Id, Result = CreateResponseDto(result) };
    }

    /// <summary>
    /// Создание dto ответа.
    /// </summary>
    /// <param name="entity"> Сущность. </param>
    /// <returns> Dto ответа. </returns>
    protected virtual TCreateDto CreateResponseDto(TEntity entity)
        => mapperService.Map<TEntity, TCreateDto>(entity);
}
