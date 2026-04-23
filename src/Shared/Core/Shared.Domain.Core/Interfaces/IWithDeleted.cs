// ----------------------------------------------------------------------------------------------
// <copyright file="IWithDeleted.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Interfaces;

/// <summary>
/// Интерфейс удаления.
/// </summary>
public interface IWithDeleted : IWithDateDeleted
{
    /// <summary>
    /// Кем удалено.
    /// </summary>
    Guid? DeletedByUserId { get; }

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
    /// Делает полезные вещи при удалении.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    void OnDelete(Guid? userId);

    /// <summary>
    /// Установить в состояние - удален.
    /// </summary>
    void SetIsDeleted();
}
