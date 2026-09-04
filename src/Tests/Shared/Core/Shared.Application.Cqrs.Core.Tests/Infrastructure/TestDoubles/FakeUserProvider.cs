// ----------------------------------------------------------------------------------------------
// <copyright file="FakeUserProvider.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Auth;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public sealed class FakeUserProvider : IUserProvider
{
    public Guid UserId { get; set; } = Guid.NewGuid();

    public string UserFullName { get; set; } = "Test User";
}
