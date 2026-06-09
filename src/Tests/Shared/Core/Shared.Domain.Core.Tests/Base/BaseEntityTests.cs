// ----------------------------------------------------------------------------------------------
// <copyright file="BaseEntityTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Base;
using Shared.Domain.Core.Interfaces;
using Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Domain.Core.Tests.Base;

/// <summary>
/// Тесты для базового класса сущности <see cref="EntityBase{TKey}"/>.
/// Проверяют инициализацию идентификатора значением по умолчанию и присваиваемость извне.
/// </summary>
public sealed class BaseEntityTests
{
    /// <summary>
    /// Проверяет, что идентификатор по умолчанию равен <c>default(Guid)</c>
    /// (то есть <see cref="Guid.Empty"/>) до явной инициализации.
    /// </summary>
    [Fact]
    public void Id_Default_IsDefaultGuid()
    {
        // Arrange
        var entity = new TestBaseEntity();

        // Act
        var id = entity.Id;

        // Assert
        id.Should().Be(Guid.Empty);
    }

    /// <summary>
    /// Проверяет, что идентификатор можно установить через инициализатор.
    /// </summary>
    [Fact]
    public void Id_SetInInitializer_IsPersisted()
    {
        // Arrange
        var expectedId = Guid.NewGuid();

        // Act
        var entity = new TestBaseEntity { Id = expectedId };

        // Assert
        entity.Id.Should().Be(expectedId);
    }

    /// <summary>
    /// Проверяет, что <see cref="IEntity.Id"/> (непараметризованный) возвращает значение,
    /// совпадающее с типизированным <see cref="IEntity{T}.Id"/>.
    /// </summary>
    [Fact]
    public void NonGenericId_MatchesTypedId()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var entity = new TestBaseEntity { Id = expectedId };

        // Act
        object nonGenericId = ((IEntity)entity).Id;

        // Assert
        nonGenericId.Should().Be(expectedId);
    }

    /// <summary>
    /// Проверяет, что <see cref="EntityBase{TKey}"/> может использоваться как реализация
    /// <see cref="IEntity{T}"/> с указанным типом ключа.
    /// </summary>
    [Fact]
    public void EntityBase_ImplementsIEntityOfTKey()
    {
        // Arrange
        var entity = new TestBaseEntity();

        // Act
        IEntity<Guid> typed = entity;

        // Assert
        typed.Should().NotBeNull();
        typed.Id.Should().Be(entity.Id);
    }
}
