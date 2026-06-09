// ----------------------------------------------------------------------------------------------
// <copyright file="TestBaseEntity.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Base;

namespace Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

/// <summary>
/// Тестовая сущность, наследующая <see cref="EntityBase{TKey}"/>, для smoke-тестов базового класса.
/// </summary>
public sealed class TestBaseEntity : EntityBase<Guid>
{
    /// <summary>
    /// Имя сущности.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
