using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Infrastructure.Dal.EFCore.Extensions;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Extensions;

/// <summary>
/// Тесты для методов расширения <see cref="QueryableExtensions"/>.
/// Проверяет динамическое включение навигационных свойств через IncludeUntyped
/// — как структурную корректность (тип IQueryable), так и фактическую загрузку данных.
/// </summary>
public class QueryableExtensionsTests
{
    private static Infrastructure.TestIncludeDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<Infrastructure.TestIncludeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new Infrastructure.TestIncludeDbContext(options);

        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var grandChildId = Guid.NewGuid();

        context.GrandChildren.Add(new Infrastructure.TestGrandChildEntity
        {
            Id = grandChildId,
            Name = "GrandChild",
        });

        context.Children.Add(new Infrastructure.TestChildEntity
        {
            Id = childId,
            Name = "Child",
            ParentId = parentId,
            GrandChildId = grandChildId,
        });

        context.Parents.Add(new Infrastructure.TestParentEntity
        {
            Id = parentId,
            Name = "Parent",
            ChildId = childId,
        });

        context.SaveChanges();

        return context;
    }

    /// <summary>
    /// Проверяет что IncludeUntyped для одиночной навигации возвращает IQueryable
    /// и фактически загружает связанную сущность.
    /// </summary>
    [Fact]
    public void IncludeUntyped_SingleNavigation_LoadsRelatedEntity()
    {
        // Arrange
        using var context = CreateContext();

        Expression<Func<Infrastructure.TestParentEntity, Infrastructure.TestChildEntity?>> expression =
            e => e.Child;

        var includable = new Includable<Infrastructure.TestParentEntity, Infrastructure.TestChildEntity?>(
            expression);

        var queryable = context.Parents.AsQueryable();

        // Act
        var result = queryable.IncludeUntyped(includable).ToList();

        // Assert — type is correct and child is actually loaded
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IEnumerable<Infrastructure.TestParentEntity>>();
        result.Should().HaveCount(1);
        result[0].Child.Should().NotBeNull("single navigation was included");
        result[0].Child!.Name.Should().Be("Child");
    }

    /// <summary>
    /// Проверяет что IncludeUntyped для коллекционной навигации загружает коллекцию.
    /// </summary>
    [Fact]
    public void IncludeUntyped_CollectionNavigation_LoadsCollection()
    {
        // Arrange
        using var context = CreateContext();

        Expression<Func<Infrastructure.TestParentEntity, ICollection<Infrastructure.TestChildEntity>>> expression =
            e => e.Children;

        var includable = new Includable<Infrastructure.TestParentEntity, ICollection<Infrastructure.TestChildEntity>>(
            expression);

        var queryable = context.Parents.AsQueryable();

        // Act
        var result = queryable.IncludeUntyped(includable).ToList();

        // Assert — collection is included and contains the related child
        result.Should().HaveCount(1);
        result[0].Children.Should().NotBeNull();
        result[0].Children.Should().HaveCount(1, "one child was seeded");
        result[0].Children.First().Name.Should().Be("Child");
    }

    /// <summary>
    /// Проверяет что IncludeUntyped поддерживает цепочку ThenInclude и фактически загружает
    /// вложенные навигационные свойства (GrandChild через Child).
    /// </summary>
    [Fact]
    public void IncludeUntyped_ThenInclude_LoadsNestedNavigation()
    {
        // Arrange
        using var context = CreateContext();

        Expression<Func<Infrastructure.TestParentEntity, Infrastructure.TestChildEntity?>> expression =
            e => e.Child;

        var includable = new Includable<Infrastructure.TestParentEntity, Infrastructure.TestChildEntity?>(
            expression);

        LambdaExpression thenExpression = (Expression<Func<Infrastructure.TestChildEntity, Infrastructure.TestGrandChildEntity?>>)(
            e => e.GrandChild);

        var thenIncludable = new Includable<Infrastructure.TestParentEntity, Infrastructure.TestGrandChildEntity?>(
            thenExpression);
        includable.SetChild(thenIncludable);

        var queryable = context.Parents.AsQueryable();

        // Act
        var result = queryable.IncludeUntyped(includable).ToList();

        // Assert — both Child and GrandChild are loaded
        result.Should().HaveCount(1);
        result[0].Child.Should().NotBeNull("Child was included");
        result[0].Child!.GrandChild.Should().NotBeNull("GrandChild was ThenIncluded");
        result[0].Child.GrandChild!.Name.Should().Be("GrandChild");
    }

    /// <summary>
    /// Проверяет что IncludeUntyped со строковым свойством (не навигационным)
    /// не вызывает исключений при построении запроса.
    /// InMemory не валидирует навигационные свойства при построении — ошибка возникла бы только при выполнении на реальной СУБД.
    /// </summary>
    [Fact]
    public void IncludeUntyped_StringProperty_DoesNotThrowOnQueryConstruction()
    {
        // Arrange
        using var context = CreateContext();

        Expression<Func<Infrastructure.TestParentEntity, string>> expression =
            e => e.Name;

        var includable = new Includable<Infrastructure.TestParentEntity, string>(expression);

        var queryable = context.Parents.AsQueryable();

        // Act — only verify that query construction does not throw
        // (execution on a real DB would throw because Name is not a navigation property)
        var act = () => queryable.IncludeUntyped(includable);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Проверяет что IncludeUntyped на пустом DbSet возвращает пустую коллекцию.
    /// </summary>
    [Fact]
    public void IncludeUntyped_EmptyDbSet_ReturnsEmptyCollection()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<Infrastructure.TestIncludeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new Infrastructure.TestIncludeDbContext(options);

        Expression<Func<Infrastructure.TestParentEntity, Infrastructure.TestChildEntity?>> expression =
            e => e.Child;

        var includable = new Includable<Infrastructure.TestParentEntity, Infrastructure.TestChildEntity?>(
            expression);

        var queryable = context.Parents.AsQueryable();

        // Act
        var result = queryable.IncludeUntyped(includable).ToList();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}
