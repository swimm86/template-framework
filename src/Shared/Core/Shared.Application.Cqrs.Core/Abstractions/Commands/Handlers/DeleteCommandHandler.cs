// ----------------------------------------------------------------------------------------------
// <copyright file="DeleteCommandHandler.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Shared.Application.Core.Dal.Repository.Interfaces;
using Shared.Application.Core.Exceptions.Models;
using Shared.Application.Core.Mapping.Interfaces;
using Shared.Application.Cqrs.Core.Abstractions.Commands.Requests;
using Shared.Application.Cqrs.Core.Abstractions.Commands.Responses;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Cqrs.Core.Abstractions.Commands.Handlers;

/// <summary>
/// Обработчик команды удулаения
/// </summary>
/// <param name="loggerFactory">Фабрика логгеров.</param>
/// <typeparam name="TRequest">Тип запроса.</typeparam>
/// <typeparam name="TEntity">Тип сущности.</typeparam>
/// <typeparam name="TKey">Тип ключа.</typeparam>
/// <typeparam name="TDeleteDto">Тип ДТО удаления.</typeparam>
/// <typeparam name="TResponse">Тип ответа.</typeparam>
public abstract class DeleteCommandHandler<TRequest, TEntity, TKey, TDeleteDto, TResponse>(
    IRepository<TEntity> repository,
    IMapper mapper,
    ILoggerFactory loggerFactory)
    : RequestHandler<TRequest, TResponse>(loggerFactory)
    where TRequest : DeleteCommand<TKey, TResponse>
    where TEntity : class, IEntity
    where TResponse : DeleteResponse<TKey, TDeleteDto>, new()
{
    /// <inheritdoc />
    public override async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
    {
        await GuardAsync(request, cancellationToken).ConfigureAwait(false);
        var entity = await FindAsync(request).ConfigureAwait(false);
        var response = await DeleteAsync(entity, request).ConfigureAwait(false);

        return response;
    }

    /// <summary>
    /// Поиск ентити.
    /// </summary>
    /// <param name="request">Запрос</param>
    /// <returns>Ентити</returns>
    /// <exception cref="NotFoundException">Ошибка если ентити не найдена</exception>
    protected virtual async Task<TEntity> FindAsync(TRequest request)
    {
        var entity = await repository.GetAsync(request.Key!).ConfigureAwait(false);

        if (entity is null)
        {
            throw new NotFoundException($"Сущность не найдена. Поиск по ключу: {request.Key}");
        }

        return entity;
    }

    /// <summary>
    /// Удаление ентити.
    /// </summary>
    /// <param name="entity">Ентити</param>
    /// <param name="request">Запрос</param>
    /// <returns>Ответ</returns>
    protected virtual async Task<TResponse> DeleteAsync(TEntity entity, TRequest request)
    {
        await repository.RemoveAsync(entity).ConfigureAwait(false);
        await repository.SaveChangesAsync().ConfigureAwait(false);

        return new TResponse { Key = request.Key, Result = mapper.Map<TEntity, TDeleteDto>(entity) };
    }
}
