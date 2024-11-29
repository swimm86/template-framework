// ----------------------------------------------------------------------------------------------
// <copyright file="IWithCreated.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Interfaces;

/// <summary>
/// Интерфейс создания.
/// </summary>
public interface IWithCreated : IWithDateCreated
{
    /// <summary>
    /// Кем создано.
    /// </summary>
    Guid? CreatedByUserId { get; }

    /// <summary>
    /// Метод установки.
    /// </summary>
    /// <param name="createdByUserId">Идентификатор пользователя.</param>
    void SetCreatedByUserId(Guid? createdByUserId);

    /// <summary>
    /// Делает полезные вещи при создании.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    void OnCreate(Guid? userId);
}
