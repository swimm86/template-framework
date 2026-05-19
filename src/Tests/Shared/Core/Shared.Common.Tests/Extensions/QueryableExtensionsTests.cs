// ----------------------------------------------------------------------------------------------
// <copyright file="QueryableExtensionsTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Common.Extensions;

namespace Shared.Common.Tests.Extensions;

/// <summary>
/// Тесты для класса расширения запросов <see cref="QueryableExtensions"/>.
/// </summary>
public sealed class QueryableExtensionsTests
{
    #region GetRange Tests

    /// <summary>
    /// Проверяет пропуск элементов без ограничения take.
    /// </summary>
    [Fact]
    public void GetRange_SkipOnly_SkipsElements()
    {
        // Arrange
        var source = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }.AsQueryable();

        // Act
        var result = source.GetRange(skip: 3).ToList();

        // Assert
        result.Should().Equal(4, 5, 6, 7, 8, 9, 10);
    }

    /// <summary>
    /// Проверяет ограничение количества элементов без skip.
    /// </summary>
    [Fact]
    public void GetRange_TakeOnly_TakesElements()
    {
        // Arrange
        var source = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }.AsQueryable();

        // Act
        var result = source.GetRange(take: 4).ToList();

        // Assert
        result.Should().Equal(1, 2, 3, 4);
    }

    /// <summary>
    /// Проверяет одновременный пропуск и ограничение количества элементов.
    /// </summary>
    [Fact]
    public void GetRange_SkipAndTake_SkipsAndTakes()
    {
        // Arrange
        var source = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }.AsQueryable();

        // Act
        var result = source.GetRange(skip: 3, take: 4).ToList();

        // Assert
        result.Should().Equal(4, 5, 6, 7);
    }

    /// <summary>
    /// Проверяет, что при null-параметрах возвращаются все элементы.
    /// </summary>
    [Fact]
    public void GetRange_NullParameters_ReturnsAllElements()
    {
        // Arrange
        var source = new[] { 1, 2, 3 }.AsQueryable();

        // Act
        var result = source.GetRange().ToList();

        // Assert
        result.Should().Equal(1, 2, 3);
    }

    /// <summary>
    /// Проверяет, что метод возвращает <see cref="IQueryable{T}"/>.
    /// </summary>
    [Fact]
    public void GetRange_ReturnsIQueryable()
    {
        // Arrange
        var source = new[] { 1, 2, 3 }.AsQueryable();

        // Act
        var result = source.GetRange(skip: 1, take: 2);

        // Assert
        result.Should().BeAssignableTo<IQueryable<int>>();
    }

    #endregion
}
