// ----------------------------------------------------------------------------------------------
// <copyright file="IWithDeleteAction.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;

namespace Shared.Domain.Core.Interfaces;

/// <summary>
/// Интерфейс для сущностей, которые можно удалять.
/// </summary>
/// <typeparam name="TEntity">Тип сущности.</typeparam>
public interface IWithDeleteAction<TEntity>
    where TEntity : class, IEntity
{
    /// <summary>
    /// Выполняет удаление сущности.
    /// </summary>
    /// <param name="unitOfWork"><see cref="IUnitOfWork"/>.</param>
    /// <param name="soft">Признак того, что сущность должна быть удалена не физически.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task DeleteAsync(
        IUnitOfWork unitOfWork,
        bool soft = true);

    /// <summary>
    /// Выполняет удаление сущности.
    /// </summary>
    /// <param name="repository"><see cref="IRepository{TEntity}"/>.</param>
    /// <param name="soft">Признак того, что сущность должна быть удалена не физически.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task DeleteAsync(
        IRepository<TEntity> repository,
        bool soft = true);
}
