using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Dal.EFCore.Conventions;
using Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Conventions;

/// <summary>
/// Тесты для конвенции именования колонок <see cref="ColumnsNamesConvention"/>.
/// Проверяет преобразование PascalCase в snake_case.
/// </summary>
public class ColumnsNamesConventionTests
{
    /// <summary>
    /// Проверяет что свойство в PascalCase преобразуется в snake_case.
    /// </summary>
    [Fact]
    public void Apply_PascalCaseProperty_ConvertsToSnakeCase()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestEfRepositoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestEfRepositoryDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(TestEfEntity));
        entityType.Should().NotBeNull();

        // Act
        var property = entityType.FindProperty(nameof(TestEfEntity.DateCreated));
        property.Should().NotBeNull();
        var columnName = property.GetColumnName();

        // Assert
        columnName.Should().Be("date_created");
    }

    /// <summary>
    /// Проверяет что простое свойство сохраняет нижний регистр.
    /// </summary>
    [Fact]
    public void Apply_SimpleProperty_PreservesLowerCase()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestEfRepositoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestEfRepositoryDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(TestEfEntity));
        entityType.Should().NotBeNull();

        // Act
        var property = entityType.FindProperty(nameof(TestEfEntity.Name));
        property.Should().NotBeNull();
        var columnName = property.GetColumnName();

        // Assert
        columnName.Should().Be("name");
    }

    /// <summary>
    /// Проверяет что все свойства сущности имеют имена колонок в snake_case.
    /// </summary>
    [Fact]
    public void Apply_AllProperties_AreSnakeCase()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestEfRepositoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestEfRepositoryDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(TestEfEntity));
        entityType.Should().NotBeNull();

        // Act
        var columnNames = entityType.GetProperties()
            .Select(p => (p.Name, ColumnName: p.GetColumnName()))
            .ToList();

        // Assert
        columnNames.Should().BeEquivalentTo(new[]
        {
            (Name: nameof(TestEfEntity.Id), ColumnName: "id"),
            (Name: nameof(TestEfEntity.Name), ColumnName: "name"),
            (Name: nameof(TestEfEntity.DateCreated), ColumnName: "date_created"),
        });
    }
}
