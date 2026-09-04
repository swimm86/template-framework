// ----------------------------------------------------------------------------------------------
// <copyright file="EfUnitOfWorkIntegrationTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.LifecycleAction;
using Shared.Infrastructure.Dal.EFCore.Interfaces;
using Shared.Infrastructure.Dal.EFCore.Settings;
using Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Repository.Integration;

/// <summary>
/// Интеграционные тесты для <see cref="EfUnitOfWork{TDbContext}"/>,
/// использующие SQLite для поддержки реальных транзакций.
/// </summary>
[Trait("Category", "Integration")]
public sealed class EfUnitOfWorkIntegrationTests : SqliteUnitOfWorkIntegrationTestBase
{
    private static TestEntityWithCreatedDeleted CreateEntity(Guid? id = null, string name = "test")
    {
        return new TestEntityWithCreatedDeleted
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
        };
    }

    #region Transaction Commit Tests

    /// <summary>Проверяет что SaveChangesAsync коммитит транзакцию при успехе.</summary>
    [Fact]
    public async Task SaveChangesAsync_OnSuccess_CommitsTransaction()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context, transactionsEnabled: true);

        var entity = CreateEntity(name: "commit-test");
        context.Entities.Add(entity);

        // Act
        var result = await uow.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(1);
        context.Entities.Should().ContainSingle(e => e.Name == "commit-test");
    }

    #endregion

    #region Transaction Rollback Tests

    /// <summary>Проверяет что SaveChangesAsync откатывает транзакцию при ошибке в BeforeSave.</summary>
    [Fact]
    public async Task SaveChangesAsync_OnFailure_RollbacksTransaction()
    {
        // Arrange
        await using var context = CreateContext();
        var beforeSaveService = new TestBeforeSaveChangesService
        {
            OnProcessAsync = () => throw new InvalidOperationException("save failed"),
        };
        var settings = new IntegrationTestEfDbSettings(transactionsEnabled: true);
        var uow = new EfUnitOfWork<IntegrationTestUnitOfWorkDbContext>(
            context,
            new FakeServices(),
            settings,
            new LifecycleActionOrchestrator(
                [],
                new LifecycleEntityRegistry(),
                new LifecycleActionGate()),
            beforeSaveService);

        var entity = CreateEntity(name: "rollback-test");
        context.Entities.Add(entity);

        // Act
        var act = () => uow.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        context.Entities.Should().NotContain(e => e.Name == "rollback-test");
    }

    #endregion

    #region CommitTransactionAsync Tests

    /// <summary>Проверяет что CommitTransactionAsync успешно коммитит транзакцию.</summary>
    [Fact]
    public async Task CommitTransactionAsync_WhenEnabled_CommitsSuccessfully()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context, transactionsEnabled: true);

        // Act
        var act = () => uow.CommitTransactionAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region RollbackTransactionAsync Tests

    /// <summary>Проверяет что RollbackTransactionAsync успешно откатывает транзакцию.</summary>
    [Fact]
    public async Task RollbackTransactionAsync_WhenEnabled_RollbacksSuccessfully()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context, transactionsEnabled: true);

        // Act
        var act = () => uow.RollbackTransactionAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Dispose Tests

    /// <summary>Проверяет что Dispose освобождает текущую транзакцию.</summary>
    [Fact]
    public async Task Dispose_CommitTransactionThrowsAfterDispose()
    {
        // Arrange
        using var context = CreateContext();
        var uow = CreateUnitOfWork(context, transactionsEnabled: true);

        var actBefore = () => uow.CommitTransactionAsync(TestContext.Current.CancellationToken);
        await actBefore.Should().NotThrowAsync();

        // Act
        uow.Dispose();

        // Assert — after dispose, current transaction is released, commit fails
        var actAfter = () => uow.CommitTransactionAsync(TestContext.Current.CancellationToken);
        await actAfter.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

    #region Helpers

    private sealed class IntegrationTestEfDbSettings : EfDbSettingsBase<IntegrationTestUnitOfWorkDbContext>
    {
        [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
        public IntegrationTestEfDbSettings(bool transactionsEnabled = true)
        {
            ConnectionString = "DataSource=:memory:";
            TransactionsEnabled = transactionsEnabled;
        }
    }

    private sealed class FakeServices : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    #endregion
}
