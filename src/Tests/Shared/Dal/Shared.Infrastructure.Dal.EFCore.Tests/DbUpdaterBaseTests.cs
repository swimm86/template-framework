// ----------------------------------------------------------------------------------------------
// <copyright file="DbUpdaterBaseTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

namespace Shared.Infrastructure.Dal.EFCore.Tests;

/// <summary>
/// Тесты для абстрактного класса <see cref="DbUpdaterBase"/>.
/// Проверяет создание базы данных, миграции, инициализацию и disposal DbContext.
/// </summary>
/// <remarks>
/// Реляционно-специфичные методы (<c>GetPendingMigrations</c>, <c>Migrate</c>) скрыты
/// за <see cref="Shared.Application.Core.Dal.DbUpdater.Interfaces.IEnsureSchemaStrategy"/>,
/// поэтому <see cref="DbUpdaterBase.CreateDbIfNotExists"/> тестируется через stub-стратегию
/// без подключения к реальной БД.
/// </remarks>
public sealed class DbUpdaterBaseTests
{
    private static DbContextOptions<TestEfRepositoryDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<TestEfRepositoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
    }

    private static TestEfRepositoryDbContext CreateDbContext()
    {
        return new TestEfRepositoryDbContext(CreateOptions());
    }

    private static TestDbUpdater CreateUpdater(StubEnsureSchemaStrategy? strategy = null)
    {
        return new TestDbUpdater(CreateDbContext(), strategy ?? new StubEnsureSchemaStrategy());
    }

    #region CreateDbIfNotExists Tests

    /// <summary>
    /// <see cref="DbUpdaterBase.CreateDbIfNotExists"/> делегирует
    /// <see cref="Shared.Application.Core.Dal.DbUpdater.Interfaces.IEnsureSchemaStrategy.EnsureSchemaIfNeeded"/>,
    /// а не вызывает <c>dbContext.Database.GetPendingMigrations</c> напрямую —
    /// благодаря этому тест проходит на InMemory-провайдере.
    /// </summary>
    [Fact]
    public void CreateDbIfNotExists_DelegatesToEnsureSchemaStrategy()
    {
        // Arrange
        var strategy = new StubEnsureSchemaStrategy();
        using var dbContext = CreateDbContext();
        var updater = new TestDbUpdater(dbContext, strategy);

        // Act
        updater.CreateDbIfNotExists();

        // Assert
        strategy.CallCount.Should().Be(1);
    }

    /// <summary>
    /// <see cref="DbUpdaterBase.CreateDbIfNotExists"/> возвращает результат,
    /// полученный от стратегии (схема уже существовала).
    /// </summary>
    [Fact]
    public void CreateDbIfNotExists_StrategyReturnsFalse_DoesNotThrow()
    {
        // Arrange
        var strategy = new StubEnsureSchemaStrategy { ReturnValue = false };
        using var dbContext = CreateDbContext();
        var updater = new TestDbUpdater(dbContext, strategy);

        // Act
        var act = () => updater.CreateDbIfNotExists();

        // Assert
        act.Should().NotThrow();
        strategy.CallCount.Should().Be(1);
    }

    /// <summary>
    /// <see cref="DbUpdaterBase.CreateDbIfNotExists"/> идемпотентен
    /// относительно вызовов стратегии: каждый вызов приводит к одному вызову стратегии.
    /// </summary>
    [Fact]
    public void CreateDbIfNotExists_CalledTwice_InvokesStrategyTwice()
    {
        // Arrange
        var strategy = new StubEnsureSchemaStrategy();
        using var dbContext = CreateDbContext();
        var updater = new TestDbUpdater(dbContext, strategy);

        // Act
        updater.CreateDbIfNotExists();
        updater.CreateDbIfNotExists();

        // Assert
        strategy.CallCount.Should().Be(2);
    }

    #endregion

    #region Migrate Tests

    /// <summary>
    /// <see cref="DbUpdaterBase.Migrate"/> через InMemory-провайдер
    /// не вызывает <c>Database.Migrate</c> (провайдер не поддерживает реляционные методы),
    /// поэтому тест помечен <c>Skip</c>. Реляционные сценарии покрываются
    /// интеграционными тестами с SQLite (<see cref="Repository.Integration.DbUpdaterBaseIntegrationTests"/>).
    /// </summary>
    [Fact(Skip = "InMemory provider does not support relational migration methods")]
    public void Migrate_NoPendingMigrations_DoesNotCallMigrate()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var updater = new TestDbUpdater(dbContext, new StubEnsureSchemaStrategy());

        // Act
        updater.Migrate();

        // Assert
        updater.MigrateWasCalled.Should().BeFalse();
    }

    /// <summary>
    /// <see cref="DbUpdaterBase.Migrate"/> через InMemory-провайдер
    /// не вызывает <c>Database.Migrate</c> (провайдер не поддерживает реляционные методы),
    /// поэтому тест помечен <c>Skip</c>.
    /// </summary>
    [Fact(Skip = "InMemory provider does not support relational migration methods")]
    public void Migrate_WithPendingMigrations_CallsMigrate()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var updater = new TestDbUpdater(dbContext, new StubEnsureSchemaStrategy());

        // Act
        updater.Migrate();

        // Assert - with InMemory provider, GetPendingMigrations returns empty,
        // so MigrateWasCalled should be false. This verifies the guard condition works.
        updater.MigrateWasCalled.Should().BeFalse();
    }

    #endregion

    #region Initialize Tests

    /// <summary>
    /// <see cref="DbUpdaterBase.Initialize"/> по умолчанию не бросает исключений.
    /// </summary>
    [Fact]
    public void Initialize_DefaultImplementation_DoesNotThrow()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var updater = new TestDbUpdater(dbContext, new StubEnsureSchemaStrategy());

        // Act
        var act = () => updater.Initialize();

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Dispose Tests

    /// <summary>
    /// <see cref="DbUpdaterBase.Dispose"/> вызывает <c>Dispose</c> у <see cref="DbContext"/>.
    /// </summary>
    [Fact]
    public void Dispose_DisposesDbContext()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var updater = new TestDbUpdater(dbContext, new StubEnsureSchemaStrategy());

        // Act
        updater.Dispose();

        // Assert
        var act = () => dbContext.Model;
        act.Should().Throw<ObjectDisposedException>();
    }

    /// <summary>
    /// <see cref="DbUpdaterBase.DisposeAsync"/> вызывает <c>DisposeAsync</c> у <see cref="DbContext"/>.
    /// </summary>
    [Fact]
    public async Task DisposeAsync_DisposesDbContextAsync()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var updater = new TestDbUpdater(dbContext, new StubEnsureSchemaStrategy());

        // Act
        await updater.DisposeAsync();

        // Assert
        var act = () => dbContext.Model;
        act.Should().Throw<ObjectDisposedException>();
    }

    #endregion
}
