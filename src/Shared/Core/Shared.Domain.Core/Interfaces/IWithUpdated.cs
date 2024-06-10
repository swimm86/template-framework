// ----------------------------------------------------------------------------------------------
// <copyright file="IWithUpdated.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Interfaces;

/// <summary>
/// Интерфейс обновления.
/// </summary>
public interface IWithUpdated
{
    /// <summary>
    /// Кем обновлено.
    /// </summary>
    Guid? UpdatedByUserId { get; }

    /// <summary>
    /// Дата удаления.
    /// </summary>
    DateTime? DateUpdated { get; }

    /// <summary>
    /// Метод установки.
    /// </summary>
    /// <param name="updatedByUserId">Идентификатор пользователя, который является инициатором обновления.</param>
    void SetUpdatedByUserId(Guid? updatedByUserId);

    /// <summary>
    /// Метод установки.
    /// </summary>
    /// <param name="dateUpdated">Дата создания.</param>
    void SetDateUpdated(DateTime? dateUpdated);

    /// <summary>
    /// Делает полезные вещи при обновлении.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    void OnUpdate(Guid? userId);
}
