// ----------------------------------------------------------------------------------------------
// <copyright file="TypeExtensionsTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Collections;
using Shared.Common.Extensions;

namespace Shared.Common.Tests.Extensions;

/// <summary>
/// Тесты для класса расширения типов <see cref="Shared.Common.Extensions.TypeExtensions"/>.
/// </summary>
public sealed class TypeExtensionsTests
{
    #region Test Types

    private interface ITestInterface { }

    private sealed class TestClass : ITestInterface
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    private sealed class NonImplementingClass { }

    #endregion

    #region ImplementsIEnumerable Tests

    /// <summary>
    /// Проверяет, что <see cref="Shared.Common.Extensions.TypeExtensions.ImplementsIEnumerable"/> возвращает true для <see cref="List{T}"/>.
    /// </summary>
    [Fact]
    public void ImplementsIEnumerable_GenericList_ReturnsTrue()
    {
        // Act
        var result = typeof(List<int>).ImplementsIEnumerable();

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что <see cref="Shared.Common.Extensions.TypeExtensions.ImplementsIEnumerable"/> возвращает true для <see cref="string"/> (string реализует IEnumerable&lt;char&gt; через интерфейсы).
    /// </summary>
    [Fact]
    public void ImplementsIEnumerable_String_ReturnsTrue()
    {
        // Act
        var result = typeof(string).ImplementsIEnumerable();

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что <see cref="Shared.Common.Extensions.TypeExtensions.ImplementsIEnumerable"/> возвращает true для массива.
    /// </summary>
    [Fact]
    public void ImplementsIEnumerable_Array_ReturnsTrue()
    {
        // Act
        var result = typeof(int[]).ImplementsIEnumerable();

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что <see cref="Shared.Common.Extensions.TypeExtensions.ImplementsIEnumerable"/> возвращает false для обычного класса.
    /// </summary>
    [Fact]
    public void ImplementsIEnumerable_NonCollectionType_ReturnsFalse()
    {
        // Act
        var result = typeof(TestClass).ImplementsIEnumerable();

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Проверяет, что <see cref="Shared.Common.Extensions.TypeExtensions.ImplementsIEnumerable"/> возвращает true для <see cref="IEnumerable"/>.
    /// </summary>
    [Fact]
    public void ImplementsIEnumerable_DirectlyImplementsIEnumerable_ReturnsTrue()
    {
        // Act
        var result = typeof(IEnumerable).ImplementsIEnumerable();

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что <see cref="Shared.Common.Extensions.TypeExtensions.ImplementsIEnumerable"/> возвращает true для <see cref="IEnumerable{T}"/>.
    /// </summary>
    [Fact]
    public void ImplementsIEnumerable_GenericIEnumerable_ReturnsTrue()
    {
        // Act
        var result = typeof(IEnumerable<int>).ImplementsIEnumerable();

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что <see cref="Shared.Common.Extensions.TypeExtensions.ImplementsIEnumerable"/> для null-типа возвращает false.
    /// </summary>
    [Fact]
    public void ImplementsIEnumerable_NullType_ReturnsFalse()
    {
        // Act
        var result = ((Type)null!).ImplementsIEnumerable();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetPropertyIgnoreCase Tests

    /// <summary>
    /// Проверяет поиск свойства с точным совпадением регистра.
    /// </summary>
    [Fact]
    public void GetPropertyIgnoreCase_ExactCaseMatch_ReturnsProperty()
    {
        // Act
        var result = typeof(TestClass).GetPropertyIgnoreCase("Name");

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Name");
    }

    /// <summary>
    /// Проверяет поиск свойства в нижнем регистре.
    /// </summary>
    [Fact]
    public void GetPropertyIgnoreCase_LowerCase_ReturnsProperty()
    {
        // Act
        var result = typeof(TestClass).GetPropertyIgnoreCase("name");

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Name");
    }

    /// <summary>
    /// Проверяет поиск свойства в верхнем регистре.
    /// </summary>
    [Fact]
    public void GetPropertyIgnoreCase_UpperCase_ReturnsProperty()
    {
        // Act
        var result = typeof(TestClass).GetPropertyIgnoreCase("NAME");

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Name");
    }

    /// <summary>
    /// Проверяет, что для несуществующего свойства возвращается null.
    /// </summary>
    [Fact]
    public void GetPropertyIgnoreCase_NonExistent_ReturnsNull()
    {
        // Act
        var result = typeof(TestClass).GetPropertyIgnoreCase("NonExistent");

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Проверяет, что при обрезании пробелов поиск работает корректно.
    /// </summary>
    [Fact]
    public void GetPropertyIgnoreCase_WithWhitespace_TrimsInput()
    {
        // Act
        var result = typeof(TestClass).GetPropertyIgnoreCase("  Name  ");

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Name");
    }

    #endregion
}
