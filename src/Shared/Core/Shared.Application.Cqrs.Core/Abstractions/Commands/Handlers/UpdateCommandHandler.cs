// ----------------------------------------------------------------------------------------------
// <copyright file="UpdateCommandHandler.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Dal.Repository.Interfaces;
using Shared.Application.Core.Dal.Repository.Models;
using Shared.Application.Core.Exceptions.Models;
using Shared.Application.Core.Mapping.Interfaces;
using Shared.Application.Cqrs.Core.Abstractions.Commands.Requests;
using Shared.Application.Cqrs.Core.Abstractions.Commands.Responses;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Cqrs.Core.Abstractions.Commands.Handlers;

/// <summary>
/// Обработчик изменения.
/// </summary>
/// <typeparam name="TRequest"> Тип запроса. </typeparam>
/// <typeparam name="TEntity"> Тип сущности. </typeparam>
/// <typeparam name="TKey"> Тип ключа. </typeparam>
/// <typeparam name="TUpdateDto"> Тип dto изменения. </typeparam>
/// <typeparam name="TResponse"> Тип ответа. </typeparam>
/// <param name="loggerFactory"> Логгер. </param>
public abstract class UpdateCommandHandler<TRequest, TEntity, TKey, TUpdateDto, TResponse>(
    ILoggerFactory loggerFactory,
    IMapper mapper,
    IRepository<TEntity> repository,
    IEnumerable<IValidator<TEntity>> validators)
    : RequestHandler<TRequest, TResponse>(loggerFactory)
    where TRequest : UpdateCommand<TKey, TUpdateDto, TResponse>
    where TEntity : class, IEntity
    where TResponse : UpdateResponse<TKey, TUpdateDto>, new()
{
    /// <summary>
    /// Обработчик.
    /// </summary>
    /// <param name="request"> Запрос. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <returns> Ответ. </returns>
    public override async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
    {
        await GuardAsync(request, cancellationToken).ConfigureAwait(false);
        var entity = await FindAsync(request, cancellationToken).ConfigureAwait(false);
        var response = await UpdateAsync(request, entity, cancellationToken).ConfigureAwait(false);
        return response;
    }

    /// <summary>
    /// Поиск сущности.
    /// </summary>
    /// <param name="request">  Запрос. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    /// <returns> Сущность. </returns>
    /// <exception cref="NullReferenceException"> Вызывается, когда сущность не найдена. </exception>
    protected virtual async Task<TEntity> FindAsync(TRequest request, CancellationToken cancellationToken)
    {
        var entity = await repository.GetAsync(request.Key!, ConstructOptions(request)).ConfigureAwait(false);
        if (entity is null)
        {
            throw new NotFoundException($"Сущность не найдена. Поиск по ключу: {request.Key}");
        }

        return entity;
    }

    /// <summary>
    /// Обновление сущности.
    /// </summary>
    /// <param name="request"> Запрос. </param>
    /// <param name="entity"> Сущность. </param>
    /// <param name="cancellationToken"> Токен отмены. </param>
    protected virtual async Task<TResponse> UpdateAsync(
    TRequest request,
        TEntity entity,
        CancellationToken cancellationToken)
    {
        mapper.Map(request.Dto, entity);
        await ValidateAsync(entity, validators, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync().ConfigureAwait(false);
        return new TResponse { Key = request.Key, Result = mapper.Map<TEntity, TUpdateDto>(entity) };
    }

    /// <summary>
    /// Построение параметров для запроса.
    /// </summary>
    /// <param name="request">Запрос.</param>
    /// <returns>Параметры для запроса.</returns>
    protected virtual QueryOptions<TEntity>? ConstructOptions(TRequest request) => null;
}
