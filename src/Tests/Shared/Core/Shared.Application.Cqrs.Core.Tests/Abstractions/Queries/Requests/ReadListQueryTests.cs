using Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Application.Cqrs.Core.Tests.Abstractions.Queries.Requests;

public sealed class ReadListQueryTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Constructor_PageNumberAtOrBelowMin_ClampedTo1(int inputPageNumber)
    {
        var request = new TestPageableRequest { PageNumber = inputPageNumber };
        var query = new TestReadListQuery(request);

        query.PageNumber.Should().Be(1);
    }

    [Fact]
    public void Constructor_FilterNull_DefaultsToNewTFilter()
    {
        var request = new TestPageableRequest { Filter = null };
        var query = new TestReadListQuery(request);

        query.Filter.Should().NotBeNull();
        query.Filter.Should().BeOfType<TestListFilter>();
    }

    [Fact]
    public void Constructor_AssignsRequestAndPageSize()
    {
        var request = new TestPageableRequest { PageSize = 50 };
        var query = new TestReadListQuery(request);

        query.Request.Should().Be(request);
        query.PageSize.Should().Be(50);
    }

    [Fact]
    public void Filter_WhenNull_CoalescesToNew()
    {
        var request = new TestPageableRequest { Filter = null };
        var query = new TestReadListQuery(request);

        query.Filter.Should().BeOfType<TestListFilter>();
    }

    [Fact]
    public void PageNumber_WhenValid_Preserved()
    {
        var request = new TestPageableRequest { PageNumber = 5 };
        var query = new TestReadListQuery(request);

        query.PageNumber.Should().Be(5);
    }
}
