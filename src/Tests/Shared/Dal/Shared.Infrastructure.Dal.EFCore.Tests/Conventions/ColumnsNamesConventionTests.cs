using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Dal.EFCore.Conventions;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Conventions;

public class ColumnsNamesConventionTests
{
    [Fact]
    public void Apply_PascalCaseProperty_ConvertsToSnakeCase()
    {
        var options = new DbContextOptionsBuilder<Infrastructure.TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new Infrastructure.TestDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(Infrastructure.TestEfEntity))!;

        var columnName = entityType.FindProperty(nameof(Infrastructure.TestEfEntity.DateCreated))!
            .GetColumnName();

        columnName.Should().Be("date_created");
    }

    [Fact]
    public void Apply_SimpleProperty_PreservesLowerCase()
    {
        var options = new DbContextOptionsBuilder<Infrastructure.TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new Infrastructure.TestDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(Infrastructure.TestEfEntity))!;

        var columnName = entityType.FindProperty(nameof(Infrastructure.TestEfEntity.Name))!
            .GetColumnName();

        columnName.Should().Be("name");
    }

    [Fact]
    public void Apply_AllProperties_AreSnakeCase()
    {
        var options = new DbContextOptionsBuilder<Infrastructure.TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new Infrastructure.TestDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(Infrastructure.TestEfEntity))!;

        var columnNames = entityType.GetProperties()
            .Select(p => (p.Name, ColumnName: p.GetColumnName()))
            .ToList();

        columnNames.Should().BeEquivalentTo(new[]
        {
            (Name: nameof(Infrastructure.TestEfEntity.Id), ColumnName: "id"),
            (Name: nameof(Infrastructure.TestEfEntity.Name), ColumnName: "name"),
            (Name: nameof(Infrastructure.TestEfEntity.DateCreated), ColumnName: "date_created"),
        });
    }
}
