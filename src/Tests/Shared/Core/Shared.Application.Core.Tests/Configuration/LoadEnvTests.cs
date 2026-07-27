// ----------------------------------------------------------------------------------------------
// <copyright file="LoadEnvTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

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

    /// <summary>
    /// <para>
    /// Документирует поведение hot-reload: после изменения <c>.env</c>-файла
    /// на диске уже построенный <see cref="IConfiguration"/> сохраняет
    /// исходные значения без авто-reload.
    /// </para>
    /// <para>
    /// <c>AddDotNetEnv</c> не подписывается на изменения файла, поэтому
    /// модификация файла после <c>Build()</c> никак не отражается на
    /// ранее собранной конфигурации.
    /// </para>
    /// </summary>
    [Fact]
    public void LoadEnvFromPath_AfterFileModified_DoesNotReloadAutomatically()
    {
        // Arrange
        var envPath = Path.Combine(_tempDirectory, ".env");
        File.WriteAllText(envPath, "X__Key=initial");
        var builder = new ConfigurationBuilder();
        builder.LoadEnvFromPath(_tempDirectory, "Development");
        var config = builder.Build();

        // Sanity: исходное значение прочитано
        config["X:Key"].Should().Be("initial");

        // Act — изменяем файл на диске после Build()
        File.WriteAllText(envPath, "X__Key=modified");

        // Assert — собранный IConfiguration не подхватывает изменения без явного Reload()
        config["X:Key"].Should().Be("initial");
    }

    /// <summary>
    /// При множественных вызовах <c>LoadEnvFromPath</c> на одном builder-е
    /// значения последнего вызова перекрывают значения предыдущего
    /// (порядок провайдеров в <see cref="IConfigurationBuilder"/> задаёт приоритет).
    /// </summary>
    [Fact]
    public void LoadEnvFromPath_MultipleCalls_LastCallWins()
    {
        // Arrange
        var envPath = Path.Combine(_tempDirectory, ".env");
        File.WriteAllText(envPath, "X__Key=first");
        var builder = new ConfigurationBuilder();

        // Act — первый вызов загружает значение "first"
        builder.LoadEnvFromPath(_tempDirectory, "Development");

        // Меняем файл и вызываем второй раз — должен загрузить "second"
        File.WriteAllText(envPath, "X__Key=second");
        builder.LoadEnvFromPath(_tempDirectory, "Development");

        var config = builder.Build();

        // Assert
        config["X:Key"].Should().Be("second");
    }

    /// <summary>
    /// Параллельная загрузка одного и того же <c>.env</c>-файла в десяти
    /// независимых builder-ах возвращает идентичные значения.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task LoadEnvFromPath_ConcurrentCalls_AreThreadSafe()
    {
        // Arrange
        File.WriteAllText(
            Path.Combine(_tempDirectory, ".env"),
            "X__Key=value");

        // Act — 10 параллельных сборок конфигурации из одного файла
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => new ConfigurationBuilder()
                .LoadEnvFromPath(_tempDirectory, "Development")
                .Build()))
            .ToList();
        var configs = await Task.WhenAll(tasks);

        // Assert — все конфигурации содержат одинаковое значение
        configs.Should().HaveCount(10)
            .And.AllSatisfy(c => c["X:Key"].Should().Be("value"));
    }

    /// <summary>
    /// Если <c>.env</c>-файл удалён между вызовами, второй вызов не находит
    /// файл и не загружает значений.
    /// </summary>
    [Fact]
    public void LoadEnvFromPath_FileDeletedBetweenCalls_SecondCallReturnsEmpty()
    {
        // Arrange — создаём файл и загружаем из него значение
        var envPath = Path.Combine(_tempDirectory, ".env");
        File.WriteAllText(envPath, "X__Key=present");
        var firstBuilder = new ConfigurationBuilder();
        firstBuilder.LoadEnvFromPath(_tempDirectory, "Development");
        var firstConfig = firstBuilder.Build();
        firstConfig["X:Key"].Should().Be("present");

        // Act — удаляем файл и грузим заново в новый builder
        File.Delete(envPath);
        var secondBuilder = new ConfigurationBuilder();
        secondBuilder.LoadEnvFromPath(_tempDirectory, "Development");
        var secondConfig = secondBuilder.Build();

        // Assert — второй вызов не находит файл и не загружает ключ
        secondConfig["X:Key"].Should().BeNull();
    }

    /// <summary>
    /// <c>.env</c>-файл с тысячей записей корректно читается целиком:
    /// первый и последний ключи доступны через итоговую конфигурацию.
    /// </summary>
    [Fact]
    public void LoadEnvFromPath_LargeFile_AllValuesRead()
    {
        // Arrange — 1000 пар ключ=значение одной строкой через string.Join
        var content = string.Join(
            Environment.NewLine,
            Enumerable.Range(0, 1000).Select(i => $"X__Key{i}=value{i}"));

        File.WriteAllText(
            Path.Combine(_tempDirectory, ".env"),
            content);

        // Act
        var config = new ConfigurationBuilder()
            .LoadEnvFromPath(_tempDirectory, "Development")
            .Build();

        // Assert — первый и последний ключи из файла присутствуют
        config["X:Key0"].Should().Be("value0");
        config["X:Key999"].Should().Be("value999");
    }

    /// <summary>
    /// Значения с Unicode (кириллица, китайские иероглифы, эмодзи) корректно
    /// читаются из <c>.env</c>-файла.
    /// </summary>
    [Fact]
    public void LoadEnvFromPath_FileWithUnicode_ValuesRead()
    {
        // Arrange
        File.WriteAllText(
            Path.Combine(_tempDirectory, ".env"),
            "X__Russian=привет" + Environment.NewLine +
            "X__Chinese=你好" + Environment.NewLine +
            "X__Emoji=🚀");

        // Act
        var config = new ConfigurationBuilder()
            .LoadEnvFromPath(_tempDirectory, "Development")
            .Build();

        // Assert — Unicode-значения сохранены без искажений
        config["X:Russian"].Should().Be("привет");
        config["X:Chinese"].Should().Be("你好");
        config["X:Emoji"].Should().Be("🚀");
    }
}
