// ----------------------------------------------------------------------------------------------
// <copyright file="UserProvider.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Auth;

namespace Gpn.Template.Application.Auth;

/// <summary>
/// Провайдер пользователей.
/// </summary>
/// <param name="userProvider">Провайдер пользователей из Admin.Auth.Sdk.</param>
public class UserProvider(
    Contour.Admin.Auth.Sdk.Context.IUserProvider userProvider)
    : IUserProvider
{
    /// <inheritdoc />
    public Guid UserId => userProvider?.GetUserId() ?? Guid.Empty;

    /// <inheritdoc />
    public string UserFullName => userProvider?.GetUserFullName() ?? "Иван Иванович";
}
