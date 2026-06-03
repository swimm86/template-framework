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
            // Arrange
            var act = () => DbUtils.Upgrade(connectionString: connectionString);

            // Act

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*connection string*");
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
            // Arrange
            var act = () => DbUtils.Upgrade(connectionStringKey: "NonexistentConnectionStringKeyForUnitTests_9f3a2c1e");

            // Act

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*connection string*");
        });
    }

    /// <summary>
    /// Режим с аргументами командной строки без путей к скриптам — <see cref="ArgumentException"/>.
    /// </summary>
    [Fact]
    public void Upgrade_WhenArgsWithoutScriptPaths_ThrowsArgumentException()
    {
        // Arrange
        var previousScriptPaths = Environment.GetEnvironmentVariable("ScriptPaths");
        try
        {
            Environment.SetEnvironmentVariable("ScriptPaths", null);
            var act = () => DbUtils.Upgrade([]);

            // Act

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*script*");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ScriptPaths", previousScriptPaths);
        }
    }
}
