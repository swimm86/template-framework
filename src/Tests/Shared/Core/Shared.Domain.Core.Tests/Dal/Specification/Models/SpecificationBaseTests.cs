using System.Linq.Expressions;
using Shared.Domain.Core.Dal;
using Shared.Domain.Core.Dal.Models;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Dal.Specification.Models;
using Shared.Domain.Core.Tests.Infrastructure.TestDoubles;
using Shared.Testing.Entities;

namespace Shared.Domain.Core.Tests.Dal.Specification.Models;

/// <summary>
/// Тесты для <see cref="SpecificationBase{TEntity}"/> — базового класса спецификаций, агрегирующего фильтры, сортировки и Include-выражения.
/// </summary>
public sealed class SpecificationBaseTests
{
    /// <summary>
    /// Проверяет, что <see cref="SpecificationBase{TEntity}.BuildOptions"/> применяет переданные настройки сортировки к результирующему <see cref="QueryOptions{TEntity}"/>.
    /// </summary>
    [Fact]
    public void BuildOptions_AppliesSortOptions()
    {
        // Arrange
        var sortOptions = new List<SortOption>
        {
            new("name", OrderDirectionType.Descending)
        };
        var specification = new TestSpecification(sortOptions);

        // Act
        var queryOptions = specification.BuildOptions();

        // Assert
        queryOptions.OrderBy.Should().ContainSingle()
            .Which.Direction.Should().Be(OrderDirectionType.Descending);
    }

    /// <summary>
    /// Проверяет, что <see cref="SpecificationBase{TEntity}.BuildOptions"/> с пустыми настройками сортировки возвращает пустой OrderBy.
    /// </summary>
    [Fact]
    public void BuildOptions_EmptySortOptions_EmptyOrderBy()
    {
        // Arrange
        var specification = new TestSpecification();

        // Act
        var queryOptions = specification.BuildOptions();

        // Assert
        queryOptions.OrderBy.Should().BeEmpty();
    }

    /// <summary>
    /// Проверяет, что <see cref="SpecificationBase{TEntity}.AddInclude{TProperty}(Expression{Func{TEntity, TProperty}})"/> делегирует вызов в Options, добавляя Include-выражение.
    /// </summary>
    [Fact]
    public void AddInclude_DelegatesToOptions()
    {
        // Arrange
        var specification = new TestSpecification();
        Expression<Func<TestEntity, string>> expression = e => e.Name;

        // Act
        specification.AddInclude(expression);

        // Assert
        specification.Options.Includes.Should().ContainSingle();
    }

    /// <summary>
    /// Проверяет, что <see cref="SpecificationBase{TEntity}.AddFilter"/> делегирует вызов в Options, добавляя фильтр.
    /// </summary>
    [Fact]
    public void AddFilter_DelegatesToOptions()
    {
        // Arrange
        var specification = new TestSpecification();
        Expression<Func<TestEntity, bool>> filter = e => e.Name == "test";

        // Act
        specification.AddFilter(filter);

        // Assert
        specification.Options.Filters.Should().ContainSingle().Which.Should().BeSameAs(filter);
    }
}
