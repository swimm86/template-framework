// ----------------------------------------------------------------------------------------------
// <copyright file="RelationalEnsureSchemaStrategyTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Dal.EFCore;
using Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;
using Shared.Infrastructure.Dal.EFCore.Tests.Repository.Integration;

namespace Shared.Infrastructure.Dal.EFCore.Tests;

/// <summary>
/// Тесты <see cref="RelationalEnsureSchemaStrategy{TContext}"/>.
/// Проверяет, что стратегия делегирует <c>GetPendingMigrations</c> и <c>EnsureCreated</c>
/// в правильном порядке.
/// </summary>
public sealed class RelationalEnsureSchemaStrategyTests
{
    [Fact]
    public void EnsureSchemaIfNeeded_NoPendingMigrations_CreatesSchema()
    {
        // Arrange
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        try
        {
            var options = new DbContextOptionsBuilder<IntegrationTestDbContext>()
                .UseSqlite(connection)
                .Options;
            var factory = new TestDbContextFactory<IntegrationTestDbContext>(options);
            var strategy = new RelationalEnsureSchemaStrategy<IntegrationTestDbContext>(factory);

            // Act
            var created = strategy.EnsureSchemaIfNeeded();

            // Assert
            created.Should().BeTrue("без pending-миграций EnsureCreated должен вернуть true");

            // Схема реально создана: можно выполнить запрос
            using var context = factory.CreateDbContext();
            context.Entities.Add(new TestEntityWithCreatedDeleted
            {
                Id = Guid.NewGuid(),
                Name = "schema-check",
            });
            var act = () => context.SaveChanges();
            act.Should().NotThrow();
        }
        finally
        {
            connection.Dispose();
        }
    }

    [Fact]
    public void EnsureSchemaIfNeeded_CalledTwice_IsIdempotent()
    {
        // Arrange
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        try
        {
            var options = new DbContextOptionsBuilder<IntegrationTestDbContext>()
                .UseSqlite(connection)
                .Options;
            var factory = new TestDbContextFactory<IntegrationTestDbContext>(options);
            var strategy = new RelationalEnsureSchemaStrategy<IntegrationTestDbContext>(factory);

            // Act
            var firstCall = strategy.EnsureSchemaIfNeeded();
            var secondCall = strategy.EnsureSchemaIfNeeded();

            // Assert
            firstCall.Should().BeTrue();
            secondCall.Should().BeFalse("при повторном вызове схема уже существует");
        }
        finally
        {
            connection.Dispose();
        }
    }
}
