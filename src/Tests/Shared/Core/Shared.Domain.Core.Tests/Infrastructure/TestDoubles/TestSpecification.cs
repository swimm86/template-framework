using System.Linq.Expressions;
using Shared.Domain.Core.Dal;
using Shared.Domain.Core.Dal.Models;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Dal.Specification.Models;
using Shared.Testing.Entities;

namespace Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

internal sealed record TestSpecification : SpecificationBase<TestEntity>
{
    public TestSpecification(ICollection<SortOption>? sortOptions = default)
        : base(sortOptions)
    {
    }

    public new QueryOptions<TestEntity> Options => base.Options;

    public new Includable<TestEntity, TProperty> AddInclude<TProperty>(
        Expression<Func<TestEntity, TProperty>> expression) =>
        base.AddInclude(expression);

    public new void AddFilter(Expression<Func<TestEntity, bool>> expression) =>
        base.AddFilter(expression);

    public new void AddOrderBy(
        Expression<Func<TestEntity, object>> expression,
        OrderDirectionType orderDirectionType) =>
        base.AddOrderBy(expression, orderDirectionType);
}
