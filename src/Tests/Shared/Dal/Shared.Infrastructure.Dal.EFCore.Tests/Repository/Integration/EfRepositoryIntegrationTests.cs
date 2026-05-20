using System.Linq.Expressions;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Dal.Specification.Interfaces;
using Shared.Infrastructure.Dal.EFCore.Repository;
using Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;
using Shared.Testing.Doubles.Mapping;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Repository.Integration;

/// <summary>
/// Интеграционные тесты для <see cref="EfRepository{TEntity}"/>,
/// использующие SQLite для поддержки транзакций, ExecuteUpdateAsync и других реляционных операций.
/// </summary>
[Trait("Category", "Integration")]
public sealed class EfRepositoryIntegrationTests
    : SqliteIntegrationTestBase
{
    private static EfRepository<TestEntityWithCreatedDeleted> CreateRepository(
        IntegrationTestDbContext context)
    {
        var evaluator = new EfQueryEvaluator(new FakeMapper());
        return new EfRepository<TestEntityWithCreatedDeleted>(context, evaluator);
    }

    private static TestEntityWithCreatedDeleted CreateEntity(
        Guid? id = null,
        string name = "test")
    {
        return new TestEntityWithCreatedDeleted
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
        };
    }

    #region Transaction Tests

    /// <summary>Проверяет что Execute с транзакцией фиксирует изменения при успехе.</summary>
    [Fact]
    public void Execute_WithTransaction_CommitsOnSuccess()
    {
        // Arrange
        using var context = CreateContext();
        var repo = CreateRepository(context);

        // Act
        var result = repo.Execute(() =>
        {
            context.Entities.Add(CreateEntity(name: "tx-added"));
            context.SaveChanges();
            return 42;
        }, useTransaction: true);

        // Assert
        result.Should().Be(42);
        context.Entities.Should().ContainSingle(e => e.Name == "tx-added");
    }

    /// <summary>Проверяет что Execute с транзакцией откатывает изменения при ошибке.</summary>
    [Fact]
    public void Execute_WithTransaction_RollbacksOnFailure()
    {
        // Arrange
        using var context = CreateContext();
        var repo = CreateRepository(context);

        // Act
        var act = () => repo.Execute<int>(() =>
        {
            context.Entities.Add(CreateEntity(name: "rolled-back"));
            context.SaveChanges();
            throw new InvalidOperationException("simulated failure");
        }, useTransaction: true);

        // Assert
        act.Should().Throw<InvalidOperationException>();
        context.Entities.Should().BeEmpty();
    }

    /// <summary>Проверяет что ExecuteAsync с транзакцией фиксирует изменения при успехе.</summary>
    [Fact]
    public async Task ExecuteAsync_WithTransaction_CommitsOnSuccess()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);

        // Act
        var result = await repo.ExecuteAsync(
            async () =>
            {
                context.Entities.Add(CreateEntity(name: "async-tx-added"));
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
                return 42;
            },
            useTransaction: true,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(42);
        context.Entities.Should().ContainSingle(e => e.Name == "async-tx-added");
    }

    /// <summary>Проверяет что ExecuteAsync с транзакцией откатывает изменения при ошибке.</summary>
    [Fact]
    public async Task ExecuteAsync_WithTransaction_RollbacksOnFailure()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);

        // Act
        var act = () => repo.ExecuteAsync<int>(
            async () =>
            {
                context.Entities.Add(CreateEntity(name: "async-rolled-back"));
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
                throw new InvalidOperationException("simulated async failure");
            },
            useTransaction: true,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        context.Entities.Should().BeEmpty();
    }

    #endregion

    #region RemoveRangeAsync by Options Tests

    /// <summary>Проверяет что RemoveRangeAsync по опциям удаляет подходящие сущности.</summary>
    [Fact]
    public async Task RemoveRangeAsync_ByOptions_RemovesMatchingEntities()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var removeId = Guid.NewGuid();
        var keepId = Guid.NewGuid();
        context.Entities.Add(CreateEntity(id: removeId, name: "remove"));
        context.Entities.Add(CreateEntity(id: keepId, name: "keep"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Detach to avoid tracking conflicts when RemoveRangeAsync loads entities
        context.ChangeTracker.Clear();

        var options = new QueryOptions<TestEntityWithCreatedDeleted>();
        options.AddFilter(e => e.Name == "remove");

        // Act
        await repo.RemoveRangeAsync(options, hard: true, TestContext.Current.CancellationToken);

        // Assert
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.Entities.Should().HaveCount(1);
        context.Entities.Single().Name.Should().Be("keep");
    }

    #endregion

    #region UpdateRangeAsync via ISpecification Tests

    /// <summary>
    /// Проверяет что UpdateRangeAsync(ISpecification, updateData) обновляет сущности,
    /// отобранные спецификацией.
    /// </summary>
    [Fact]
    public async Task UpdateRangeAsync_BySpecification_UpdatesMatchingEntities()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "spec-update"));
        context.Entities.Add(CreateEntity(name: "spec-keep"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        ISpecification<TestEntityWithCreatedDeleted> specification =
            new NameEqualsSpecification("spec-update");
        Expression<Func<TestEntityWithCreatedDeleted, string>> property = e => e.Name;
        Expression<Func<TestEntityWithCreatedDeleted, string>> value = e => "spec-updated";

        // Act
        await repo.UpdateRangeAsync(specification, (property, value));

        // Assert — ExecuteUpdateAsync updates directly, reload to verify
        context.ChangeTracker.Clear();
        var updated = context.Entities.Single(e => e.Name == "spec-updated");
        updated.Should().NotBeNull();
        context.Entities.Should().HaveCount(2);
        context.Entities.Should().Contain(e => e.Name == "spec-keep");
    }

    private sealed class NameEqualsSpecification(string name)
        : ISpecification<TestEntityWithCreatedDeleted>
    {
        public QueryOptions<TestEntityWithCreatedDeleted> BuildOptions()
        {
            var options = new QueryOptions<TestEntityWithCreatedDeleted>();
            options.AddFilter(e => e.Name == name);
            return options;
        }
    }

    #endregion

    #region UpdateRangeAsync Tests

    /// <summary>Проверяет что UpdateRangeAsync по условию обновляет подходящие сущности.</summary>
    [Fact]
    public async Task UpdateRangeAsync_ByCondition_UpdatesMatchingEntities()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "old1"));
        context.Entities.Add(CreateEntity(name: "old2"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Expression<Func<TestEntityWithCreatedDeleted, bool>> condition = e => e.Name.StartsWith("old");
        Expression<Func<TestEntityWithCreatedDeleted, string>> property = e => e.Name;
        Expression<Func<TestEntityWithCreatedDeleted, string>> value = e => "updated";

        // Act
        await repo.UpdateRangeAsync(condition, (property, value));

        // Assert — ExecuteUpdateAsync updates DB directly, reload to verify
        context.ChangeTracker.Clear();
        var updated = context.Entities.ToList();
        updated.Should().HaveCount(2);
        updated.Should().AllSatisfy(e => e.Name.Should().Be("updated"));
    }

    /// <summary>Проверяет что UpdateRangeAsync по опциям обновляет подходящие сущности.</summary>
    [Fact]
    public async Task UpdateRangeAsync_ByOptions_UpdatesMatchingEntities()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "update-me"));
        context.Entities.Add(CreateEntity(name: "keep"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var options = new QueryOptions<TestEntityWithCreatedDeleted>();
        options.AddFilter(e => e.Name == "update-me");
        Expression<Func<TestEntityWithCreatedDeleted, string>> property = e => e.Name;
        Expression<Func<TestEntityWithCreatedDeleted, string>> value = e => "updated";

        // Act
        await repo.UpdateRangeAsync(options, (property, value));

        // Assert — ExecuteUpdateAsync updates DB directly, reload to verify
        context.ChangeTracker.Clear();
        var updated = context.Entities.Single(e => e.Name == "updated");
        updated.Should().NotBeNull();
        context.Entities.Should().HaveCount(2);
    }

    #endregion
}
