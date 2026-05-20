using Shared.Application.Cqrs.Core.Abstractions.Queries.Requests;
using Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Application.Cqrs.Core.Tests.Abstractions.Queries.Requests;

/// <summary>
/// Тесты для <see cref="ReadListQuery{TRequest,TFilter,TResponse}"/>: пагинация, фильтр, PageNumber.
/// </summary>
public sealed class ReadListQueryTests
{
    /// <summary>
    /// Номер страницы меньше 1 схлопывается в 1.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Constructor_PageNumberAtOrBelowMin_ClampedTo1(int inputPageNumber)
    {
        // Arrange
        var request = new TestPageableRequest { PageNumber = inputPageNumber };

        // Act
        var query = new TestReadListQuery(request);

        // Assert
        query.PageNumber.Should().Be(1);
    }

    /// <summary>
    /// <see langword="null"/>-фильтр заменяется на экземпляр по умолчанию.
    /// </summary>
    [Fact]
    public void Constructor_FilterNull_DefaultsToNewTFilter()
    {
        // Arrange
        var request = new TestPageableRequest { Filter = null };

        // Act
        var query = new TestReadListQuery(request);

        // Assert
        query.Filter.Should().NotBeNull();
        query.Filter.Should().BeOfType<TestListFilter>();
    }

    /// <summary>
    /// Конструктор сохраняет переданный запрос и размер страницы.
    /// </summary>
    [Fact]
    public void Constructor_AssignsRequestAndPageSize()
    {
        // Arrange
        var request = new TestPageableRequest { PageSize = 50 };

        // Act
        var query = new TestReadListQuery(request);

        // Assert
        query.Request.Should().Be(request);
        query.PageSize.Should().Be(50);
    }

    /// <summary>
    /// Свойство Filter при null коалесцируется в новый экземпляр.
    /// </summary>
    [Fact]
    public void Filter_WhenNull_CoalescesToNew()
    {
        // Arrange
        var request = new TestPageableRequest { Filter = null };

        // Act
        var query = new TestReadListQuery(request);

        // Assert
        query.Filter.Should().BeOfType<TestListFilter>();
    }

    /// <summary>
    /// Номер страницы сохраняется при валидном значении.
    /// </summary>
    [Fact]
    public void PageNumber_WhenValid_Preserved()
    {
        // Arrange
        var request = new TestPageableRequest { PageNumber = 5 };

        // Act
        var query = new TestReadListQuery(request);

        // Assert
        query.PageNumber.Should().Be(5);
    }
}
