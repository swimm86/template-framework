// ----------------------------------------------------------------------------------------------
// <copyright file="TestServiceProvider.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

public sealed class TestServiceProvider : IServiceProvider
{
    public object? GetService(Type serviceType) => null;
}
