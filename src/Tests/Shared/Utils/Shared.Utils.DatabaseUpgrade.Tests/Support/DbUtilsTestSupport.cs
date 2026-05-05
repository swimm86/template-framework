using Npgsql;
using Testcontainers.PostgreSql;

namespace Shared.Utils.DatabaseUpgrade.Tests.Support;

/// <summary>
/// Общая инфраструктура для тестов <see cref="DbUtils"/> — вынесена в <c>Support</c>, по тому же принципу, что и хелперы в <c>Shared.Application.Core.Tests.Support</c>.
/// </summary>
public static class DbUtilsTestSupport
{
    internal const string ScriptsResourcePath = "Scripts";

    internal const string InvalidScriptsResourcePath = "InvalidScripts";

    /// <summary>
    /// Образ PostgreSQL для Testcontainers (явная версия, без устаревшего конструктора билдера).
    /// </summary>
    public const string PostgreSqlDockerImage = "postgres:16-alpine";

    /// <summary>
    /// Создаёт контейнер PostgreSQL для интеграционных тестов миграций.
    /// </summary>
    public static PostgreSqlContainer CreatePostgreSqlContainer(string databaseName)
    {
        return new PostgreSqlBuilder(PostgreSqlDockerImage)
            .WithDatabase(databaseName)
            .Build();
    }

    /// <summary>
    /// Выполняет действие во временной пустой рабочей директории (без посторонних appsettings).
    /// </summary>
    public static void RunInEmptyWorkingDirectory(Action test)
    {
        var temp = Directory.CreateTempSubdirectory("DbUtilsTests_");
        var previousCwd = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = temp.FullName;
            test();
        }
        finally
        {
            Environment.CurrentDirectory = previousCwd;
            try
            {
                Directory.Delete(path: temp.FullName, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }

    /// <summary>
    /// Количество записей о применённом скрипте в журнале DbUp.
    /// </summary>
    public static int CountMigrationAppliedForScriptFile(string connectionString, string scriptFileNameSuffix)
    {
        using var connection = new NpgsqlConnection(connectionString);
        using var command = new NpgsqlCommand();

        connection.Open();
        command.Connection = connection;
        command.CommandText =
            "SELECT COUNT(*) FROM public.schemaversions WHERE scriptname LIKE '%' || @s;";
        command.Parameters.AddWithValue(parameterName: "s", value: scriptFileNameSuffix);

        using var reader = command.ExecuteReader();
        var migrationAppliedCount = 0;
        while (reader.Read())
        {
            _ = int.TryParse(reader["count"]?.ToString(), out migrationAppliedCount);
            break;
        }

        return migrationAppliedCount;
    }

    /// <summary>
    /// Существование таблицы в схеме public.
    /// </summary>
    public static bool TableExistsInPublic(string connectionString, string tableName)
    {
        using var connection = new NpgsqlConnection(connectionString);
        using var command = new NpgsqlCommand();

        connection.Open();
        command.Connection = connection;
        command.CommandText =
            "SELECT EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = @t);";
        command.Parameters.AddWithValue(parameterName: "t", value: tableName);

        using var reader = command.ExecuteReader();
        var tableExists = false;
        while (reader.Read())
        {
            _ = bool.TryParse(reader["exists"]?.ToString(), out tableExists);
            break;
        }

        return tableExists;
    }
}
