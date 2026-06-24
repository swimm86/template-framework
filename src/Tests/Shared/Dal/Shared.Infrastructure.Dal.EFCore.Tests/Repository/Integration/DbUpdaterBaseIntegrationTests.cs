using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Repository.Integration;

/// <summary>
/// Интеграционные тесты для <see cref="DbUpdaterBase"/>, использующие SQLite.
/// Провайдер SQLite поддерживает реляционные методы (GetPendingMigrations, EnsureCreated),
/// недоступные в InMemory-провайдере.
/// </summary>
[Trait("Category", "Integration")]
public sealed class DbUpdaterBaseIntegrationTests : IDisposable
{
    private readonly DbConnection _connection;

    public DbUpdaterBaseIntegrationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    private DbContextOptions<IntegrationTestDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<IntegrationTestDbContext>()
            .UseSqlite(_connection)
            .Options;
    }

    private IntegrationTestDbContext CreateContext()
    {
        return new IntegrationTestDbContext(CreateOptions());
    }

    private IDbContextFactory<IntegrationTestDbContext> CreateContextFactory()
    {
        var options = CreateOptions();
        return new TestDbContextFactory<IntegrationTestDbContext>(options);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    #region CreateDbIfNotExists Tests

    /// <summary>
    /// <c>CreateDbIfNotExists</c> создаёт схему базы данных, когда нет pending-миграций
    /// (миграции не настроены, <c>GetPendingMigrations</c> возвращает пустой список).
    /// </summary>
    [Fact]
    public void CreateDbIfNotExists_NoPendingMigrations_CreatesSchema()
    {
        // Arrange
        using var context = CreateContext();
        var factory = CreateContextFactory();
        var strategy = new TestRelationalEnsureSchemaStrategy<IntegrationTestDbContext>(factory);
        var updater = new TestDbUpdater(context, strategy);

        // Act
        updater.CreateDbIfNotExists();

        // Assert — schema was created: can successfully insert and query
        context.Entities.Add(new TestEntityWithCreatedDeleted
        {
            Id = Guid.NewGuid(),
            Name = "db-exists-check",
        });
        var act = () => context.SaveChanges();
        act.Should().NotThrow();
    }

    /// <summary>
    /// <c>CreateDbIfNotExists</c> идемпотентен: повторный вызов не вызывает ошибок.
    /// </summary>
    [Fact]
    public void CreateDbIfNotExists_CalledTwice_DoesNotThrow()
    {
        // Arrange
        using var context = CreateContext();
        var factory = CreateContextFactory();
        var strategy = new TestRelationalEnsureSchemaStrategy<IntegrationTestDbContext>(factory);
        var updater = new TestDbUpdater(context, strategy);
        updater.CreateDbIfNotExists();

        // Act
        var act = () => updater.CreateDbIfNotExists();

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Migrate Tests

    /// <summary>
    /// <c>Migrate</c> не вызывает <c>Database.Migrate</c>, когда нет pending-миграций.
    /// </summary>
    [Fact]
    public void Migrate_NoPendingMigrations_DoesNotCallDatabaseMigrate()
    {
        // Arrange
        using var context = CreateContext();
        var updater = new TestDbUpdater(context, new StubEnsureSchemaStrategy());

        // Act
        updater.Migrate();

        // Assert
        updater.MigrateWasCalled.Should().BeTrue("TestDbUpdater.Migrate() records the call before delegating");
    }

    /// <summary>
    /// <c>Migrate</c> не бросает исключений при отсутствии pending-миграций.
    /// </summary>
    [Fact]
    public void Migrate_NoPendingMigrations_DoesNotThrow()
    {
        // Arrange
        using var context = CreateContext();
        var updater = new TestDbUpdater(context, new StubEnsureSchemaStrategy());

        // Act
        var act = () => updater.Migrate();

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Initialize Tests

    /// <summary>
    /// <c>Initialize</c> не бросает исключений (виртуальный метод с пустой реализацией).
    /// </summary>
    [Fact]
    public void Initialize_WithSqliteContext_DoesNotThrow()
    {
        // Arrange
        using var context = CreateContext();
        var updater = new TestDbUpdater(context, new StubEnsureSchemaStrategy());

        // Act
        var act = () => updater.Initialize();

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Dispose Tests

    /// <summary>
    /// <c>Dispose</c> освобождает <see cref="DbContext"/> (операции после <c>Dispose</c> бросают исключение).
    /// </summary>
    [Fact]
    public void Dispose_WithSqliteContext_DisposesContext()
    {
        // Arrange
        var context = CreateContext();
        var updater = new TestDbUpdater(context, new StubEnsureSchemaStrategy());

        // Act
        updater.Dispose();

        // Assert
        var act = () => context.Model;
        act.Should().Throw<ObjectDisposedException>();
    }

    /// <summary>
    /// <c>DisposeAsync</c> освобождает <see cref="DbContext"/> асинхронно.
    /// </summary>
    [Fact]
    public async Task DisposeAsync_WithSqliteContext_DisposesContext()
    {
        // Arrange
        var context = CreateContext();
        var updater = new TestDbUpdater(context, new StubEnsureSchemaStrategy());

        // Act
        await updater.DisposeAsync();

        // Assert
        var act = () => context.Model;
        act.Should().Throw<ObjectDisposedException>();
    }

    #endregion
}
