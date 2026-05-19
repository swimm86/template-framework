using Shared.Domain.Core.Exceptions.Models.Base;
using Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Domain.Core.Tests.Exceptions.Models.Base;

/// <summary>
/// Модульные тесты для <see cref="Shared.Domain.Core.Exceptions.Models.Base.AppException"/>.
/// </summary>
public sealed class AppExceptionTests
{
    /// <summary>
    /// Проверяет, что <see cref="AppException"/> с пустым словарём
    /// выбрасывает <see cref="ArgumentException"/> с именем параметра <c>additionalData</c>.
    /// </summary>
    [Fact]
    public void Constructor_WithEmptyDictionary_ThrowsArgumentException()
    {
        // Arrange
        var emptyData = new Dictionary<string, object>();

        // Act
        Action act = () => { _ = new TestAppException("message", emptyData); };

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("additionalData");
    }

    /// <summary>
    /// Проверяет, что <see cref="AppException"/> с пустым строковым ключом в словаре
    /// выбрасывает <see cref="ArgumentException"/> с именем параметра <c>additionalData</c>.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithNullOrEmptyKeyInDictionary_ThrowsArgumentException(string key)
    {
        // Arrange
        var data = new Dictionary<string, object> { { key, "value" } };

        // Act
        Action act = () => _ = new TestAppException("message", data);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("additionalData");
    }

    /// <summary>
    /// Проверяет, что параметр-конструктор <see cref="AppException"/> без параметров
    /// не выбрасывает исключений.
    /// </summary>
    [Fact]
    public void Constructor_Default_DoesNotThrow()
    {
        // Act
        Action act = () => _ = new TestAppException();

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Проверяет, что <see cref="AppException"/> сохраняет сообщение,
    /// переданное в конструктор.
    /// </summary>
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        // Arrange
        const string expected = "test message";

        // Act
        var exception = new TestAppException(expected);

        // Assert
        exception.Message.Should().Be(expected);
    }

    /// <summary>
    /// Проверяет, что <see cref="AppException.AdditionalData"/> возвращает
    /// переданные данные как <see cref="IReadOnlyDictionary{TKey,TValue}"/>
    /// и содержит ожидаемые значения.
    /// </summary>
    [Fact]
    public void Constructor_WithAdditionalData_ExposesReadOnly()
    {
        // Arrange
        var data = new Dictionary<string, object> { { "key", "value" } };

        // Act
        var exception = new TestAppException("message", data);

        // Assert
        exception.AdditionalData.Should().NotBeNull();
        exception.AdditionalData.Should().BeAssignableTo<IReadOnlyDictionary<string, object>>();
        exception.AdditionalData.Should().ContainKey("key").WhoseValue.Should().Be("value");
    }

    /// <summary>
    /// Проверяет, что <see cref="AppException"/> сохраняет внутреннее исключение,
    /// переданное в конструктор.
    /// </summary>
    [Fact]
    public void Constructor_WithInnerException_SetsInner()
    {
        // Arrange
        var inner = new InvalidOperationException("inner error");

        // Act
        var exception = new TestAppException("outer message", inner);

        // Assert
        exception.InnerException.Should().Be(inner);
    }
}
