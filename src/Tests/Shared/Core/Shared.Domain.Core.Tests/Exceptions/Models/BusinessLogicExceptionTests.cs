using Shared.Domain.Core.Exceptions.Models;
using Shared.Domain.Core.Exceptions.Models.Base;

namespace Shared.Domain.Core.Tests.Exceptions.Models;

/// <summary>
/// Модульные тесты для <see cref="BusinessLogicException"/>.
/// </summary>
public sealed class BusinessLogicExceptionTests
{
    /// <summary>
    /// Проверяет, что <see cref="BusinessLogicException"/> сохраняет сообщение
    /// и является наследником <see cref="AppException"/>.
    /// </summary>
    [Fact]
    public void Constructor_WithMessage_SetsMessageAndPreservesInBase()
    {
        // Arrange
        const string expected = "business error";

        // Act
        var exception = new BusinessLogicException(expected);

        // Assert
        exception.Message.Should().Be(expected);
        exception.Should().BeAssignableTo<AppException>();
    }

    /// <summary>
    /// Проверяет, что <see cref="BusinessLogicException"/> с сообщением и
    /// дополнительными данными корректно передаёт их в базовый класс.
    /// </summary>
    [Fact]
    public void Constructor_WithMessageAndAdditionalData_PassesToBase()
    {
        // Arrange
        var data = new Dictionary<string, object> { { "code", 42 } };

        // Act
        var exception = new BusinessLogicException("business error", data);

        // Assert
        exception.Message.Should().Be("business error");
        exception.AdditionalData.Should().NotBeNull();
        exception.AdditionalData.Should().ContainKey("code").WhoseValue.Should().Be(42);
    }

    /// <summary>
    /// Проверяет, что <see cref="BusinessLogicException"/> с сообщением,
    /// внутренним исключением и дополнительными данными корректно передаёт
    /// все параметры в базовый класс.
    /// </summary>
    [Fact]
    public void Constructor_WithMessageAndInnerException_PassesToBase()
    {
        // Arrange
        var inner = new InvalidOperationException("inner");
        var data = new Dictionary<string, object> { { "code", 42 } };

        // Act
        var exception = new BusinessLogicException("business error", inner, data);

        // Assert
        exception.Message.Should().Be("business error");
        exception.InnerException.Should().Be(inner);
        exception.AdditionalData.Should().NotBeNull();
        exception.AdditionalData.Should().ContainKey("code").WhoseValue.Should().Be(42);
    }
}
