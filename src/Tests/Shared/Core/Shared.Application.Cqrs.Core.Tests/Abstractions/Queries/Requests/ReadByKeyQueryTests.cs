using Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Application.Cqrs.Core.Tests.Abstractions.Queries.Requests;

public sealed class ReadByKeyQueryTests
{
    [Fact]
    public void Constructor_AssignsKey()
    {
        var query = new TestReadByKeyQuery("key-123");

        query.Key.Should().Be("key-123");
    }

    [Fact]
    public void Constructor_NullKey_DoesNotThrow()
    {
        var query = new TestReadByKeyQuery(null!);

        query.Key.Should().BeNull();
    }

    [Fact]
    public void Key_IsGettableProperty()
    {
        var property = typeof(TestReadByKeyQuery).GetProperty("Key");

        property.Should().NotBeNull();
        property!.CanRead.Should().BeTrue();
        property.CanWrite.Should().BeFalse();
    }
}
