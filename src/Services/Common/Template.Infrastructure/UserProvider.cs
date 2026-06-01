// ----------------------------------------------------------------------------------------------
// <copyright file="UserProvider.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Auth;

namespace Template.Infrastructure;

/// <summary>
/// Провайдер информации о текущем пользователе.
/// </summary>
public class UserProvider
    : IUserProvider
{
    /// <inheritdoc />
    public Guid UserId => Guid.Parse("db6903bb-0261-4bfc-a614-1f1ee8aedbbb");

    /// <inheritdoc />
    public string UserFullName => "User";
}
