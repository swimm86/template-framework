using Shared.Domain.Core.Converters;
using Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Domain.Core.Tests.Converters;

public sealed class DefaultObjectToStringConverterTests
{
    private readonly DefaultObjectToStringConverter _sut = new();

    [Fact]
    public void Convert_Null_ReturnsEmpty()
    {
        var result = _sut.Convert(null);

        result.Should().Be(string.Empty);
    }

    [Fact]
    public void Convert_StringValue_ReturnsSame()
    {
        var result = _sut.Convert("Hello World");

        result.Should().Be("Hello World");
    }

    [Fact]
    public void Convert_BoolTrue_ReturnsLocalizedYes()
    {
        var result = _sut.Convert(true);

        result.Should().Be("Да");
    }

    [Fact]
    public void Convert_BoolFalse_ReturnsLocalizedNo()
    {
        var result = _sut.Convert(false);

        result.Should().Be("Нет");
    }

    [Fact]
    public void Convert_Guid_ReturnsNFormat()
    {
        var guid = Guid.NewGuid();

        var result = _sut.Convert(guid);

        result.Should().HaveLength(32);
        result.Should().NotContain("-");
        result.Should().Be(guid.ToString("N"));
    }

    [Fact]
    public void Convert_EnumWithDescription_ReturnsDescriptionText()
    {
        var result = _sut.Convert(TestEnumWithDescription.FirstValue);

        result.Should().Be("Первое значение");
    }

    [Fact]
    public void Convert_DateTime_ReturnsIsoDateFormat()
    {
        var dt = new DateTime(2025, 5, 18);

        var result = _sut.Convert(dt);

        result.Should().Be("2025-05-18");
    }

    [Fact]
    public void Convert_DateOnly_ReturnsIsoDateFormat()
    {
        var d = new DateOnly(2025, 5, 18);

        var result = _sut.Convert(d);

        result.Should().Be("2025-05-18");
    }
}
