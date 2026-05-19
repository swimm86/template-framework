// ----------------------------------------------------------------------------------------------
// <copyright file="IEntityWithUserData.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
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
    /// Устанавливает имя пользователя, создавшего сущность.
    /// </summary>
    /// <param name="name">Имя пользователя.</param>
    void SetCreatedByUserName(string name);

    /// <summary>
    /// Устанавливает имя пользователя, обновившего сущность.
    /// </summary>
    /// <param name="name">Имя пользователя.</param>
    void SetUpdatedByUserName(string name);
}
