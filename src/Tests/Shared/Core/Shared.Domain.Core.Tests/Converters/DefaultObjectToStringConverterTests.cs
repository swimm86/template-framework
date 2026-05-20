using Shared.Domain.Core.Converters;
using Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Domain.Core.Tests.Converters;

/// <summary>
/// Тесты для стандартного конвертера объектов в строковое представление.
/// </summary>
public sealed class DefaultObjectToStringConverterTests
{
    private readonly DefaultObjectToStringConverter _sut = new();

    /// <summary>
    /// Проверяет, что конвертация null возвращает пустую строку.
    /// </summary>
    [Fact]
    public void Convert_Null_ReturnsEmpty()
    {
        // Arrange

        // Act
        var result = _sut.Convert(null);

        // Assert
        result.Should().Be(string.Empty);
    }

    /// <summary>
    /// Проверяет, что конвертация строки возвращает ту же строку.
    /// </summary>
    [Fact]
    public void Convert_StringValue_ReturnsSame()
    {
        // Arrange

        // Act
        var result = _sut.Convert("Hello World");

        // Assert
        result.Should().Be("Hello World");
    }

    /// <summary>
    /// Проверяет, что конвертация true возвращает локализованное "Да".
    /// </summary>
    [Fact]
    public void Convert_BoolTrue_ReturnsLocalizedYes()
    {
        // Arrange

        // Act
        var result = _sut.Convert(true);

        // Assert
        result.Should().Be("Да");
    }

    /// <summary>
    /// Проверяет, что конвертация false возвращает локализованное "Нет".
    /// </summary>
    [Fact]
    public void Convert_BoolFalse_ReturnsLocalizedNo()
    {
        // Arrange

        // Act
        var result = _sut.Convert(false);

        // Assert
        result.Should().Be("Нет");
    }

    /// <summary>
    /// Проверяет, что конвертация Guid возвращает строку в формате N (без дефисов, 32 символа).
    /// </summary>
    [Fact]
    public void Convert_Guid_ReturnsNFormat()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var result = _sut.Convert(guid);

        // Assert
        result.Should().HaveLength(32);
        result.Should().NotContain("-");
        result.Should().Be(guid.ToString("N"));
    }

    /// <summary>
    /// Проверяет, что конвертация перечисления с атрибутом Description возвращает текст описания.
    /// </summary>
    [Fact]
    public void Convert_EnumWithDescription_ReturnsDescriptionText()
    {
        // Arrange

        // Act
        var result = _sut.Convert(TestEnumWithDescription.FirstValue);

        // Assert
        result.Should().Be("Первое значение");
    }

    /// <summary>
    /// Проверяет, что конвертация DateTime возвращает дату в ISO-формате (yyyy-MM-dd).
    /// </summary>
    [Fact]
    public void Convert_DateTime_ReturnsIsoDateFormat()
    {
        // Arrange
        var dt = new DateTime(2025, 5, 18);

        // Act
        var result = _sut.Convert(dt);

        // Assert
        result.Should().Be("2025-05-18");
    }

    /// <summary>
    /// Проверяет, что конвертация DateOnly возвращает дату в ISO-формате (yyyy-MM-dd).
    /// </summary>
    [Fact]
    public void Convert_DateOnly_ReturnsIsoDateFormat()
    {
        // Arrange
        var d = new DateOnly(2025, 5, 18);

        // Act
        var result = _sut.Convert(d);

        // Assert
        result.Should().Be("2025-05-18");
    }
}
