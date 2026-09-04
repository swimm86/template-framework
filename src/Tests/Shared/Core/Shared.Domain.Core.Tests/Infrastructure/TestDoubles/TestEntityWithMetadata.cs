// ----------------------------------------------------------------------------------------------
// <copyright file="TestEntityWithMetadata.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Base;

namespace Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

public class TestEntityWithMetadata : EntityWithMetadata<TestEntityWithMetadata, Guid>
{
    public string Name { get; set; } = string.Empty;
}
