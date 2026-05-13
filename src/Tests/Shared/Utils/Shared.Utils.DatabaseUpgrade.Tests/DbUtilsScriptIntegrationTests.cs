using Shared.Utils.DatabaseUpgrade.Tests.Support;
using Testcontainers.PostgreSql;

namespace Shared.Utils.DatabaseUpgrade.Tests;

/// <summary>
/// Интеграционные тесты <see cref="DbUtils"/> с PostgreSQL (Testcontainers). Требуется Docker.
/// </summary>
/// <remarks>
/// Инфраструктура: <see cref="DbUtilsTestSupport"/>. Модульные сценарии — <see cref="DbUtilsTests"/>.
/// </remarks>
public sealed class DbUtilsScriptIntegrationTests : IAsyncLifetime
{
    private const string ValidScriptFileName = "001.epps.ddl_create_test_table.sql";

    private readonly PostgreSqlContainer _container = DbUtilsTestSupport.CreatePostgreSqlContainer(
        $"epps_{nameof(DbUtilsScriptIntegrationTests)}");

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
        Assert.Throws<InvalidOperationException>(() => DbUtils.Upgrade(
            connectionString: _container.GetConnectionString(),
            scriptsPath: DbUtilsTestSupport.InvalidScriptsResourcePath));
    }

    /// <summary>
    /// Корректные скрипты применяются: запись в журнале и ожидаемая таблица.
    /// </summary>
    [Fact]
    public void Upgrade_AppliesMigration_WhenScriptsAndConnectionAreValid()
    {
        var cs = _container.GetConnectionString();

        Assert.Null(
            Record.Exception(() => DbUtils.Upgrade(
                connectionString: cs,
                scriptsPath: DbUtilsTestSupport.ScriptsResourcePath)));

        Assert.Equal(
            1,
            DbUtilsTestSupport.CountMigrationAppliedForScriptFile(cs, ValidScriptFileName));
        Assert.True(DbUtilsTestSupport.TableExistsInPublic(cs, "test"));
    }
}
