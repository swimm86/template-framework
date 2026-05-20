// ----------------------------------------------------------------------------------------------
// <copyright file="DbContextBaseTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

namespace Shared.Infrastructure.Dal.EFCore.Tests;

/// <summary>
/// Тесты для базового класса <see cref="DbContextBase"/>.
/// Проверяет конфигурацию логирования, культуры и conventions.
/// </summary>
public sealed class DbContextBaseTests
{
    #region OnConfiguring

    /// <summary>
    /// Проверяет, что в Development-окружении включается EnableSensitiveDataLogging.
    /// Проверяется через CoreOptionsExtension.IsSensitiveDataLoggingEnabled из внутреннего сервиса.
    /// </summary>
    [Fact]
    public async Task OnConfiguring_DevelopmentEnvironment_EnablesSensitiveDataLogging()
    {
        // Arrange
        var environment = CreateMockEnvironment(isDevelopment: true);
        var options = new DbContextOptionsBuilder<TestDbContextForBase>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        // Act
        await using var context = new TestDbContextForBase(options, environment);
        _ = context.Model;

        // Assert — sensitive data logging must be enabled for Development
        var coreOptions = context.GetService<IDbContextOptions>()
            .FindExtension<CoreOptionsExtension>();
        coreOptions.Should().NotBeNull();
        coreOptions.IsSensitiveDataLoggingEnabled.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что в Production-окружении EnableSensitiveDataLogging не включается.
    /// </summary>
    [Fact]
    public async Task OnConfiguring_ProductionEnvironment_DoesNotEnableSensitiveDataLogging()
    {
        // Arrange
        var environment = CreateMockEnvironment(isDevelopment: false);
        var options = new DbContextOptionsBuilder<TestDbContextForBase>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        // Act
        await using var context = new TestDbContextForBase(options, environment);
        _ = context.Model;

        // Assert — sensitive data logging must NOT be enabled for Production
        var coreOptions = context.GetService<IDbContextOptions>()
            .FindExtension<CoreOptionsExtension>();
        // CoreOptionsExtension may be null or have IsSensitiveDataLoggingEnabled = false
        var sensitiveLoggingEnabled = coreOptions?.IsSensitiveDataLoggingEnabled ?? false;
        sensitiveLoggingEnabled.Should().BeFalse();
    }

    /// <summary>
    /// Проверяет, что при конфигурации устанавливается DefaultThreadCurrentCulture и DefaultThreadCurrentUICulture.
    /// </summary>
    [Fact]
    public async Task OnConfiguring_SetsDefaultThreadCulture()
    {
        // Arrange
        var originalCulture = CultureInfo.DefaultThreadCurrentCulture;
        var originalUiCulture = CultureInfo.DefaultThreadCurrentUICulture;
        try
        {
            var environment = CreateMockEnvironment(isDevelopment: false);
            var options = new DbContextOptionsBuilder<TestDbContextForBase>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            // Act
            await using var context = new TestDbContextForBase(options, environment);
            _ = context.Model;

            // Assert
            CultureInfo.DefaultThreadCurrentCulture.Should().NotBeNull();
            CultureInfo.DefaultThreadCurrentCulture.Name.Should().Be("en-US");
            CultureInfo.DefaultThreadCurrentCulture.DateTimeFormat.ShortDatePattern.Should().Be("dd/MM/yyyy");

            CultureInfo.DefaultThreadCurrentUICulture.Should().NotBeNull();
            CultureInfo.DefaultThreadCurrentUICulture.Name.Should().Be("en-US");
            CultureInfo.DefaultThreadCurrentUICulture.DateTimeFormat.ShortDatePattern.Should().Be("dd/MM/yyyy");
        }
        finally
        {
            CultureInfo.DefaultThreadCurrentCulture = originalCulture;
            CultureInfo.DefaultThreadCurrentUICulture = originalUiCulture;
        }
    }

    #endregion

    #region ConfigureConventions

    /// <summary>
    /// Проверяет, что добавляется ColumnsNamesConvention для snake_case имён колонок.
    /// </summary>
    [Fact]
    public async Task ConfigureConventions_AddsColumnsNamesConvention()
    {
        // Arrange
        var environment = CreateMockEnvironment(isDevelopment: false);
        var options = new DbContextOptionsBuilder<TestDbContextForBase>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        // Act
        await using var context = new TestDbContextForBase(options, environment);
        var entityType = context.Model.FindEntityType(typeof(TestEfEntity));
        entityType.Should().NotBeNull();

        // Assert
        var dateCreatedColumn = entityType.FindProperty(nameof(TestEfEntity.DateCreated));
        dateCreatedColumn.Should().NotBeNull();
        dateCreatedColumn.GetColumnName().Should().Be("date_created");
    }

    #endregion

    #region OnModelCreating

    /// <summary>
    /// Проверяет, что применяются конфигурации из вызывающей сборки (Assembly.GetCallingAssembly).
    /// </summary>
    [Fact]
    public async Task OnModelCreating_AppliesConfigurationsFromCallingAssembly()
    {
        // Arrange
        var environment = CreateMockEnvironment(isDevelopment: false);
        var options = new DbContextOptionsBuilder<TestDbContextForBase>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        // Act
        await using var context = new TestDbContextForBase(options, environment);

        // Assert - TestEfEntity зарегистрирован через OnModelCreating в TestDbContextForBase
        var entityType = context.Model.FindEntityType(typeof(TestEfEntity));
        entityType.Should().NotBeNull();

        // Id имеет ValueGeneratedNever, как настроено в TestDbContextForBase.OnModelCreating
        var idProperty = entityType.FindProperty(nameof(TestEfEntity.Id));
        idProperty.Should().NotBeNull();
        idProperty.ValueGenerated.Should().Be(ValueGenerated.Never);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Создаёт mock <see cref="IHostEnvironment"/> с заданным режимом Development.
    /// </summary>
    /// <param name="isDevelopment">Флаг, указывающий является ли окружение Development.</param>
    /// <returns>Mock <see cref="IHostEnvironment"/>.</returns>
    private static IHostEnvironment CreateMockEnvironment(bool isDevelopment)
    {
        return new MockHostEnvironment(isDevelopment);
    }

    /// <summary>
    /// Простая реализация <see cref="IHostEnvironment"/> для тестирования.
    /// </summary>
    private sealed class MockHostEnvironment(bool isDevelopment) : IHostEnvironment
    {
        /// <inheritdoc />
        public string EnvironmentName { get; set; } = isDevelopment
            ? Environments.Development
            : Environments.Production;

        /// <inheritdoc />
        public string ApplicationName { get; set; } = "TestApplication";

        /// <inheritdoc />
        public string ContentRootPath { get; set; } = string.Empty;

        /// <inheritdoc />
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    #endregion
}
