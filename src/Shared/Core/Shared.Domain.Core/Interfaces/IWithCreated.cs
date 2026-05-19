// ----------------------------------------------------------------------------------------------
// <copyright file="IWithCreated.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Interfaces;

/// <summary>
/// Определяет контракт для объектов, поддерживающих отслеживание информации о создании.
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
    /// Устанавливает идентификатор пользователя, создавшего сущность.
    /// </summary>
    /// <param name="createdByUserId">Идентификатор пользователя.</param>
    void SetCreatedByUserId(Guid? createdByUserId);

    /// <summary>
    /// Устанавливает имя пользователя, который создал сущность.
    /// </summary>
    /// <param name="userName">Имя пользователя, который создал сущность.</param>
    void SetCreatedByUserName(string userName);

    /// <summary>
    /// Инициализирует данные создания сущности.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="userName">Имя пользователя, который создал сущность.</param>
    void OnCreate(Guid? userId, string? userName);
}
