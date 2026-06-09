// ----------------------------------------------------------------------------------------------
// <copyright file="EfUnitOfWorkSequentialSavesTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Shared.Application.Core.LifecycleAction;
using Shared.Infrastructure.Dal.EFCore.Interfaces;
using Shared.Infrastructure.Dal.EFCore.Settings;
using Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Repository.Integration;

/// <summary>
/// Интеграционные тесты W5: последовательные вызовы <c>SaveChangesAsync</c>
/// в одной инстанции <see cref="EfUnitOfWork{TDbContext}"/>. После
/// <c>Commit</c> следующий <c>SaveChanges</c> стартует свежую транзакцию.
/// </summary>
/// <remarks>
/// <para>
/// Реальный кейс: batch-импорт (несколько коммитов в одном HTTP-запросе
/// или фоновой джобе) требует, чтобы UoW не «зависал» после первого
/// commit-а, а был готов к следующей операции.
/// </para>
/// <para>
/// Использует SQLite — единственный поддерживаемый провайдер
/// для реальных транзакций в unit-test-окружении (InMemory не
/// поддерживает транзакции, имитация недостаточна для этой проверки).
/// </para>
/// </remarks>
[Trait("Category", "Integration")]
public sealed class EfUnitOfWorkSequentialSavesTests
    : SqliteUnitOfWorkIntegrationTestBase
{
    /// <summary>
    /// UoW с публичным доступом к <c>CurrentDbTransaction</c> для проверки,
    /// что транзакция пересоздаётся после каждого коммита.
    /// </summary>
    private sealed class TestUnitOfWork(
        IntegrationTestUnitOfWorkDbContext dbContext,
        IServiceProvider serviceProvider,
        EfDbSettingsBase<IntegrationTestUnitOfWorkDbContext> settings)
        : EfUnitOfWork<IntegrationTestUnitOfWorkDbContext>(
            dbContext,
            serviceProvider,
            settings,
            new LifecycleActionOrchestrator(
                [],
                new LifecycleEntityRegistry(),
                new LifecycleActionGate()))
    {
        public object? CurrentTransaction => CurrentDbTransaction;
    }

    /// <summary>
    /// W5: после первого успешного <c>SaveChangesAsync</c> (commit)
    /// следующий <c>SaveChangesAsync</c> в той же инстанции
    /// стартует свежую транзакцию, не падает и тоже коммитит.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_TwiceInSameSession_BothCommitsAndFreshTransaction()
    {
        // Arrange
        await using var context = CreateContext();
        var settings = new IntegrationTestEfDbSettings(transactionsEnabled: true);
        var uow = new TestUnitOfWork(context, new EmptyServiceProvider(), settings);

        // Act 1
        context.Entities.Add(new TestEntityWithCreatedDeleted
        {
            Id = Guid.NewGuid(),
            Name = "first-commit",
        });
        var firstResult = await uow.SaveChangesAsync(CancellationToken.None);

        // Assert 1: транзакция пересоздана в finally-блоке
        firstResult.Should().Be(1);
        uow.CurrentTransaction.Should().NotBeNull(
            "после Commit в finally-блоке должна стартовать свежая транзакция");

        // Act 2
        context.Entities.Add(new TestEntityWithCreatedDeleted
        {
            Id = Guid.NewGuid(),
            Name = "second-commit",
        });
        var secondResult = await uow.SaveChangesAsync(CancellationToken.None);

        // Assert 2: обе entity в БД
        secondResult.Should().Be(1);
        var persisted = await context.Entities
            .AsNoTracking()
            .Select(e => e.Name)
            .ToListAsync(TestContext.Current.CancellationToken);
        persisted.Should().BeEquivalentTo(
            new[] { "first-commit", "second-commit" });
    }

    /// <summary>
    /// W5 (расширенный): три последовательных коммита в одной сессии —
    /// пограничный случай для batch-импорта.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_ThreeSequentialCommits_AllPersisted()
    {
        // Arrange
        await using var context = CreateContext();
        var settings = new IntegrationTestEfDbSettings(transactionsEnabled: true);
        var uow = new TestUnitOfWork(context, new EmptyServiceProvider(), settings);

        // Act
        for (var i = 0; i < 3; i++)
        {
            context.Entities.Add(new TestEntityWithCreatedDeleted
            {
                Id = Guid.NewGuid(),
                Name = $"batch-{i}",
            });
            await uow.SaveChangesAsync(CancellationToken.None);

            uow.CurrentTransaction.Should().NotBeNull(
                $"после коммита #{i + 1} транзакция должна быть пересоздана");
        }

        // Assert
        var persisted = await context.Entities
            .AsNoTracking()
            .Select(e => e.Name)
            .ToListAsync(TestContext.Current.CancellationToken);
        persisted.Should().HaveCount(3);
    }

    /// <summary>
    /// W5 (recovery): после rollback-а в одном SaveChanges следующий
    /// SaveChanges в той же инстанции продолжает работать корректно.
    /// Используется <c>Update</c> (а не <c>Insert</c>), чтобы избежать
    /// EF Core нюанса с re-tracking Added-entities после rollback.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_CommitThenRollbackThenCommit_RecoversCorrectly()
    {
        // Arrange
        await using var context = CreateContext();
        var settings = new IntegrationTestEfDbSettings(transactionsEnabled: true);
        var flakyService = new FlakyBeforeSaveChangesService
        {
            ThrowOnCallIndex = 1,
        };
        var uow = new FlakyEfUnitOfWork(
            context,
            new EmptyServiceProvider(),
            settings,
            flakyService);

        // Act 1: первый коммит
        context.Entities.Add(new TestEntityWithCreatedDeleted
        {
            Id = Guid.NewGuid(),
            Name = "v1",
        });
        await uow.SaveChangesAsync(CancellationToken.None);

        // Act 2: UPDATE той же entity, BeforeSave бросает — rollback
        var tracked = await context.Entities.FirstAsync(TestContext.Current.CancellationToken);
        tracked.Name = "v2-doomed";
        var act2 = () => uow.SaveChangesAsync(CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(act2);

        // После rollback транзакция пересоздана
        uow.CurrentTransaction.Should().NotBeNull();

        var trackedAfterRollback = context.ChangeTracker
            .Entries<TestEntityWithCreatedDeleted>()
            .Count();
        trackedAfterRollback.Should().Be(0,
            "EfUnitOfWork.SaveChangesAsync автоматически очищает ChangeTracker при rollback");

        // Act 3: ещё один успешный коммит
        context.Entities.Add(new TestEntityWithCreatedDeleted
        {
            Id = Guid.NewGuid(),
            Name = "after-failure",
        });
        var thirdResult = await uow.SaveChangesAsync(CancellationToken.None);

        // Assert
        thirdResult.Should().Be(1, "после rollback UoW готов к новой операции без ручного ClearTracking");
        uow.CurrentTransaction.Should().NotBeNull();
    }

    #region Helpers

    /// <summary>
    /// Subclass <see cref="EfUnitOfWork{TDbContext}"/> с <c>IBeforeSaveChangesService</c>
    /// для сценария commit → rollback → commit.
    /// </summary>
    private sealed class FlakyEfUnitOfWork(
        IntegrationTestUnitOfWorkDbContext dbContext,
        IServiceProvider serviceProvider,
        EfDbSettingsBase<IntegrationTestUnitOfWorkDbContext> settings,
        IBeforeSaveChangesService beforeSaveChangesService)
        : EfUnitOfWork<IntegrationTestUnitOfWorkDbContext>(
            dbContext,
            serviceProvider,
            settings,
            new LifecycleActionOrchestrator(
                [],
                new LifecycleEntityRegistry(),
                new LifecycleActionGate()),
            beforeSaveChangesService)
    {
        public object? CurrentTransaction => CurrentDbTransaction;
    }

    private sealed class IntegrationTestEfDbSettings
        : EfDbSettingsBase<IntegrationTestUnitOfWorkDbContext>
    {
        [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
        public IntegrationTestEfDbSettings(bool transactionsEnabled = true)
        {
            ConnectionString = "DataSource=:memory:";
            TransactionsEnabled = transactionsEnabled;
        }
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    /// <summary>
    /// Before-save сервис, бросающий исключение на указанном индексе вызова.
    /// </summary>
    private sealed class FlakyBeforeSaveChangesService
        : IBeforeSaveChangesService
    {
        private int _callIndex;

        /// <summary>
        /// 0-based индекс вызова, на котором бросить исключение.
        /// </summary>
        public int ThrowOnCallIndex { get; init; }

        public Task ProcessAsync(
            DbContext dbContext,
            CancellationToken cancellationToken = default)
        {
            var current = _callIndex++;
            if (current == ThrowOnCallIndex)
            {
                throw new InvalidOperationException(
                    $"FlakyBeforeSaveChangesService: simulated failure on call #{current}");
            }

            return Task.CompletedTask;
        }

        public void Process(DbContext dbContext)
        {
            // Sync overload вызывается только из legacy-кода, здесь не нужен.
        }
    }

    #endregion
}
