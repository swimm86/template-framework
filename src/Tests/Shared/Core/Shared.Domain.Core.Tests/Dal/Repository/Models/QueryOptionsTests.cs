using System.Linq.Expressions;
using Shared.Domain.Core.Dal;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Testing.Entities;

namespace Shared.Domain.Core.Tests.Dal.Repository.Models;

/// <summary>
/// Тесты для <see cref="QueryOptions{T}"/> — объекта параметров запроса, содержащего фильтры, сортировки, Include-выражения и флаги.
/// </summary>
public sealed class QueryOptionsTests
{
    /// <summary>
    /// Проверяет, что <see cref="QueryOptions{T}.AddFilter"/> добавляет переданное выражение фильтра в коллекцию Filters.
    /// </summary>
    [Fact]
    public void AddFilter_AddsToFilters()
    {
        // Arrange
        var options = new QueryOptions<TestEntity>();
        Expression<Func<TestEntity, bool>> filter = e => e.Name == "test";

        // Act
        options.AddFilter(filter);

        // Assert
        options.Filters.Should().ContainSingle().Which.Should().BeSameAs(filter);
    }

    /// <summary>
    /// Проверяет, что <see cref="QueryOptions{T}.AddFilterIf"/> с условием <c>true</c> добавляет фильтр.
    /// </summary>
    [Fact]
    public void AddFilterIf_ConditionTrue_AddsFilter()
    {
        // Arrange
        var options = new QueryOptions<TestEntity>();
        Expression<Func<TestEntity, bool>> filter = e => e.Name == "test";

        // Act
        options.AddFilterIf(true, filter);

        // Assert
        options.Filters.Should().ContainSingle().Which.Should().BeSameAs(filter);
    }

    /// <summary>
    /// Проверяет, что <see cref="QueryOptions{T}.AddFilterIf"/> с условием <c>false</c> не добавляет фильтр.
    /// </summary>
    [Fact]
    public void AddFilterIf_ConditionFalse_DoesNotAdd()
    {
        // Arrange
        var options = new QueryOptions<TestEntity>();

        // Act
        options.AddFilterIf(false, e => e.Name == "test");

        // Assert
        options.Filters.Should().BeEmpty();
    }

    /// <summary>
    /// Проверяет, что <see cref="QueryOptions{T}.AddOrderBy(Expression{Func{T, object}}, OrderDirectionType, int?)"/> добавляет сортировку с указанным выражением и направлением.
    /// </summary>
    [Fact]
    public void AddOrderBy_WithExpressionAndDirection_AddsToOrderBy()
    {
        // Arrange
        var options = new QueryOptions<TestEntity>();
        Expression<Func<TestEntity, object>> expression = e => e.Name;

        // Act
        options.AddOrderBy(expression, OrderDirectionType.Descending);

        // Assert
        options.OrderBy.Should().ContainSingle()
            .Which.Direction.Should().Be(OrderDirectionType.Descending);
    }

    /// <summary>
    /// Проверяет, что <see cref="QueryOptions{T}.AddInclude{TProperty}(Expression{Func{T, TProperty}})"/> возвращает объект для цепочки включений (ThenInclude) и добавляет Include.
    /// </summary>
    [Fact]
    public void AddInclude_ReturnsIncludableForChaining()
    {
        // Arrange
        var options = new QueryOptions<TestEntity>();

        // Act
        var result = options.AddInclude(e => e.Name);

        // Assert
        result.Should().NotBeNull();
        options.Includes.Should().ContainSingle();
    }

    /// <summary>
    /// Проверяет, что конструктор <see cref="QueryOptions{T}"/> принимает и сохраняет флаги <c>withTracking</c>, <c>asSplitQuery</c> и <c>distinct</c>.
    /// </summary>
    [Fact]
    public void Constructor_AcceptsWithTrackingAndSplitQueryAndDistinctFlags()
    {
        // Arrange

        // Act
        var options = new QueryOptions<TestEntity>(withTracking: true, asSplitQuery: true, distinct: true);

        // Assert
        options.WithTracking.Should().BeTrue();
        options.AsSplitQuery.Should().BeTrue();
        options.Distinct.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что значения по умолчанию для флагов <c>WithTracking</c>, <c>AsSplitQuery</c> и <c>Distinct</c> равны <c>false</c>.
    /// </summary>
    [Fact]
    public void Constructor_DefaultFlagsAreFalse()
    {
        // Arrange

        // Act
        var options = new QueryOptions<TestEntity>();

        // Assert
        options.WithTracking.Should().BeFalse();
        options.AsSplitQuery.Should().BeFalse();
        options.Distinct.Should().BeFalse();
    }

    /// <summary>
    /// Проверяет, что свойство <see cref="QueryOptions{T}.WithTracking"/> доступно для записи.
    /// </summary>
    [Fact]
    public void WithTracking_IsMutableProperty()
    {
        // Arrange
        var options = new QueryOptions<TestEntity>();

        // Act
        options.WithTracking = true;

        // Assert
        options.WithTracking.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что свойство <see cref="QueryOptions{T}.AsSplitQuery"/> доступно для записи.
    /// </summary>
    [Fact]
    public void AsSplitQuery_IsMutableProperty()
    {
        // Arrange
        var options = new QueryOptions<TestEntity>();

        // Act
        options.AsSplitQuery = true;

        // Assert
        options.AsSplitQuery.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что свойство <see cref="QueryOptions{T}.Distinct"/> доступно для записи.
    /// </summary>
    [Fact]
    public void Distinct_IsMutableProperty()
    {
        // Arrange
        var options = new QueryOptions<TestEntity>();

        // Act
        options.Distinct = true;

        // Assert
        options.Distinct.Should().BeTrue();
    }
}
