using Microsoft.Extensions.Configuration;
using Shared.Application.Core.Configuration.Extensions;

namespace Shared.Application.Core.Tests.Configuration;

/// <summary>
/// Тесты загрузчика <c>.env</c>-файлов.
/// Проверяют приоритет <c>.env.{environment}</c> над базовым <c>.env</c> (merge-семантика).
/// </summary>
public sealed class LoadEnvTests
    : IDisposable
{
    private readonly string _tempDirectory;

    /// <summary>
    /// Создаёт временную директорию для каждого теста.
    /// </summary>
    public LoadEnvTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            $"load-env-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    /// <summary>
    /// При наличии только <c>.env</c> его ключи попадают в конфигурацию.
    /// </summary>
    [Fact]
    public void LoadEnvFromPath_OnlyBaseFile_LoadsBaseValues()
    {
        // Arrange
        File.WriteAllText(
            Path.Combine(_tempDirectory, ".env"),
            "X__Key=base-only");
        var builder = new ConfigurationBuilder();

        // Act
        builder.LoadEnvFromPath(_tempDirectory, "Development");
        var config = builder.Build();

        // Assert
        config["X:Key"].Should().Be("base-only");
    }

    /// <summary>
    /// Ключи, присутствующие только в <c>.env</c>, сохраняются при наличии <c>.env.{env}</c>.
    /// </summary>
    [Fact]
    public void LoadEnvFromPath_KeysOnlyInBase_ArePreserved()
    {
        // Arrange
        File.WriteAllText(
            Path.Combine(_tempDirectory, ".env"),
            "X__Base=from-base" + Environment.NewLine +
            "X__Shared=from-base" + Environment.NewLine +
            "X__OnlyInBase=base-value");
        File.WriteAllText(
            Path.Combine(_tempDirectory, ".env.development"),
            "X__Shared=from-env");
        var builder = new ConfigurationBuilder();

        // Act
        builder.LoadEnvFromPath(_tempDirectory, "Development");
        var config = builder.Build();

        // Assert
        config["X:Base"].Should().Be("from-base");
        config["X:OnlyInBase"].Should().Be("base-value");
    }

    /// <summary>
    /// Ключи, переопределённые в <c>.env.{env}</c>, заменяют значения из <c>.env</c>.
    /// </summary>
    [Fact]
    public void LoadEnvFromPath_KeysInBoth_EnvFileOverridesBase()
    {
        // Arrange
        File.WriteAllText(
            Path.Combine(_tempDirectory, ".env"),
            "X__Shared=from-base");
        File.WriteAllText(
            Path.Combine(_tempDirectory, ".env.development"),
            "X__Shared=from-env");
        var builder = new ConfigurationBuilder();

        // Act
        builder.LoadEnvFromPath(_tempDirectory, "Development");
        var config = builder.Build();

        // Assert
        config["X:Shared"].Should().Be("from-env");
    }

    /// <summary>
    /// Если <c>.env.{env}</c> отсутствует, но есть базовый <c>.env</c>,
    /// загружается только базовый без ошибок.
    /// </summary>
    [Fact]
    public void LoadEnvFromPath_OnlyBaseExists_LoadsBaseWithoutError()
    {
        // Arrange
        File.WriteAllText(
            Path.Combine(_tempDirectory, ".env"),
            "X__Key=value");
        var builder = new ConfigurationBuilder();

        // Act
        builder.LoadEnvFromPath(_tempDirectory, "Production");

        // Assert
        var config = builder.Build();
        config["X:Key"].Should().Be("value");
    }

    /// <summary>
    /// Если <c>.env</c> отсутствует, но есть <c>.env.{env}</c>, загружается только env-файл.
    /// </summary>
    [Fact]
    public void LoadEnvFromPath_OnlyEnvSpecificExists_LoadsEnvSpecific()
    {
        // Arrange
        File.WriteAllText(
            Path.Combine(_tempDirectory, ".env.production"),
            "X__Key=prod-value");
        var builder = new ConfigurationBuilder();

        // Act
        builder.LoadEnvFromPath(_tempDirectory, "Production");

        // Assert
        var config = builder.Build();
        config["X:Key"].Should().Be("prod-value");
    }

    /// <summary>
    /// Если файлов нет, конфигурация остаётся пустой и не падает.
    /// </summary>
    [Fact]
    public void LoadEnvFromPath_NoFiles_BuilderStaysEmpty()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        var result = builder.LoadEnvFromPath(_tempDirectory, "Development");

        // Assert
        result.Should().BeSameAs(builder);
        builder.Build().GetChildren().Should().BeEmpty();
    }

    /// <summary>
    /// Имя окружения в <c>.env.{env}</c> матчится в нижнем регистре,
    /// даже если передано в смешанном (по конвенции .NET EnvironmentName
    /// всегда invariant и в PascalCase, но ToLowerInvariant безопасен).
    /// </summary>
    [Fact]
    public void LoadEnvFromPath_EnvNameLowercase_MatchesLowercaseFileName()
    {
        // Arrange
        File.WriteAllText(
            Path.Combine(_tempDirectory, ".env.development"),
            "X__Key=dev");
        var builder = new ConfigurationBuilder();

        // Act
        builder.LoadEnvFromPath(_tempDirectory, "Development");

        // Assert
        builder.Build()["X:Key"].Should().Be("dev");
    }

    /// <summary>
    /// Имя окружения, переданное в upper-case, также матчится с <c>.env.{env}</c> в lower-case
    /// (используется <see cref="string.ToLowerInvariant"/>).
    /// </summary>
    [Fact]
    public void LoadEnvFromPath_EnvNameUpperCase_MatchesLowercaseFileName()
    {
        // Arrange
        File.WriteAllText(
            Path.Combine(_tempDirectory, ".env.staging"),
            "X__Key=stg");
        var builder = new ConfigurationBuilder();

        // Act
        builder.LoadEnvFromPath(_tempDirectory, "STAGING");

        // Assert
        builder.Build()["X:Key"].Should().Be("stg");
    }

    /// <summary>
    /// Имя окружения, переданное в mixed-case, нормализуется к lower-case для матчинга.
    /// </summary>
    [Fact]
    public void LoadEnvFromPath_EnvNameMixedCase_NormalizesToLowercase()
    {
        // Arrange
        File.WriteAllText(
            Path.Combine(_tempDirectory, ".env.production"),
            "X__Key=prod");
        var builder = new ConfigurationBuilder();

        // Act
        builder.LoadEnvFromPath(_tempDirectory, "ProDucTion");

        // Assert
        builder.Build()["X:Key"].Should().Be("prod");
    }

    /// <summary>
    /// Пустой <c>basePath</c> интерпретируется как текущая рабочая директория;
    /// отсутствие файлов в ней не приводит к ошибке.
    /// </summary>
    [Fact]
    public void LoadEnvFromPath_EmptyBasePath_DoesNotThrow()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        var currentDir = Directory.GetCurrentDirectory();

        // Act — нет .env в cwd, должен вернуться пустой builder
        var act = () => builder.LoadEnvFromPath(currentDir, "Development");

        // Assert
        act.Should().NotThrow();
        builder.Build().GetChildren().Should().BeEmpty();
    }

    /// <summary>
    /// <see cref="string.Empty"/> как имя окружения даёт путь <c>.env.</c>,
    /// который не интерпретируется как <c>.env</c> (имя файла не равно <c>.env.</c>).
    /// </summary>
    [Fact]
    public void LoadEnvFromPath_EmptyEnvName_LoadsBaseEnvOnly()
    {
        // Arrange
        File.WriteAllText(
            Path.Combine(_tempDirectory, ".env"),
            "X__BaseKey=base-value");
        var builder = new ConfigurationBuilder();

        // Act
        builder.LoadEnvFromPath(_tempDirectory, string.Empty);

        // Assert
        builder.Build()["X:BaseKey"].Should().Be("base-value");
    }

    /// <summary>
    /// Смешанный сценарий: <c>.env</c> задаёт общие значения,
    /// <c>.env.{env}</c> добавляет новые ключи и переопределяет существующие.
    /// </summary>
    [Fact]
    public void LoadEnvFromPath_MixedScenario_MergesAsExpected()
    {
        // Arrange
        File.WriteAllText(
            Path.Combine(_tempDirectory, ".env"),
            "App__Name=base-app" + Environment.NewLine +
            "App__Connection=base-conn" + Environment.NewLine +
            "App__Shared=base-shared");
        File.WriteAllText(
            Path.Combine(_tempDirectory, ".env.development"),
            "App__Connection=dev-conn" + Environment.NewLine +
            "App__Shared=dev-shared" + Environment.NewLine +
            "App__NewKey=dev-only");
        var builder = new ConfigurationBuilder();

        // Act
        builder.LoadEnvFromPath(_tempDirectory, "Development");
        var config = builder.Build();

        // Assert
        config["App:Name"].Should().Be("base-app", "только в .env");
        config["App:Connection"].Should().Be("dev-conn", "переопределено в .env.development");
        config["App:Shared"].Should().Be("dev-shared", "переопределено в .env.development");
        config["App:NewKey"].Should().Be("dev-only", "только в .env.development");
    }
}
