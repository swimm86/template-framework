// ----------------------------------------------------------------------------------------------
// <copyright file="IWithDeleted.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Interfaces;

/// <summary>
/// Интерфейс удаления.
/// </summary>
public interface IWithDeleted
{
    /// <summary>
    /// Кем удалено.
    /// </summary>
    Guid? DeletedByUserId { get; }

    /// <summary>
    /// Дата удаления.
    /// </summary>
    DateTime? DateDeleted { get; }

    /// <summary>
    /// Удален.
    /// </summary>
    bool IsDeleted { get; }

    /// <summary>
    /// Метод установки.
    /// </summary>
    /// <param name="deletedByUserId">Идентификатор пользователя, который является инициатором удаления.</param>
    void SetDeletedByUserId(Guid? deletedByUserId);

    /// <summary>
    /// Метод установки.
    /// </summary>
    /// <param name="dateDeleted">Дата удаления.</param>
    void SetDateDeleted(DateTime? dateDeleted);

    /// <summary>
    /// Делает полезные вещи при удалении.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    void OnDelete(Guid? userId);

    /// <summary>
    /// Установить в состояние - удален.
    /// </summary>
    void SetIsDeleted();
}
