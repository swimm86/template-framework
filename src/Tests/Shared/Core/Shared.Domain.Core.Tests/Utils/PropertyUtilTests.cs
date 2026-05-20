using Shared.Domain.Core.Converters.Interfaces;
using Shared.Domain.Core.Utils;

namespace Shared.Domain.Core.Tests.Utils;

/// <summary>
/// Тесты для <see cref="PropertyUtil"/> — утилиты для получения и установки значений свойств через рефлексию.
/// </summary>
public sealed class PropertyUtilTests
{
    private sealed class TestPoco
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public DateTime? BirthDate { get; set; }
    }

    private sealed class FakeConverter : IObjectToStringConverter
    {
        public string Convert(object? valueToConvert) => $"CONVERTED:{valueToConvert}";
    }

    private readonly PropertyUtil _sut = new();

    /// <summary>
    /// Проверяет, что <see cref="PropertyUtil.GetProperty"/> возвращает значение существующего свойства.
    /// </summary>
    [Fact]
    public void GetProperty_ExistingProperty_ReturnsValue()
    {
        // Arrange
        var obj = new TestPoco { Name = "Alice" };

        // Act
        var result = _sut.GetProperty(obj, nameof(TestPoco.Name));

        // Assert
        result.Should().Be("Alice");
    }

    /// <summary>
    /// Проверяет, что <see cref="PropertyUtil.GetProperty"/> выбрасывает <see cref="InvalidOperationException"/> при отсутствующем свойстве и флаге <c>throwIfNotFound: true</c>.
    /// </summary>
    [Fact]
    public void GetProperty_MissingProperty_ThrowIfNotFoundTrue_ThrowsInvalidOperationException()
    {
        // Arrange
        var obj = new TestPoco();

        // Act & Assert
        Action act = () => _sut.GetProperty(obj, "NonExistent", throwIfNotFound: true);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Property NonExistent not found");
    }

    /// <summary>
    /// Проверяет, что <see cref="PropertyUtil.GetProperty"/> возвращает <c>null</c> при отсутствующем свойстве и флаге <c>throwIfNotFound: false</c>.
    /// </summary>
    [Fact]
    public void GetProperty_MissingProperty_ThrowIfNotFoundFalse_ReturnsNull()
    {
        // Arrange
        var obj = new TestPoco();

        // Act
        var result = _sut.GetProperty(obj, "NonExistent", throwIfNotFound: false);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Проверяет, что <see cref="PropertyUtil.GetProperty"/> выбрасывает <see cref="ArgumentNullException"/> при передаче <c>null</c> в качестве объекта.
    /// </summary>
    [Fact]
    public void GetProperty_NullObject_ThrowsArgumentNullException()
    {
        // Act & Assert
        Action act = () => _sut.GetProperty(null!, "Name");

        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Проверяет, что <see cref="PropertyUtil.SetProperty"/> устанавливает значение существующего свойства.
    /// </summary>
    [Fact]
    public void SetProperty_ExistingProperty_SetsValue()
    {
        // Arrange
        var obj = new TestPoco { Age = 10 };

        // Act
        _sut.SetProperty(obj, nameof(TestPoco.Age), 25);

        // Assert
        obj.Age.Should().Be(25);
    }

    /// <summary>
    /// Проверяет, что <see cref="PropertyUtil.SetProperty"/> выбрасывает <see cref="InvalidOperationException"/> при отсутствующем свойстве и флаге <c>throwIfNotFound: true</c>.
    /// </summary>
    [Fact]
    public void SetProperty_MissingProperty_ThrowIfNotFoundTrue_ThrowsInvalidOperationException()
    {
        // Arrange
        var obj = new TestPoco();

        // Act & Assert
        Action act = () => _sut.SetProperty(obj, "NonExistent", "value", throwIfNotFound: true);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Property NonExistent not found");
    }

    /// <summary>
    /// Проверяет, что <see cref="PropertyUtil.SetProperty"/> не изменяет объект при отсутствующем свойстве и флаге <c>throwIfNotFound: false</c>.
    /// </summary>
    [Fact]
    public void SetProperty_MissingProperty_ThrowIfNotFoundFalse_NoOp()
    {
        // Arrange
        var obj = new TestPoco { Name = "Original" };

        // Act
        _sut.SetProperty(obj, "NonExistent", "value", throwIfNotFound: false);

        // Assert
        obj.Name.Should().Be("Original");
    }

    /// <summary>
    /// Проверяет, что <see cref="PropertyUtil.SetProperty"/> выбрасывает <see cref="ArgumentNullException"/> при передаче <c>null</c> в качестве объекта.
    /// </summary>
    [Fact]
    public void SetProperty_NullObject_ThrowsArgumentNullException()
    {
        // Act & Assert
        Action act = () => _sut.SetProperty(null!, "Name", "value");

        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Проверяет, что <see cref="PropertyUtil.GetPropertyAsString"/> с кастомным конвертером возвращает преобразованное значение.
    /// </summary>
    [Fact]
    public void GetPropertyAsString_WithCustomConverter_ReturnsConvertedValue()
    {
        // Arrange
        var obj = new TestPoco { Name = "Alice" };
        var converter = new FakeConverter();

        // Act
        var result = _sut.GetPropertyAsString(obj, nameof(TestPoco.Name), converter);

        // Assert
        result.Should().Be("CONVERTED:Alice");
    }

    /// <summary>
    /// Проверяет, что <see cref="PropertyUtil.GetPropertyAsString"/> с конвертером по умолчанию возвращает строковое представление значения.
    /// </summary>
    [Fact]
    public void GetPropertyAsString_WithDefaultConverter_ReturnsStringRepresentation()
    {
        // Arrange
        var obj = new TestPoco { Name = "Alice" };

        // Act
        var result = _sut.GetPropertyAsString(obj, nameof(TestPoco.Name));

        // Assert
        result.Should().Be("Alice");
    }
}
