// ----------------------------------------------------------------------------------------------
// <copyright file="IWithCreated.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Interfaces;

/// <summary>
/// Интерфейс создания.
/// </summary>
public interface IWithCreated
{
    /// <summary>
    /// Кем создано.
    /// </summary>
    Guid? CreatedByUserId { get; }

    /// <summary>
    /// Дата удаления.
    /// </summary>
    DateTime DateCreated { get; }

    /// <summary>
    /// Метод установки.
    /// </summary>
    /// <param name="createdByUserId">Идентификатор пользователя, который является инициатором создания.</param>
    void SetCreatedByUserId(Guid? createdByUserId);

    /// <summary>
    /// Метод установки.
    /// </summary>
    /// <param name="dateCreated">Дата создания.</param>
    void SetDateCreated(DateTime dateCreated);

    /// <summary>
    /// Делает полезные вещи при создании.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    void OnCreate(Guid? userId);
}
