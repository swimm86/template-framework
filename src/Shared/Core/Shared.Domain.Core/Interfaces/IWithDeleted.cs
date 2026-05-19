// ----------------------------------------------------------------------------------------------
// <copyright file="IWithDeleted.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Interfaces;

/// <summary>
/// Определяет контракт для объектов, поддерживающих мягкое удаление.
/// </summary>
public interface IWithDeleted : IWithDateDeleted
{
    /// <summary>
    /// Кем удалено.
    /// </summary>
    Guid? DeletedByUserId { get; }

    /// <summary>
    /// Признак удаления сущности.
    /// </summary>
    bool IsDeleted { get; }

    /// <summary>
    /// Устанавливает идентификатор пользователя, удалившего сущность.
    /// </summary>
    /// <param name="deletedByUserId">Идентификатор пользователя, который является инициатором удаления.</param>
    void SetDeletedByUserId(Guid? deletedByUserId);

    /// <summary>
    /// Инициализирует данные удаления сущности.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    void OnDelete(Guid? userId);

    /// <summary>
    /// Устанавливает признак удаления сущности.
    /// </summary>
    void SetIsDeleted();
}
