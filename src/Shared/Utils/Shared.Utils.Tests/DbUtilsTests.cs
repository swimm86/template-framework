using System.Data.Common;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Shared.Utils.DatabaseUpgrade.Tests;

/// <summary>
/// Тест для <see cref="DbUtils"/>.
/// </summary>
public class DbUtilsTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
            .WithDatabase($"epps_{nameof(DbUtilsTests)}")
            .Build();

    /// <inheritdoc/>
    public Task DisposeAsync()
    {
        return _container.DisposeAsync().AsTask();
    }

    /// <inheritdoc/>
    public Task InitializeAsync()
    {
        return _container.StartAsync();
    }

    /// <summary>
    /// Тест обновления бд при некорректных параметрах.
    /// </summary>
    [Fact]
    public void Upgrade_MigrationNotApplied_WhenInvalidParameters()
    {
        Assert.Throws<ArgumentException>(() => DbUtils.Upgrade(connectionString: string.Empty));

        Assert.Throws<ArgumentException>(() => DbUtils.Upgrade(connectionStringKey: "SomeConnectionStringKey"));

        Assert.Throws<InvalidOperationException>(() => DbUtils.Upgrade(
            connectionString: _container.GetConnectionString(),
            scriptsPath: string.Join(".", "DatabaseUpgrade", "InvalidScripts")));
    }

    /// <summary>
    /// Тест обновления бд и проверки корректности примененной миграции при корректных параметрах.
    /// </summary>
    [Fact]
    public void Upgrade_MigrationApplied_WhenCorrectParameters()
    {
        Assert.Null(
            Record.Exception(() => DbUtils.Upgrade(
                connectionString: _container.GetConnectionString(),
                scriptsPath: string.Join(".", "DatabaseUpgrade", "Scripts"))));

        Assert.True(CountMigrationApplied(_container.GetConnectionString()) == 1);

        Assert.True(CheckTestTableExists(_container.GetConnectionString()));
    }

    private static int CountMigrationApplied(string connectionString)
    {
        using DbConnection connection = new NpgsqlConnection(connectionString);
        using DbCommand command = new NpgsqlCommand();

        connection.Open();
        command.Connection = connection;
        command.CommandText = $"SELECT COUNT(*) FROM public.schemaversions WHERE scriptname LIKE '%001.epps.ddl_create_test_table.sql';";

        var reader = command.ExecuteReader();
        var migrationAppliedCount = 0;
        while (reader.Read())
        {
            _ = int.TryParse(reader["count"].ToString(), out migrationAppliedCount);
            break;
        }

        return migrationAppliedCount;
    }

    private static bool CheckTestTableExists(string connectionString)
    {
        using DbConnection connection = new NpgsqlConnection(connectionString);
        using DbCommand command = new NpgsqlCommand();

        connection.Open();
        command.Connection = connection;
        command.CommandText = $"SELECT EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = 'test');";

        var tableExists = false;
        var reader = command.ExecuteReader();
        while (reader.Read())
        {
            _ = bool.TryParse(reader["exists"].ToString(), out tableExists);
            break;
        }
        connection.Close();

        return tableExists;
    }
}
