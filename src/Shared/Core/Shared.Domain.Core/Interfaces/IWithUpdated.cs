// ----------------------------------------------------------------------------------------------
// <copyright file="IWithUpdated.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Interfaces;

/// <summary>
/// Интерфейс обновления.
/// </summary>
public interface IWithUpdated : IWithDateUpdated
{
    /// <summary>
    /// Кем обновлено.
    /// </summary>
    Guid? UpdatedByUserId { get; }

    /// <summary>
    /// Метод установки.
    /// </summary>
    /// <param name="updatedByUserId">Идентификатор пользователя, который является инициатором обновления.</param>
    void SetUpdatedByUserId(Guid? updatedByUserId);

    /// <summary>
    /// Делает полезные вещи при обновлении.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    void OnUpdate(Guid? userId);
}
