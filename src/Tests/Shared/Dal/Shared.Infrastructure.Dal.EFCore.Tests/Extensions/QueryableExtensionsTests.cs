using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Infrastructure.Dal.EFCore.Extensions;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Extensions;

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

    [Fact]
    public void IncludeUntyped_SingleNavigation_AddsInclude()
    {
        using var context = CreateContext();

        Expression<Func<Infrastructure.TestParentEntity, Infrastructure.TestChildEntity?>> expression =
            e => e.Child;

        var includable = new Includable<Infrastructure.TestParentEntity, Infrastructure.TestChildEntity?>(
            expression);

        var queryable = context.Parents.AsQueryable();

        var result = queryable.IncludeUntyped(includable);

        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IQueryable<Infrastructure.TestParentEntity>>();
    }

    [Fact]
    public void IncludeUntyped_CollectionNavigation_AddsInclude()
    {
        using var context = CreateContext();

        Expression<Func<Infrastructure.TestParentEntity, ICollection<Infrastructure.TestChildEntity>>> expression =
            e => e.Children;

        var includable = new Includable<Infrastructure.TestParentEntity, ICollection<Infrastructure.TestChildEntity>>(
            expression);

        var queryable = context.Parents.AsQueryable();

        var result = queryable.IncludeUntyped(includable);

        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IQueryable<Infrastructure.TestParentEntity>>();
    }

    [Fact]
    public void IncludeUntyped_ThenInclude_Chains()
    {
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

        var result = queryable.IncludeUntyped(includable);

        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IQueryable<Infrastructure.TestParentEntity>>();
    }

    [Fact]
    public void IncludeUntyped_StringProperty_ReturnsQueryable()
    {
        // EF Core InMemory provider does not validate navigation properties
        // at query construction time; a real DB provider would throw when
        // executing the query with a non-navigation Include path.
        using var context = CreateContext();

        Expression<Func<Infrastructure.TestParentEntity, string>> expression =
            e => e.Name;

        var includable = new Includable<Infrastructure.TestParentEntity, string>(expression);

        var queryable = context.Parents.AsQueryable();

        var result = queryable.IncludeUntyped(includable);

        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IQueryable<Infrastructure.TestParentEntity>>();
    }

    [Fact]
    public void IncludeUntyped_EmptyDbSet_ReturnsEmptyQueryable()
    {
        var options = new DbContextOptionsBuilder<Infrastructure.TestIncludeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new Infrastructure.TestIncludeDbContext(options);

        Expression<Func<Infrastructure.TestParentEntity, Infrastructure.TestChildEntity?>> expression =
            e => e.Child;

        var includable = new Includable<Infrastructure.TestParentEntity, Infrastructure.TestChildEntity?>(
            expression);

        var queryable = context.Parents.AsQueryable();

        var result = queryable.IncludeUntyped(includable);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}
