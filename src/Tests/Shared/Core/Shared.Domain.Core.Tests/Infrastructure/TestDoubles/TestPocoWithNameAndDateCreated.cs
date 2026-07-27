// ----------------------------------------------------------------------------------------------
// <copyright file="TestPocoWithNameAndDateCreated.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

internal sealed class TestPocoWithNameAndDateCreated
{
    public string Name { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; }
}
