// ----------------------------------------------------------------------------------------------
// <copyright file="ISetterRepository.Add.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Dal.Repository.Interfaces;

/// <inheritdoc cref="ISetterRepository{TEntity}"/>
public partial interface ISetterRepository<TEntity>
    where TEntity : class, IEntity
{
    /// <summary>
    /// Асинхронно добавляет экземпляр сущности в БД.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="entity">Экземпляр сущности.</param>
    /// <param name="userId">Id пользователя, добавившего запись.</param>
    /// <param name="userName">Имя пользователя, добавившего запись.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Экземпляр созданной сущности.</returns>
    Task<TEntity> AddAsync(
        TEntity entity,
        Guid? userId = null,
        string? userName = null,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="AddAsync(TEntity, Guid?, string?, CancellationToken)"/>
    /// <param name="entities">Коллекция экземпляров сущности.</param>
    /// <param name="userId"/><param name="userName"/><param name="cancellationToken"/>
    Task AddRangeAsync(
        IEnumerable<TEntity> entities,
        Guid? userId = null,
        string? userName = null,
        CancellationToken cancellationToken = default);
}
