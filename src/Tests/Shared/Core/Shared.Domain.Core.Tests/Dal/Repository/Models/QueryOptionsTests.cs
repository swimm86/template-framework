using System.Linq.Expressions;
using Shared.Domain.Core.Dal;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Testing.Entities;

namespace Shared.Domain.Core.Tests.Dal.Repository.Models;

public sealed class QueryOptionsTests
{
    [Fact]
    public void AddFilter_AddsToFilters()
    {
        var options = new QueryOptions<TestEntity>();
        Expression<Func<TestEntity, bool>> filter = e => e.Name == "test";

        options.AddFilter(filter);

        options.Filters.Should().ContainSingle().Which.Should().BeSameAs(filter);
    }

    [Fact]
    public void AddFilterIf_ConditionTrue_AddsFilter()
    {
        var options = new QueryOptions<TestEntity>();
        Expression<Func<TestEntity, bool>> filter = e => e.Name == "test";

        options.AddFilterIf(true, filter);

        options.Filters.Should().ContainSingle().Which.Should().BeSameAs(filter);
    }

    [Fact]
    public void AddFilterIf_ConditionFalse_DoesNotAdd()
    {
        var options = new QueryOptions<TestEntity>();

        options.AddFilterIf(false, e => e.Name == "test");

        options.Filters.Should().BeEmpty();
    }

    [Fact]
    public void AddOrderBy_WithExpressionAndDirection_AddsToOrderBy()
    {
        var options = new QueryOptions<TestEntity>();
        Expression<Func<TestEntity, object>> expression = e => e.Name;

        options.AddOrderBy(expression, OrderDirectionType.Descending);

        options.OrderBy.Should().ContainSingle()
            .Which.Direction.Should().Be(OrderDirectionType.Descending);
    }

    [Fact]
    public void AddInclude_ReturnsIncludableForChaining()
    {
        var options = new QueryOptions<TestEntity>();

        var result = options.AddInclude(e => e.Name);

        result.Should().NotBeNull();
        options.Includes.Should().ContainSingle();
    }

    [Fact]
    public void Constructor_AcceptsWithTrackingAndSplitQueryAndDistinctFlags()
    {
        var options = new QueryOptions<TestEntity>(withTracking: true, asSplitQuery: true, distinct: true);

        options.WithTracking.Should().BeTrue();
        options.AsSplitQuery.Should().BeTrue();
        options.Distinct.Should().BeTrue();
    }

    [Fact]
    public void Constructor_DefaultFlagsAreFalse()
    {
        var options = new QueryOptions<TestEntity>();

        options.WithTracking.Should().BeFalse();
        options.AsSplitQuery.Should().BeFalse();
        options.Distinct.Should().BeFalse();
    }

    [Fact]
    public void WithTracking_IsMutableProperty()
    {
        var options = new QueryOptions<TestEntity>();

        options.WithTracking = true;

        options.WithTracking.Should().BeTrue();
    }

    [Fact]
    public void AsSplitQuery_IsMutableProperty()
    {
        var options = new QueryOptions<TestEntity>();

        options.AsSplitQuery = true;

        options.AsSplitQuery.Should().BeTrue();
    }

    [Fact]
    public void Distinct_IsMutableProperty()
    {
        var options = new QueryOptions<TestEntity>();

        options.Distinct = true;

        options.Distinct.Should().BeTrue();
    }
}
