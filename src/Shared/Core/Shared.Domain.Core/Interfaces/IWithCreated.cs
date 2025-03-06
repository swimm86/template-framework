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
    /// Имя пользователя, который создал сущность.
    /// </summary>
    string? CreatedByUserName { get; }

    /// <summary>
    /// Метод установки.
    /// </summary>
    /// <param name="createdByUserId">Идентификатор пользователя.</param>
    void SetCreatedByUserId(Guid? createdByUserId);

    /// <summary>
    /// Устанавливает имя пользователя, который создал сущность.
    /// </summary>
    /// <param name="userName">Имя пользователя, который создал сущность.</param>
    void SetCreatedByUserName(string userName);

    /// <summary>
    /// Делает полезные вещи при создании.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="userName">Имя пользователя, который создал сущность.</param>
    void OnCreate(Guid? userId, string? userName);
}
