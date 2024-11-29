// ----------------------------------------------------------------------------------------------
// <copyright file="IEntityWithUserData.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Interfaces;

/// <summary>
/// Интерфейс содержащий более подробные данные о пользователе.
/// </summary>
public interface IEntityWithUserData
{
    /// <summary>
    /// Имя кем создано.
    /// </summary>
    public string? CreatedByUserName { get; }

    /// <summary>
    /// Имя кем обновлено.
    /// </summary>
    public string? UpdatedByUserName { get; }

    /// <summary>
    /// Установка данных пользователя.
    /// </summary>
    /// <param name="name">Имя.</param>
    void SetCreatedByUserName(string name);

    /// <summary>
    /// Установка данных пользователя.
    /// </summary>
    /// <param name="name">Имя.</param>
    void SetUpdatedByUserName(string name);
}
