using Shared.Utils.DatabaseUpgrade.Tests.Support;

namespace Shared.Utils.DatabaseUpgrade.Tests;

/// <summary>
/// Модульные тесты <see cref="DbUtils"/> без базы данных и Docker.
/// </summary>
/// <remarks>
/// Скрипты и PostgreSQL — см. <see cref="DbUtilsScriptIntegrationTests"/>.
/// </remarks>
public sealed class DbUtilsTests
{
    /// <summary>
    /// Пустая или отсутствующая строка подключения при отсутствии конфигурации в рабочей директории —
    /// <see cref="ArgumentException"/>.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Upgrade_WhenConnectionStringUnresolved_ThrowsArgumentException(string? connectionString)
    {
        DbUtilsTestSupport.RunInEmptyWorkingDirectory(() =>
        {
            var ex = Assert.Throws<ArgumentException>(() => DbUtils.Upgrade(connectionString: connectionString));
            Assert.Contains("строк", ex.Message, StringComparison.OrdinalIgnoreCase);
        });
    }

    /// <summary>
    /// Указан ключ строки подключения, но в конфигурации значения нет —
    /// <see cref="ArgumentException"/>.
    /// </summary>
    [Fact]
    public void Upgrade_WhenConnectionStringKeyHasNoConfigurationValue_ThrowsArgumentException()
    {
        DbUtilsTestSupport.RunInEmptyWorkingDirectory(() =>
        {
            var ex = Assert.Throws<ArgumentException>(
                () => DbUtils.Upgrade(connectionStringKey: "NonexistentConnectionStringKeyForUnitTests_9f3a2c1e"));
            Assert.Contains("строк", ex.Message, StringComparison.OrdinalIgnoreCase);
        });
    }

    /// <summary>
    /// Режим с аргументами командной строки без путей к скриптам — <see cref="ArgumentException"/>.
    /// </summary>
    [Fact]
    public void Upgrade_WhenArgsWithoutScriptPaths_ThrowsArgumentException()
    {
        var previousScriptPaths = Environment.GetEnvironmentVariable("ScriptPaths");
        try
        {
            Environment.SetEnvironmentVariable("ScriptPaths", null);
            var ex = Assert.Throws<ArgumentException>(() => DbUtils.Upgrade(Array.Empty<string>()));
            Assert.Contains("скрипт", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ScriptPaths", previousScriptPaths);
        }
    }
}
