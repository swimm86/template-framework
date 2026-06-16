using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shared.Infrastructure.Dal.EFCore.Postgres;
using Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Extensions;

/// <summary>
/// Тесты валидации строки подключения в <see cref="DbContextOptionsBuilderInitializer"/>.
/// Проверяет, что initializer бросает <see cref="InvalidOperationException"/>
/// с понятным сообщением, если connection string не сконфигурирован.
/// </summary>
public sealed class DbContextOptionsBuilderInitializerConnectionStringTests
{
    /// <summary>
    /// Пустая конфигурация (без секции с <c>ConnectionString</c>) —
    /// <see cref="InvalidOperationException"/> с упоминанием имени типа настроек.
    /// </summary>
    [Fact]
    public void Initialize_EmptyConfiguration_ThrowsInvalidOperationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var initializer = new DbContextOptionsBuilderInitializer(configuration);
        var options = new DbContextOptionsBuilder();

        // Act
        var act = () => initializer.Initialize<EmptyConnectionStringDbSettings>(
            options,
            migrationAssemblyName: "Fake.Migrations");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*EmptyConnectionStringDbSettings*not configured*");
    }

    /// <summary>
    /// Сконфигурирован, но пустой connection string — <see cref="InvalidOperationException"/>.
    /// </summary>
    [Fact]
    public void Initialize_EmptyConnectionString_ThrowsInvalidOperationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EmptyConnectionStringDbSettings:ConnectionString"] = string.Empty,
                ["EmptyConnectionStringDbSettings:TransactionsEnabled"] = "true",
            })
            .Build();
        var initializer = new DbContextOptionsBuilderInitializer(configuration);
        var options = new DbContextOptionsBuilder();

        // Act
        var act = () => initializer.Initialize<EmptyConnectionStringDbSettings>(
            options,
            migrationAssemblyName: "Fake.Migrations");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not configured*");
    }

    /// <summary>
    /// Сконфигурирован, но whitespace-only connection string — <see cref="InvalidOperationException"/>.
    /// </summary>
    [Fact]
    public void Initialize_WhitespaceConnectionString_ThrowsInvalidOperationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["WhitespaceConnectionStringDbSettings:ConnectionString"] = "   ",
                ["WhitespaceConnectionStringDbSettings:TransactionsEnabled"] = "true",
            })
            .Build();
        var initializer = new DbContextOptionsBuilderInitializer(configuration);
        var options = new DbContextOptionsBuilder();

        // Act
        var act = () => initializer.Initialize<WhitespaceConnectionStringDbSettings>(
            options,
            migrationAssemblyName: "Fake.Migrations");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not configured*");
    }
}
