// ----------------------------------------------------------------------------------------------
// <copyright file="IUserProvider.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Auth;

/// <summary>
/// Интерфейс провайдера информации о текущем пользователе.
/// </summary>
public interface IUserProvider
{
    /// <summary>
    /// Возвращает идентификатор пользователя.
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// Возвращает полное имя пользователя.
    /// </summary>
    string UserFullName { get; }
}
