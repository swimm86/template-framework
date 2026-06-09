// ----------------------------------------------------------------------------------------------
// <copyright file="HashHelperTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;
using Shared.Common.Helpers;

namespace Shared.Common.Tests.Helpers;

/// <summary>
/// Тесты для вспомогательного класса <see cref="HashHelper"/>.
/// Проверяют детерминированность, регистронезависимость и коллизионную устойчивость SHA-256.
/// </summary>
public sealed class HashHelperTests
{
    /// <summary>
    /// SHA-256 всегда возвращает 32 байта (256 бит).
    /// </summary>
    [Fact]
    public void ComputeSha256_AnyInput_Returns32Bytes()
    {
        // Act
        var result = HashHelper.ComputeSha256("name", "email@example.com");

        // Assert
        result.Should().HaveCount(32);
    }

    /// <summary>
    /// Для одинакового набора компонентов хэш детерминирован между вызовами.
    /// </summary>
    [Fact]
    public void ComputeSha256_SameInputs_ReturnsIdenticalHash()
    {
        // Act
        var first = HashHelper.ComputeSha256("Alice", "alice@example.com");
        var second = HashHelper.ComputeSha256("Alice", "alice@example.com");

        // Assert
        first.Should().Equal(second);
    }

    /// <summary>
    /// Эталонный вектор: SHA-256("alice|alice@example.com") в UTF-8 без BOM,
    /// с trim и lower-invariant. Проверяет совпадение с независимым вычислением.
    /// </summary>
    [Fact]
    public void ComputeSha256_KnownInput_MatchesIndependentComputation()
    {
        // Arrange
        var expected = SHA256.HashData("alice|alice@example.com"u8.ToArray());

        // Act
        var actual = HashHelper.ComputeSha256("alice", "alice@example.com");

        // Assert
        actual.Should().Equal(expected);
    }

    /// <summary>
    /// Разный порядок компонентов при равном наборе даёт разные хэши (защита от коллизий).
    /// </summary>
    [Fact]
    public void ComputeSha256_DifferentPartOrder_ProducesDifferentHash()
    {
        // Act
        var ab = HashHelper.ComputeSha256("a", "b");
        var ba = HashHelper.ComputeSha256("b", "a");

        // Assert
        ab.Should().NotEqual(ba);
    }

    /// <summary>
    /// Изменение одного компонента полностью меняет хэш (лавинный эффект).
    /// </summary>
    [Fact]
    public void ComputeSha256_AnyComponentChanged_ProducesDifferentHash()
    {
        // Act
        var baseline = HashHelper.ComputeSha256("Alice", "alice@example.com");
        var nameChanged = HashHelper.ComputeSha256("Bob", "alice@example.com");
        var emailChanged = HashHelper.ComputeSha256("Alice", "bob@example.com");

        // Assert
        nameChanged.Should().NotEqual(baseline);
        emailChanged.Should().NotEqual(baseline);
    }

    /// <summary>
    /// Нормализация: пробелы по краям и регистр игнорируются, чтобы уникальность
    /// работала вне зависимости от способа ввода.
    /// </summary>
    /// <param name="name">Имя в различных вариантах написания.</param>
    /// <param name="email">Email в различных вариантах написания.</param>
    [Theory]
    [InlineData("Alice", "alice@example.com")]
    [InlineData("  Alice  ", "alice@example.com")]
    [InlineData("ALICE", "Alice@Example.com")]
    [InlineData("alice", "  ALICE@example.COM  ")]
    public void ComputeSha256_NormalizesCaseAndWhitespace_ReturnsSameHash(string name, string email)
    {
        // Arrange
        var baseline = HashHelper.ComputeSha256("alice", "alice@example.com");

        // Act
        var actual = HashHelper.ComputeSha256(name, email);

        // Assert
        actual.Should().Equal(baseline);
    }

    /// <summary>
    /// Хэш стабилен между вызовами, даже если значение содержит символ-разделитель внутри.
    /// Документирует поведение: разделитель '|' не экранируется, поэтому значения
    /// <c>"a|b"</c> и <c>["a", "b"]</c> коллизируют — это сознательный компромисс
    /// ради простоты API; входные данные должны быть свободны от разделителя.
    /// </summary>
    [Fact]
    public void ComputeSha256_SeparatorInsideComponent_IsDeterministic()
    {
        // Act
        var first = HashHelper.ComputeSha256("a|b");
        var second = HashHelper.ComputeSha256("a|b");

        // Assert
        first.Should().Equal(second);
    }

    /// <summary>
    /// Пустые и null-компоненты обрабатываются как пустые строки, не вызывая исключений.
    /// </summary>
    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    [InlineData("Alice", null)]
    [InlineData(null, "alice@example.com")]
    public void ComputeSha256_NullOrWhitespaceComponents_DoNotThrowAndReturn32Bytes(
        string? name,
        string? email)
    {
        // Act
        var result = HashHelper.ComputeSha256(name, email);

        // Assert
        result.Should().HaveCount(32);
    }

    /// <summary>
    /// Передача null в массив components приводит к <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void ComputeSha256_NullArray_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => HashHelper.ComputeSha256(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Пустой массив компонентов формирует хэш от пустой строки — детерминированно и без исключений.
    /// </summary>
    [Fact]
    public void ComputeSha256_EmptyArray_ReturnsHashOfEmptyString()
    {
        // Arrange
        var expected = SHA256.HashData(Encoding.UTF8.GetBytes(string.Empty));

        // Act
        var actual = HashHelper.ComputeSha256();

        // Assert
        actual.Should().Equal(expected);
    }

    /// <summary>
    /// Метод возвращает новый массив при каждом вызове — вызывающий код может безопасно
    /// хранить ссылку, не опасаясь мутации изнутри хелпера.
    /// </summary>
    [Fact]
    public void ComputeSha256_ReturnsFreshArrayInstance()
    {
        // Act
        var first = HashHelper.ComputeSha256("Alice", "alice@example.com");
        var second = HashHelper.ComputeSha256("Alice", "alice@example.com");

        // Assert
        first.Should().NotBeSameAs(second);
    }
}
