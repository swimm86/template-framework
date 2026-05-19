// ----------------------------------------------------------------------------------------------
// <copyright file="IWithUpdated.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Interfaces;

/// <summary>
/// Определяет контракт для объектов, поддерживающих отслеживание информации об обновлении.
/// </summary>
public interface IWithUpdated : IWithDateUpdated
{
    /// <summary>
    /// Кем обновлено.
    /// </summary>
    Guid? UpdatedByUserId { get; }

    /// <summary>
    /// Устанавливает идентификатор пользователя, обновившего сущность.
    /// </summary>
    /// <param name="updatedByUserId">Идентификатор пользователя, который является инициатором обновления.</param>
    void SetUpdatedByUserId(Guid? updatedByUserId);

    /// <summary>
    /// Инициализирует данные обновления сущности.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    void OnUpdate(Guid? userId);
}
