using System.Linq.Expressions;
using Shared.Domain.Core.Dal;
using Shared.Domain.Core.Dal.Models;
using Shared.Domain.Core.Tests.Infrastructure.TestDoubles;
using Shared.Testing.Entities;

namespace Shared.Domain.Core.Tests.Dal.Specification.Models;

public sealed class SpecificationBaseTests
{
    [Fact]
    public void BuildOptions_AppliesSortOptions()
    {
        var sortOptions = new List<SortOption>
        {
            new("name", OrderDirectionType.Descending)
        };
        var specification = new TestSpecification(sortOptions);

        var queryOptions = specification.BuildOptions();

        queryOptions.OrderBy.Should().ContainSingle()
            .Which.Direction.Should().Be(OrderDirectionType.Descending);
    }

    [Fact]
    public void BuildOptions_EmptySortOptions_EmptyOrderBy()
    {
        var specification = new TestSpecification();

        var queryOptions = specification.BuildOptions();

        queryOptions.OrderBy.Should().BeEmpty();
    }

    [Fact]
    public void AddInclude_DelegatesToOptions()
    {
        var specification = new TestSpecification();
        Expression<Func<TestEntity, string>> expression = e => e.Name;

        specification.AddInclude(expression);

        specification.Options.Includes.Should().ContainSingle();
    }

    [Fact]
    public void AddFilter_DelegatesToOptions()
    {
        var specification = new TestSpecification();
        Expression<Func<TestEntity, bool>> filter = e => e.Name == "test";

        specification.AddFilter(filter);

        specification.Options.Filters.Should().ContainSingle().Which.Should().BeSameAs(filter);
    }
}
