// ----------------------------------------------------------------------------------------------
// <copyright file="DbContextOptionsBuilderInitializerSensitiveLoggingTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Shared.Common.Helpers;
using Shared.Infrastructure.Dal.EFCore.Postgres;
using Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Extensions;

/// <summary>
/// Тесты <see cref="DbContextOptionsBuilderInitializer"/> для флага
/// <c>EnableSensitiveDataLogging</c> из <see cref="Shared.Application.Core.Dal.Settings.Models.Base.DbSettingsBase"/>.
/// </summary>
public sealed class DbContextOptionsBuilderInitializerSensitiveLoggingTests
{
    private static IConfiguration BuildConfiguration<T>()
    {
        var moduleName = Shared.Common.Helpers.AssemblyHelper.GetModuleName();
        var sectionPath = string.Join(":", moduleName.Split('.')) + ":" + typeof(T).Name;
        var enableFlag = typeof(T) == typeof(SensitiveLoggingEnabledDbSettings) ? "true" : "false";
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{sectionPath}:ConnectionString"] = "Host=localhost;Database=test;Username=test;Password=test",
                [$"{sectionPath}:TransactionsEnabled"] = "true",
                [$"{sectionPath}:EnableSensitiveDataLogging"] = enableFlag,
            })
            .Build();
    }

    private static bool IsSensitiveDataLoggingEnabled(DbContextOptionsBuilder options)
    {
        return options.Options
            .FindExtension<CoreOptionsExtension>()
            ?.IsSensitiveDataLoggingEnabled ?? false;
    }

    /// <summary>
    /// При <c>EnableSensitiveDataLogging = true</c> в настройках — флаг включается в опциях.
    /// </summary>
    [Fact]
    public void Initialize_EnableSensitiveDataLoggingTrue_EnablesSensitiveDataLogging()
    {
        // Arrange
        var configuration = BuildConfiguration<SensitiveLoggingEnabledDbSettings>();
        var initializer = new DbContextOptionsBuilderInitializer(configuration);
        var options = new DbContextOptionsBuilder();

        // Act
        initializer.Initialize<SensitiveLoggingEnabledDbSettings>(
            options,
            migrationAssemblyName: "Fake.Migrations");

        // Assert
        IsSensitiveDataLoggingEnabled(options).Should().BeTrue();
    }

    /// <summary>
    /// При <c>EnableSensitiveDataLogging = false</c> в настройках — флаг остаётся выключенным.
    /// </summary>
    [Fact]
    public void Initialize_EnableSensitiveDataLoggingFalse_LeavesSensitiveDataLoggingDisabled()
    {
        // Arrange
        var configuration = BuildConfiguration<SensitiveLoggingDisabledDbSettings>();
        var initializer = new DbContextOptionsBuilderInitializer(configuration);
        var options = new DbContextOptionsBuilder();

        // Act
        initializer.Initialize<SensitiveLoggingDisabledDbSettings>(
            options,
            migrationAssemblyName: "Fake.Migrations");

        // Assert
        IsSensitiveDataLoggingEnabled(options).Should().BeFalse();
    }
}
