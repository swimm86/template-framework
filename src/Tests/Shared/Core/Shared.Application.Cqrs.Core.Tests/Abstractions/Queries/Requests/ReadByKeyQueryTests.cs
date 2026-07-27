// ----------------------------------------------------------------------------------------------
// <copyright file="ReadByKeyQueryTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Cqrs.Core.Abstractions.Queries.Requests;
using Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Application.Cqrs.Core.Tests.Abstractions.Queries.Requests;

/// <summary>
/// Тесты для <see cref="ReadByKeyQuery{TResponse}"/>: конструктор, допустимость null, read-only свойство.
/// </summary>
public sealed class ReadByKeyQueryTests
{
    /// <summary>
    /// Конструктор присваивает переданный ключ свойству <see cref="ReadByKeyQuery{TKey}.Key"/>.
    /// </summary>
    [Fact]
    public void Constructor_AssignsKey()
    {
        // Arrange
        var query = new TestReadByKeyQuery("key-123");

        // Assert
        query.Key.Should().Be("key-123");
    }

    /// <summary>
    /// Передача <see langword="null"/> в конструктор не вызывает исключения.
    /// </summary>
    [Fact]
    public void Constructor_NullKey_DoesNotThrow()
    {
        // Arrange
        var query = new TestReadByKeyQuery(null!);

        // Act
        query.Key.Should().BeNull();
    }
}
