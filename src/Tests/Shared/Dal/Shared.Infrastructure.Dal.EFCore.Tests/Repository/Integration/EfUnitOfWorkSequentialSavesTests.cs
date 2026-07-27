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
    /// Фабрика UoW для интеграционных тестов: создаёт экземпляр
    /// <see cref="EfUnitOfWork{TDbContext}"/> с реальной транзакцией SQLite.
    /// </summary>
    private static EfUnitOfWork<IntegrationTestUnitOfWorkDbContext> CreateUnitOfWork(
        IntegrationTestUnitOfWorkDbContext context,
        bool transactionsEnabled = true)
    {
        var settings = new IntegrationTestEfDbSettings(transactionsEnabled);
        return new EfUnitOfWork<IntegrationTestUnitOfWorkDbContext>(
            context,
            new EmptyServiceProvider(),
            settings,
            new LifecycleActionOrchestrator(
                [],
                new LifecycleEntityRegistry(),
                new LifecycleActionGate()));
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
        var uow = CreateUnitOfWork(context, transactionsEnabled: true);

        // Act 1
        context.Entities.Add(new TestEntityWithCreatedDeleted
        {
            Id = Guid.NewGuid(),
            Name = "first-commit",
        });
        var firstResult = await uow.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert 1: транзакция пересоздана в finally-блоке — повторный commit не бросает
        firstResult.Should().Be(1);
        var actCommit = () => uow.CommitTransactionAsync(TestContext.Current.CancellationToken);
        await actCommit.Should().NotThrowAsync(
            "после Commit в finally-блоке должна стартоваться свежая транзакция");

        // Act 2
        context.Entities.Add(new TestEntityWithCreatedDeleted
        {
            Id = Guid.NewGuid(),
            Name = "second-commit",
        });
        var secondResult = await uow.SaveChangesAsync(TestContext.Current.CancellationToken);

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
        var uow = CreateUnitOfWork(context, transactionsEnabled: true);

        // Act
        for (var i = 0; i < 3; i++)
        {
            context.Entities.Add(new TestEntityWithCreatedDeleted
            {
                Id = Guid.NewGuid(),
                Name = $"batch-{i}",
            });
            await uow.SaveChangesAsync(TestContext.Current.CancellationToken);

        var actCommit = () => uow.CommitTransactionAsync(TestContext.Current.CancellationToken);
            await actCommit.Should().NotThrowAsync(
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
        var uow = new EfUnitOfWork<IntegrationTestUnitOfWorkDbContext>(
            context,
            new EmptyServiceProvider(),
            settings,
            new LifecycleActionOrchestrator(
                [],
                new LifecycleEntityRegistry(),
                new LifecycleActionGate()),
            flakyService);

        // Act 1: первый коммит
        context.Entities.Add(new TestEntityWithCreatedDeleted
        {
            Id = Guid.NewGuid(),
            Name = "v1",
        });
        await uow.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act 2: UPDATE той же entity, BeforeSave бросает — rollback
        var tracked = await context.Entities.FirstAsync(TestContext.Current.CancellationToken);
        tracked.Name = "v2-doomed";
        var act2 = () => uow.SaveChangesAsync(TestContext.Current.CancellationToken);
        await act2.Should().ThrowAsync<InvalidOperationException>();

        // После rollback транзакция пересоздана — повторный commit не бросает
        var actAfterRollback = () => uow.CommitTransactionAsync(TestContext.Current.CancellationToken);
        await actAfterRollback.Should().NotThrowAsync();

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
        var thirdResult = await uow.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        thirdResult.Should().Be(1, "после rollback UoW готов к новой операции без ручного ClearTracking");
        var actAfterThird = () => uow.CommitTransactionAsync(TestContext.Current.CancellationToken);
        await actAfterThird.Should().NotThrowAsync();
    }

    #region Helpers

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
