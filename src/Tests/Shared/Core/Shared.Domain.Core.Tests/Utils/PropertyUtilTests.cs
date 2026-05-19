using Shared.Domain.Core.Converters.Interfaces;
using Shared.Domain.Core.Utils;
using Shared.Domain.Core.Utils.Interfaces;

namespace Shared.Domain.Core.Tests.Utils;

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

    [Fact]
    public void GetProperty_ExistingProperty_ReturnsValue()
    {
        var obj = new TestPoco { Name = "Alice" };

        var result = _sut.GetProperty(obj, nameof(TestPoco.Name));

        result.Should().Be("Alice");
    }

    [Fact]
    public void GetProperty_MissingProperty_ThrowIfNotFoundTrue_ThrowsInvalidOperationException()
    {
        var obj = new TestPoco();

        Action act = () => _sut.GetProperty(obj, "NonExistent", throwIfNotFound: true);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Property NonExistent not found");
    }

    [Fact]
    public void GetProperty_MissingProperty_ThrowIfNotFoundFalse_ReturnsNull()
    {
        var obj = new TestPoco();

        var result = _sut.GetProperty(obj, "NonExistent", throwIfNotFound: false);

        result.Should().BeNull();
    }

    [Fact]
    public void GetProperty_NullObject_ThrowsArgumentNullException()
    {
        Action act = () => _sut.GetProperty(null!, "Name");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SetProperty_ExistingProperty_SetsValue()
    {
        var obj = new TestPoco { Age = 10 };

        _sut.SetProperty(obj, nameof(TestPoco.Age), 25);

        obj.Age.Should().Be(25);
    }

    [Fact]
    public void SetProperty_MissingProperty_ThrowIfNotFoundTrue_ThrowsInvalidOperationException()
    {
        var obj = new TestPoco();

        Action act = () => _sut.SetProperty(obj, "NonExistent", "value", throwIfNotFound: true);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Property NonExistent not found");
    }

    [Fact]
    public void SetProperty_MissingProperty_ThrowIfNotFoundFalse_NoOp()
    {
        var obj = new TestPoco { Name = "Original" };

        _sut.SetProperty(obj, "NonExistent", "value", throwIfNotFound: false);

        obj.Name.Should().Be("Original");
    }

    [Fact]
    public void SetProperty_NullObject_ThrowsArgumentNullException()
    {
        Action act = () => _sut.SetProperty(null!, "Name", "value");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetPropertyAsString_WithCustomConverter_ReturnsConvertedValue()
    {
        var obj = new TestPoco { Name = "Alice" };
        var converter = new FakeConverter();

        var result = _sut.GetPropertyAsString(obj, nameof(TestPoco.Name), converter);

        result.Should().Be("CONVERTED:Alice");
    }

    [Fact]
    public void GetPropertyAsString_WithDefaultConverter_ReturnsStringRepresentation()
    {
        var obj = new TestPoco { Name = "Alice" };

        var result = _sut.GetPropertyAsString(obj, nameof(TestPoco.Name));

        result.Should().Be("Alice");
    }
}
