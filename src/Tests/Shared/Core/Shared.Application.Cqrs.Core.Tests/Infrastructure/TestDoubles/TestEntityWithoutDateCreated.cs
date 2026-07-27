// ----------------------------------------------------------------------------------------------
// <copyright file="TestEntityWithoutDateCreated.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Base;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public sealed class TestEntityWithoutDateCreated : EntityBase<Guid>
{
    public string Name { get; set; } = string.Empty;
}
