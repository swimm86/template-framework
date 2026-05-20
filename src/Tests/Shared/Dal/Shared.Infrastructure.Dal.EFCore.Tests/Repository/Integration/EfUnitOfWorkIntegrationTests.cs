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

    private static TestEfUnitOfWorkWrapper CreateUnitOfWorkWrapper(
        IntegrationTestUnitOfWorkDbContext context,
        bool transactionsEnabled = true)
    {
        var settings = new IntegrationTestEfDbSettings(transactionsEnabled);
        return new TestEfUnitOfWorkWrapper(context, new FakeServices(), settings);
    }

    #region Transaction Commit Tests

    /// <summary>Проверяет что SaveChangesAsync коммитит транзакцию при успехе.</summary>
    [Fact]
    public async Task SaveChangesAsync_OnSuccess_CommitsTransaction()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWorkWrapper(context, transactionsEnabled: true);

        var entity = CreateEntity(name: "commit-test");
        context.Entities.Add(entity);

        // Act
        var result = await uow.SaveChangesAsync(CancellationToken.None);

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
        var uow = new TestEfUnitOfWorkWrapper(
            context,
            new FakeServices(),
            settings,
            beforeSaveService);

        var entity = CreateEntity(name: "rollback-test");
        context.Entities.Add(entity);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => uow.SaveChangesAsync(CancellationToken.None));

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
        var uow = CreateUnitOfWorkWrapper(context, transactionsEnabled: true);

        // Act & Assert — after commit, ResetTransactionAsync creates a new transaction
        var act = () => uow.CommitTransactionAsync(CancellationToken.None);
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
        var uow = CreateUnitOfWorkWrapper(context, transactionsEnabled: true);

        // Act & Assert — after rollback, ResetTransactionAsync creates a new transaction
        var act = () => uow.RollbackTransactionAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Dispose Tests

    /// <summary>Проверяет что Dispose освобождает текущую транзакцию.</summary>
    [Fact]
    public void Dispose_DisposesCurrentTransaction()
    {
        // Arrange
        using var context = CreateContext();
        var uow = CreateUnitOfWorkWrapper(context, transactionsEnabled: true);

        uow.CurrentTransaction.Should().NotBeNull();

        // Act
        uow.Dispose();

        // Assert — after dispose, CurrentTransaction is explicitly set to null
        uow.CurrentTransaction.Should().BeNull();
    }

    #endregion

    #region Helpers

    private sealed class TestEfUnitOfWorkWrapper(
        IntegrationTestUnitOfWorkDbContext dbContext,
        IServiceProvider serviceProvider,
        EfDbSettingsBase<IntegrationTestUnitOfWorkDbContext> settings,
        IBeforeSaveChangesService? beforeSaveChangesService = default)
        : EfUnitOfWork<IntegrationTestUnitOfWorkDbContext>(
            dbContext,
            serviceProvider,
            settings,
            beforeSaveChangesService)
    {
        public bool IsTransactionEnabled => UseTransaction;

        public object? CurrentTransaction => CurrentDbTransaction;
    }

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
