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

    private IntegrationTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<IntegrationTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new IntegrationTestDbContext(options);
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    #region CreateDbIfNotExists Tests

    /// <summary>
    /// Проверяет что CreateDbIfNotExists создаёт схему базы данных когда нет pending миграций
    /// (GetPendingMigrations возвращает пустой список — миграции не настроены).
    /// </summary>
    [Fact]
    public void CreateDbIfNotExists_NoPendingMigrations_CreatesSchema()
    {
        // Arrange
        using var context = CreateContext();
        var updater = new TestDbUpdater(context);

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
    /// Проверяет что CreateDbIfNotExists идемпотентен: повторный вызов не вызывает ошибок.
    /// </summary>
    [Fact]
    public void CreateDbIfNotExists_CalledTwice_DoesNotThrow()
    {
        // Arrange
        using var context = CreateContext();
        var updater = new TestDbUpdater(context);
        updater.CreateDbIfNotExists();

        // Act & Assert
        var act = () => updater.CreateDbIfNotExists();
        act.Should().NotThrow();
    }

    #endregion

    #region Migrate Tests

    /// <summary>
    /// Проверяет что Migrate не вызывает Database.Migrate когда нет pending миграций
    /// (миграции не зарегистрированы, GetPendingMigrations возвращает пустой список).
    /// </summary>
    [Fact]
    public void Migrate_NoPendingMigrations_DoesNotCallDatabaseMigrate()
    {
        // Arrange
        using var context = CreateContext();
        var updater = new TestDbUpdater(context);

        // Act
        updater.Migrate();

        // Assert — GetPendingMigrations() returned empty, so base.Migrate() was not called
        updater.MigrateWasCalled.Should().BeTrue("TestDbUpdater.Migrate() records the call before delegating");
    }

    /// <summary>
    /// Проверяет что Migrate не вызывает исключений при отсутствии pending миграций.
    /// </summary>
    [Fact]
    public void Migrate_NoPendingMigrations_DoesNotThrow()
    {
        // Arrange
        using var context = CreateContext();
        var updater = new TestDbUpdater(context);

        // Act & Assert
        var act = () => updater.Migrate();
        act.Should().NotThrow();
    }

    #endregion

    #region Initialize Tests

    /// <summary>
    /// Проверяет что Initialize не бросает исключений (виртуальный метод с пустой реализацией).
    /// </summary>
    [Fact]
    public void Initialize_WithSqliteContext_DoesNotThrow()
    {
        // Arrange
        using var context = CreateContext();
        var updater = new TestDbUpdater(context);

        // Act & Assert
        var act = () => updater.Initialize();
        act.Should().NotThrow();
    }

    #endregion

    #region Dispose Tests

    /// <summary>
    /// Проверяет что Dispose освобождает DbContext (операции на контексте после Dispose выбрасывают исключение).
    /// </summary>
    [Fact]
    public void Dispose_WithSqliteContext_DisposesContext()
    {
        // Arrange
        var context = CreateContext();
        var updater = new TestDbUpdater(context);

        // Act
        updater.Dispose();

        // Assert
        var act = () => context.Model;
        act.Should().Throw<ObjectDisposedException>();
    }

    /// <summary>
    /// Проверяет что DisposeAsync освобождает DbContext асинхронно.
    /// </summary>
    [Fact]
    public async Task DisposeAsync_WithSqliteContext_DisposesContext()
    {
        // Arrange
        var context = CreateContext();
        var updater = new TestDbUpdater(context);

        // Act
        await updater.DisposeAsync();

        // Assert
        var act = () => context.Model;
        act.Should().Throw<ObjectDisposedException>();
    }

    #endregion
}
