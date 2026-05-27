using Shared.Utils.DatabaseUpgrade.Tests.Support;
using Testcontainers.PostgreSql;

namespace Shared.Utils.DatabaseUpgrade.Tests;

/// <summary>
/// Интеграционные тесты <see cref="DbUtils"/> с PostgreSQL (Testcontainers). Требуется Docker.
/// </summary>
/// <remarks>
/// Инфраструктура: <see cref="DbUtilsTestSupport"/>. Модульные сценарии — <see cref="DbUtilsTests"/>.
/// </remarks>
[Trait("Category", "Integration")]
public sealed class DbUtilsScriptIntegrationTests : IAsyncLifetime
{
    private const string ValidScriptFileName = "001.ddl_create_test_table.sql";

    private readonly PostgreSqlContainer _container = DbUtilsTestSupport.CreatePostgreSqlContainer(
        nameof(DbUtilsScriptIntegrationTests));

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => _container.DisposeAsync();

    /// <inheritdoc/>
    public ValueTask InitializeAsync() => new ValueTask(_container.StartAsync());

    /// <summary>
    /// Некорректный SQL в скрипте — <see cref="InvalidOperationException"/> после подключения к БД.
    /// </summary>
    [Fact]
    public void Upgrade_ThrowsInvalidOperation_WhenScriptHasSqlError()
    {
        // Arrange

        // Act
        var act = () => DbUtils.Upgrade(
            connectionString: _container.GetConnectionString(),
            scriptsPath: DbUtilsTestSupport.InvalidScriptsResourcePath);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    /// <summary>
    /// Корректные скрипты применяются: запись в журнале и ожидаемая таблица.
    /// </summary>
    [Fact]
    public void Upgrade_AppliesMigration_WhenScriptsAndConnectionAreValid()
    {
        // Arrange
        var cs = _container.GetConnectionString();

        // Act
        var act = () => DbUtils.Upgrade(
            connectionString: cs,
            scriptsPath: DbUtilsTestSupport.ScriptsResourcePath);

        // Assert
        act.Should().NotThrow();
        DbUtilsTestSupport.CountMigrationAppliedForScriptFile(cs, ValidScriptFileName)
            .Should().Be(1);
        DbUtilsTestSupport.TableExistsInPublic(cs, "test").Should().BeTrue();
    }
}
