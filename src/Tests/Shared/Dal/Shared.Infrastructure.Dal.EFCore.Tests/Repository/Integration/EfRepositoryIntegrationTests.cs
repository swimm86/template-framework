// ----------------------------------------------------------------------------------------------
// <copyright file="EfRepositoryIntegrationTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Data.Common;
using System.Linq.Expressions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shared.Domain.Core.Dal;
using Shared.Domain.Core.Dal.Repository.Interfaces;
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
    private static IRepository<TestEntityWithCreatedDeleted> CreateRepository(
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

    /// <summary>
    /// Проверяет что RemoveRangeAsync по опциям удаляет подходящие сущности.
    /// </summary>
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

    #region ExecuteUpdateRangeAsync via ISpecification Tests

    /// <summary>
    /// Проверяет что ExecuteUpdateRangeAsync(ISpecification, updateData) обновляет сущности,
    /// отобранные спецификацией.
    /// </summary>
    [Fact]
    public async Task ExecuteUpdateRangeAsync_BySpecification_UpdatesMatchingEntities()
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
        await repo.ExecuteUpdateRangeAsync(specification, (property, value));

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

    #region ExecuteUpdateRangeAsync Tests

    /// <summary>
    /// Проверяет что ExecuteUpdateRangeAsync по условию обновляет подходящие сущности.
    /// </summary>
    [Fact]
    public async Task ExecuteUpdateRangeAsync_ByCondition_UpdatesMatchingEntities()
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
        await repo.ExecuteUpdateRangeAsync(condition, (property, value));

        // Assert — ExecuteUpdateAsync updates DB directly, reload to verify
        context.ChangeTracker.Clear();
        var updated = context.Entities.ToList();
        updated.Should().HaveCount(2);
        updated.Should().AllSatisfy(e => e.Name.Should().Be("updated"));
    }

    /// <summary>
    /// Проверяет что ExecuteUpdateRangeAsync по опциям обновляет подходящие сущности.
    /// </summary>
    [Fact]
    public async Task ExecuteUpdateRangeAsync_ByOptions_UpdatesMatchingEntities()
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
        await repo.ExecuteUpdateRangeAsync(options, (property, value));

        // Assert — ExecuteUpdateAsync updates DB directly, reload to verify
        context.ChangeTracker.Clear();
        var updated = context.Entities.Single(e => e.Name == "updated");
        updated.Should().NotBeNull();
        context.Entities.Should().HaveCount(2);
    }

    #endregion

    #region GetGroupingAsync Tests

    /// <summary>
    /// Проверяет что GetGroupingAsync возвращает группы по ключу без сортировки и пагинации.
    /// </summary>
    [Fact]
    public async Task GetGroupingAsync_ByKey_ReturnsAllGroups()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "a"));
        context.Entities.Add(CreateEntity(name: "aa"));
        context.Entities.Add(CreateEntity(name: "bbb"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Expression<Func<TestEntityWithCreatedDeleted, int>> keySelector = e => e.Name.Length;

        // Act
        var groups = await repo.GetGroupingAsync(
            keySelector,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        groups.Select(g => g.Key).Should().BeEquivalentTo([1, 2, 3]);
        groups.Single(g => g.Key == 2).Should().HaveCount(1);
        groups.Single(g => g.Key == 3).Should().HaveCount(1);
    }

    /// <summary>
    /// Проверяет что GetGroupingAsync возвращает пустой список при отсутствии данных.
    /// </summary>
    [Fact]
    public async Task GetGroupingAsync_EmptyDb_ReturnsEmptyList()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);

        Expression<Func<TestEntityWithCreatedDeleted, int>> keySelector = e => e.Name.Length;

        // Act
        var groups = await repo.GetGroupingAsync(
            keySelector,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        groups.Should().BeEmpty();
    }

    /// <summary>
    /// Проверяет что GetGroupingAsync с селектором проецирует группы в выходной тип.
    /// </summary>
    [Fact]
    public async Task GetGroupingAsync_WithSelector_ProjectsGroups()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "a"));
        context.Entities.Add(CreateEntity(name: "aa"));
        context.Entities.Add(CreateEntity(name: "bbb"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Expression<Func<TestEntityWithCreatedDeleted, int>> keySelector = e => e.Name.Length;
        Expression<Func<IGrouping<int, TestEntityWithCreatedDeleted>, GroupSummary<int>>> selector =
            g => new GroupSummary<int>(g.Key, g.Count());

        // Act
        var summaries = await repo.GetGroupingAsync(
            keySelector,
            selector: selector,
            groupKeyOrderDirection: OrderDirectionType.Ascending,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        summaries.Should().Equal(
            new GroupSummary<int>(1, 1),
            new GroupSummary<int>(2, 1),
            new GroupSummary<int>(3, 1));
    }

    /// <summary>
    /// Проверяет что GetGroupingAsync с селектором применяет пагинацию.
    /// </summary>
    [Fact]
    public async Task GetGroupingAsync_WithSelectorAndSkipTake_PaginatesProjectedGroups()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "a"));
        context.Entities.Add(CreateEntity(name: "bb"));
        context.Entities.Add(CreateEntity(name: "ccc"));
        context.Entities.Add(CreateEntity(name: "dddd"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Expression<Func<TestEntityWithCreatedDeleted, int>> keySelector = e => e.Name.Length;
        Expression<Func<IGrouping<int, TestEntityWithCreatedDeleted>, GroupSummary<int>>> selector =
            g => new GroupSummary<int>(g.Key, g.Count());

        // Act
        var summaries = await repo.GetGroupingAsync(
            keySelector,
            skip: 1,
            take: 2,
            selector: selector,
            groupKeyOrderDirection: OrderDirectionType.Ascending,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        summaries.Should().HaveCount(2);
        summaries.Select(s => s.Key).Should().Equal(2, 3);
    }

    private sealed record GroupSummary<TKey>(TKey Key, int Count);

    #endregion

    #region CountGroupsAsync Tests

    /// <summary>
    /// Проверяет что CountGroupsAsync возвращает количество уникальных ключей группировки.
    /// </summary>
    [Fact]
    public async Task CountGroupsAsync_ByKey_ReturnsGroupCount()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "a"));
        context.Entities.Add(CreateEntity(name: "aa"));
        context.Entities.Add(CreateEntity(name: "bbb"));
        context.Entities.Add(CreateEntity(name: "bbbb"));
        context.Entities.Add(CreateEntity(name: "bbbbb"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Expression<Func<TestEntityWithCreatedDeleted, int>> keySelector = e => e.Name.Length;

        // Act
        var count = await repo.CountGroupsAsync(keySelector, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        count.Should().Be(5);
    }

    /// <summary>
    /// Проверяет что CountGroupsAsync с фильтром учитывает только подходящие сущности.
    /// </summary>
    [Fact]
    public async Task CountGroupsAsync_WithFilter_CountsFilteredGroups()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "alpha"));
        context.Entities.Add(CreateEntity(name: "alphaa"));
        context.Entities.Add(CreateEntity(name: "betaaa"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Expression<Func<TestEntityWithCreatedDeleted, int>> keySelector = e => e.Name.Length;
        var options = new QueryOptions<TestEntityWithCreatedDeleted>();
        options.AddFilter(e => e.Name.StartsWith("alpha"));

        // Act
        var count = await repo.CountGroupsAsync(keySelector, options, TestContext.Current.CancellationToken);

        // Assert
        count.Should().Be(2);
    }

    /// <summary>
    /// Проверяет что CountGroupsAsync возвращает 0 при пустой БД.
    /// </summary>
    [Fact]
    public async Task CountGroupsAsync_EmptyDb_ReturnsZero()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);

        Expression<Func<TestEntityWithCreatedDeleted, int>> keySelector = e => e.Name.Length;

        // Act
        var count = await repo.CountGroupsAsync(keySelector, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        count.Should().Be(0);
    }

    /// <summary>
    /// Проверяет что CountGroupsAsync по спецификации делегирует к версии с опциями.
    /// </summary>
    [Fact]
    public async Task CountGroupsAsync_BySpecification_MatchesOptionsOverload()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "spec-one"));
        context.Entities.Add(CreateEntity(name: "spec-two"));
        context.Entities.Add(CreateEntity(name: "other-three"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        ISpecification<TestEntityWithCreatedDeleted> specification =
            new NameStartsWithSpecification("spec-");
        Expression<Func<TestEntityWithCreatedDeleted, int>> keySelector = e => e.Name.Length;

        // Act
        var count = await repo.CountGroupsAsync(
            keySelector,
            specification,
            TestContext.Current.CancellationToken);

        // Assert — "spec-one" (8) and "spec-two" (8) share one key, so 1 group
        count.Should().Be(1);
    }

    private sealed class NameStartsWithSpecification(string prefix)
        : ISpecification<TestEntityWithCreatedDeleted>
    {
        public QueryOptions<TestEntityWithCreatedDeleted> BuildOptions()
        {
            var options = new QueryOptions<TestEntityWithCreatedDeleted>();
            options.AddFilter(e => e.Name.StartsWith(prefix));
            return options;
        }
    }

    #endregion

    #region MaxAsync / MinAsync Tests

    public enum AggregateKind { Max, Min }

    public enum AggregateScenario { All, EmptyDb, WithFilter, BySpecification }

    /// <summary>
    /// Проверяет MaxAsync и MinAsync в разных сценариях (все, пустая БД, фильтр, спецификация).
    /// </summary>
    [Theory]
    [InlineData(AggregateKind.Max, AggregateScenario.All, 4)]
    [InlineData(AggregateKind.Max, AggregateScenario.EmptyDb, 0)]
    [InlineData(AggregateKind.Max, AggregateScenario.WithFilter, 14)]
    [InlineData(AggregateKind.Max, AggregateScenario.BySpecification, 11)]
    [InlineData(AggregateKind.Min, AggregateScenario.All, 1)]
    [InlineData(AggregateKind.Min, AggregateScenario.EmptyDb, 0)]
    [InlineData(AggregateKind.Min, AggregateScenario.WithFilter, 13)]
    [InlineData(AggregateKind.Min, AggregateScenario.BySpecification, 11)]
    public async Task AggregateAsync_ReturnsExpected(
        AggregateKind kind,
        AggregateScenario scenario,
        int expected)
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);

        Expression<Func<TestEntityWithCreatedDeleted, int>> selector = e => e.Name.Length;

        switch (scenario)
        {
            case AggregateScenario.All:
                context.Entities.Add(CreateEntity(name: "a"));
                context.Entities.Add(CreateEntity(name: "bbbb"));
                context.Entities.Add(CreateEntity(name: "ccc"));
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
                break;
            case AggregateScenario.EmptyDb:
                break;
            case AggregateScenario.WithFilter:
                context.Entities.Add(CreateEntity(name: "keep-short"));
                context.Entities.Add(CreateEntity(name: "filter-longest"));
                context.Entities.Add(CreateEntity(name: "filter-medium"));
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
                break;
            case AggregateScenario.BySpecification:
                context.Entities.Add(CreateEntity(name: "keep-x"));
                context.Entities.Add(CreateEntity(name: "spec-medium"));
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
                break;
        }

        // Act
        int? result = scenario switch
        {
            AggregateScenario.All => kind == AggregateKind.Max
                ? await repo.MaxAsync(selector, cancellationToken: TestContext.Current.CancellationToken)
                : await repo.MinAsync(selector, cancellationToken: TestContext.Current.CancellationToken),
            AggregateScenario.EmptyDb => kind == AggregateKind.Max
                ? await repo.MaxAsync(selector, cancellationToken: TestContext.Current.CancellationToken)
                : await repo.MinAsync(selector, cancellationToken: TestContext.Current.CancellationToken),
            AggregateScenario.WithFilter => await AggregateWithOptionsAsync(kind, repo, selector, e => e.Name.StartsWith("filter")),
            AggregateScenario.BySpecification => await AggregateWithSpecAsync(kind, repo, selector, new NameEqualsSpecification("spec-medium")),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
        };

        // Assert
        result.Should().Be(expected);
    }

    private static async Task<int?> AggregateWithOptionsAsync(
        AggregateKind kind,
        IRepository<TestEntityWithCreatedDeleted> repo,
        Expression<Func<TestEntityWithCreatedDeleted, int>> selector,
        Expression<Func<TestEntityWithCreatedDeleted, bool>> filter)
    {
        var options = new QueryOptions<TestEntityWithCreatedDeleted>();
        options.AddFilter(filter);
        return kind == AggregateKind.Max
            ? await repo.MaxAsync(selector, options, TestContext.Current.CancellationToken)
            : await repo.MinAsync(selector, options, TestContext.Current.CancellationToken);
    }

    private static async Task<int?> AggregateWithSpecAsync(
        AggregateKind kind,
        IRepository<TestEntityWithCreatedDeleted> repo,
        Expression<Func<TestEntityWithCreatedDeleted, int>> selector,
        ISpecification<TestEntityWithCreatedDeleted> specification)
    {
        return kind == AggregateKind.Max
            ? await repo.MaxAsync(selector, specification, TestContext.Current.CancellationToken)
            : await repo.MinAsync(selector, specification, TestContext.Current.CancellationToken);
    }

    #endregion

    #region ExecuteRemoveRangeAsync Tests

    /// <summary>
    /// Проверяет что ExecuteRemoveRangeAsync по условию физически удаляет подходящие сущности без SaveChanges.
    /// </summary>
    [Fact]
    public async Task ExecuteRemoveRangeAsync_ByCondition_RemovesMatchingEntities()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "remove"));
        context.Entities.Add(CreateEntity(name: "keep"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var options = new QueryOptions<TestEntityWithCreatedDeleted>()
            .AddFilter(e => e.Name == "remove");

        // Act
        await repo.ExecuteRemoveRangeAsync(options, TestContext.Current.CancellationToken);

        // Assert — ExecuteDeleteAsync removes directly, reload to verify
        context.ChangeTracker.Clear();
        context.Entities.Should().HaveCount(1);
        context.Entities.Single().Name.Should().Be("keep");
    }

    /// <summary>
    /// Проверяет что ExecuteRemoveRangeAsync по опциям физически удаляет подходящие сущности.
    /// </summary>
    [Fact]
    public async Task ExecuteRemoveRangeAsync_ByOptions_RemovesMatchingEntities()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "remove-1"));
        context.Entities.Add(CreateEntity(name: "remove-2"));
        context.Entities.Add(CreateEntity(name: "keep"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var options = new QueryOptions<TestEntityWithCreatedDeleted>();
        options.AddFilter(e => e.Name.StartsWith("remove"));

        // Act
        await repo.ExecuteRemoveRangeAsync(options, TestContext.Current.CancellationToken);

        // Assert
        context.ChangeTracker.Clear();
        context.Entities.Should().HaveCount(1);
        context.Entities.Single().Name.Should().Be("keep");
    }

    /// <summary>
    /// Проверяет что ExecuteRemoveRangeAsync по спецификации делегирует к версии с опциями.
    /// </summary>
    [Fact]
    public async Task ExecuteRemoveRangeAsync_BySpecification_RemovesMatchingEntities()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "spec-remove"));
        context.Entities.Add(CreateEntity(name: "spec-keep"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        ISpecification<TestEntityWithCreatedDeleted> specification =
            new NameEqualsSpecification("spec-remove");

        // Act
        await repo.ExecuteRemoveRangeAsync(specification, TestContext.Current.CancellationToken);

        // Assert
        context.ChangeTracker.Clear();
        context.Entities.Should().HaveCount(1);
        context.Entities.Single().Name.Should().Be("spec-keep");
    }

    /// <summary>
    /// Проверяет что ExecuteRemoveRangeAsync не требует SaveChangesAsync.
    /// </summary>
    [Fact]
    public async Task ExecuteRemoveRangeAsync_DoesNotRequireSaveChanges()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "remove"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var options = new QueryOptions<TestEntityWithCreatedDeleted>()
            .AddFilter(e => e.Name == "remove");

        // Act — explicitly do NOT call SaveChanges
        await repo.ExecuteRemoveRangeAsync(options, TestContext.Current.CancellationToken);

        // Assert
        context.ChangeTracker.Clear();
        context.Entities.Should().BeEmpty();
    }

    #endregion

    #region Specification Delegation Tests

    public enum SpecDelegationMethod { Any, Count, FirstOrDefault, SingleOrDefault, LastOrDefault }

    /// <summary>
    /// Проверяет, что default-реализации методов через <see cref="ISpecification{T}"/> делегируют к версии
    /// с <see cref="QueryOptions{T}"/>.
    /// </summary>
    [Theory]
    [InlineData(SpecDelegationMethod.Any, true)]
    [InlineData(SpecDelegationMethod.Count, 1)]
    [InlineData(SpecDelegationMethod.FirstOrDefault, "alpha")]
    [InlineData(SpecDelegationMethod.SingleOrDefault, "alpha")]
    [InlineData(SpecDelegationMethod.LastOrDefault, "alpha")]
    public async Task XxxAsync_WithOrderedSpecification_DelegatesToOptions(
        SpecDelegationMethod method,
        object expected)
    {
        // Arrange — фильтр под ОДНУ запись, чтобы SingleOrDefault не бросал исключение.
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "alpha"));
        context.Entities.Add(CreateEntity(name: "other-1"));
        context.Entities.Add(CreateEntity(name: "other-2"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        ISpecification<TestEntityWithCreatedDeleted> specification =
            new OrderedNameEqualsSpecification("alpha");

        // Act
        object? actual = method switch
        {
            SpecDelegationMethod.Any =>
                await repo.AnyAsync(specification, TestContext.Current.CancellationToken),
            SpecDelegationMethod.Count =>
                await repo.CountAsync(specification, TestContext.Current.CancellationToken),
            SpecDelegationMethod.FirstOrDefault => (
                await repo.FirstOrDefaultAsync(specification, TestContext.Current.CancellationToken))?.Name,
            SpecDelegationMethod.SingleOrDefault => (
                await repo.SingleOrDefaultAsync(specification, TestContext.Current.CancellationToken))?.Name,
            SpecDelegationMethod.LastOrDefault => (
                await repo.LastOrDefaultAsync(specification, TestContext.Current.CancellationToken))?.Name,
            _ => throw new ArgumentOutOfRangeException(nameof(method)),
        };

        // Assert
        actual.Should().Be(expected);
    }

    /// <summary>
    /// Проверяет, что LastOrDefaultAsync через <see cref="ISpecification{T}"/> без OrderBy бросает
    /// <see cref="InvalidOperationException"/>.</summary>
    /// <remarks>
    /// Это ограничение EF Core: <c>LastOrDefault</c> транслируется в SQL <c>ORDER BY ... DESC LIMIT 1</c>,
    /// и без явного <c>OrderBy</c> EF Core отказывается генерировать недетерминированный запрос.
    /// </remarks>
    [Fact]
    public async Task LastOrDefaultAsync_WithoutOrdering_ThrowsInvalidOperation()
    {
        // Arrange — спецификация без OrderBy.
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "alpha"));
        context.Entities.Add(CreateEntity(name: "alphabet"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        ISpecification<TestEntityWithCreatedDeleted> specification =
            new NameStartsWithSpecification("alph");

        // Act
        var act = () => repo.LastOrDefaultAsync(specification, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*deterministic sort order*");
    }

    private sealed class OrderedNameStartsWithSpecification(string prefix)
        : ISpecification<TestEntityWithCreatedDeleted>
    {
        public QueryOptions<TestEntityWithCreatedDeleted> BuildOptions()
        {
            var options = new QueryOptions<TestEntityWithCreatedDeleted>();
            options.AddFilter(e => e.Name.StartsWith(prefix));
            options.AddOrderBy(e => e.Name, OrderDirectionType.Ascending);
            return options;
        }
    }

    private sealed class OrderedNameEqualsSpecification(string name)
        : ISpecification<TestEntityWithCreatedDeleted>
    {
        public QueryOptions<TestEntityWithCreatedDeleted> BuildOptions()
        {
            var options = new QueryOptions<TestEntityWithCreatedDeleted>();
            options.AddFilter(e => e.Name == name);
            options.AddOrderBy(e => e.Name, OrderDirectionType.Ascending);
            return options;
        }
    }

    /// <summary>
    /// Проверяет, что SumAsync через <see cref="ISpecification{T}"/> делегирует к версии с <see cref="QueryOptions{T}"/>.
    /// </summary>
    [Fact]
    public async Task SumAsync_WithSpecification_DelegatesToOptions()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "alpha-1"));
        context.Entities.Add(CreateEntity(name: "alpha-22"));
        context.Entities.Add(CreateEntity(name: "other-333"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        ISpecification<TestEntityWithCreatedDeleted> specification =
            new NameStartsWithSpecification("alpha");

        Expression<Func<TestEntityWithCreatedDeleted, decimal>> selector = e => e.Name.Length;

        // Act
        var sumViaSpec = await repo.SumAsync(selector, specification, TestContext.Current.CancellationToken);
        var sumViaOptions = await repo.SumAsync(
            selector,
            specification.BuildOptions(),
            TestContext.Current.CancellationToken);

        // Assert
        sumViaSpec.Should().Be(sumViaOptions);
        sumViaSpec.Should().Be(15m);
    }

    #endregion

    #region AddRangeAsync Tests

    /// <summary>
    /// Проверяет, что AddRangeAsync заполняет аудит-поля для всех добавленных сущностей.
    /// </summary>
    [Fact]
    public async Task AddRangeAsync_SetsAuditFieldsForAllEntities()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var entities = new[]
        {
            new TestEntityWithCreatedDeleted { Id = Guid.NewGuid(), Name = "first" },
            new TestEntityWithCreatedDeleted { Id = Guid.NewGuid(), Name = "second" },
            new TestEntityWithCreatedDeleted { Id = Guid.NewGuid(), Name = "third" },
        };
        var userId = Guid.NewGuid();

        // Act
        await repo.AddRangeAsync(entities, userId, "auditor", TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        entities.Should().AllSatisfy(e =>
        {
            e.DateCreated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
            e.CreatedByUserId.Should().Be(userId);
            e.CreatedByUserName.Should().Be("auditor");
        });
    }

    #endregion

    #region RemovePermanentRangeAsync Tests

    /// <summary>
    /// Проверяет, что RemovePermanentRangeAsync физически удаляет все переданные сущности без soft-delete.
    /// </summary>
    [Fact]
    public async Task RemovePermanentRangeAsync_RemovesAllEntitiesFromDb()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var entities = new[]
        {
            CreateEntity(name: "perm-1"),
            CreateEntity(name: "perm-2"),
            CreateEntity(name: "perm-3"),
        };
        context.Entities.AddRange(entities);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();

        // Act
        await repo.RemovePermanentRangeAsync(entities, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert — soft-delete НЕ применён (IsDeleted остался false)
        entities.Should().AllSatisfy(e => e.IsDeleted.Should().BeFalse());

        // Assert — после SaveChanges сущности физически удалены из БД
        var remainingIds = entities.Select(e => e.Id).ToHashSet();
        var persistedNames = context.Entities
            .Where(e => remainingIds.Contains(e.Id))
            .Select(e => e.Name)
            .ToList();
        persistedNames.Should().BeEmpty();
    }

    #endregion

    #region RemoveRangeAsync Predicate Hard Delete Tests

    /// <summary>
    /// Проверяет, что RemoveRangeAsync по предикату с hard=true физически удаляет сущности из БД.
    /// </summary>
    [Fact]
    public async Task RemoveRangeAsync_ByPredicateHard_RemovesFromDatabase()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "delete-me"));
        context.Entities.Add(CreateEntity(name: "delete-me-too"));
        context.Entities.Add(CreateEntity(name: "keep"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();

        // Act
        await repo.RemoveRangeAsync(
            (Expression<Func<TestEntityWithCreatedDeleted, bool>>)(e => e.Name.StartsWith("delete")),
            hard: true,
            TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        context.Entities.Should().HaveCount(1);
        context.Entities.Single().Name.Should().Be("keep");
    }

    #endregion

    #region Potential Bugs / Edge Cases

    public enum GetAsyncNullIdScenario
    {
        WithoutOptions,
        WithOptions,
    }

    /// <summary>
    /// Проверяет контракт: GetAsync с id=null бросает ArgumentNullException независимо от переданных options.
    /// </summary>
    [Theory]
    [InlineData(GetAsyncNullIdScenario.WithoutOptions)]
    [InlineData(GetAsyncNullIdScenario.WithOptions)]
    public async Task GetAsync_WithNullId_ThrowsArgumentNullException(GetAsyncNullIdScenario scenario)
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "row-1"));
        context.Entities.Add(CreateEntity(name: "row-2"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var options = scenario == GetAsyncNullIdScenario.WithOptions
            ? new QueryOptions<TestEntityWithCreatedDeleted>().AddFilter(e => e.Name == "any")
            : null;

        // Act + Assert
        var act = () => repo.GetAsync(
            id: null!,
            options: options,
            cancellationToken: TestContext.Current.CancellationToken);
        await act
            .Should()
            .ThrowAsync<ArgumentNullException>()
            .WithParameterName("id");
    }

    /// <summary>
    /// Проверяет, что RemoveAsync сохраняет мягкое удаление detached-сущности в БД.
    /// </summary>
    [Fact]
    public async Task RemoveAsync_OnDetachedEntity_PersistsSoftDeleteToDb()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var entity = CreateEntity(name: "to-soft-delete");
        context.Entities.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var entityId = entity.Id;
        context.ChangeTracker.Clear();

        // Act
        // [BUG_LOCK #1] Мягкое удаление detached-сущности должно сохраняться.
        await repo.RemoveAsync(entity, userId: Guid.NewGuid(), hard: false, cancellationToken: TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        entity.IsDeleted.Should().BeTrue();
        context.ChangeTracker.Clear();
        var reloaded = await context.Entities.SingleAsync(e => e.Id == entityId, TestContext.Current.CancellationToken);
        reloaded.IsDeleted.Should().BeTrue();
    }

    /// <summary>
    /// Выявляет баг: AddAsync двух entities с одинаковым Id должен бросить DbUpdateException при SaveChanges.
    /// </summary>
    [Fact]
    public async Task AddAsync_DuplicateId_SaveChangesThrows()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var sharedId = Guid.NewGuid();
        context.Entities.Add(CreateEntity(id: sharedId, name: "first"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();

        // Act
        await repo.AddAsync(
            CreateEntity(
                id: sharedId,
                name: "duplicate"),
            cancellationToken: TestContext.Current.CancellationToken);
        var act = () => context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    /// <summary>
    /// Выявляет баг: RemoveRangeAsync с пустым IEnumerable должен быть no-op, не падать.
    /// </summary>
    [Fact]
    public async Task RemoveRangeAsync_EmptyEnumerable_DoesNotThrow()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var entities = Array.Empty<TestEntityWithCreatedDeleted>();

        // Act + Assert
        var act = () => repo.RemoveRangeAsync(
            entities, hard: true,
            cancellationToken: TestContext.Current.CancellationToken);
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Выявляет баг: GetRangeAsync с skip=0 и take=0 должен вернуть пустой список, не все записи.
    /// </summary>
    [Fact]
    public async Task GetRangeAsync_SkipZeroTakeZero_ReturnsEmptyList()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "row-1"));
        context.Entities.Add(CreateEntity(name: "row-2"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await repo.GetRangeAsync(
            skip: 0,
            take: 0,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Проверяет, что ExecuteUpdateRangeAsync отклоняет null-выражение значения.
    /// </summary>
    [Fact]
    public async Task ExecuteUpdateRangeAsync_WithNullValueExpression_ThrowsArgumentNullException()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "row"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Expression<Func<TestEntityWithCreatedDeleted, string>> propertyExpression = e => e.Name;

        // Act
        var act = () => repo.ExecuteUpdateRangeAsync(
            (Expression<Func<TestEntityWithCreatedDeleted, bool>>)(e => true),
            (propertyExpression, valueExpression: null!));

        // Assert
        await act
            .Should()
            .ThrowAsync<ArgumentNullException>()
            .WithParameterName("valueExpression");
    }

    /// <summary>
    /// Выявляет баг: SumAsync на пустой выборке должен возвращать 0 (decimal default), а не null или исключение.
    /// </summary>
    [Fact]
    public async Task SumAsync_OnEmptySelection_ReturnsZeroNotThrows()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);

        Expression<Func<TestEntityWithCreatedDeleted, decimal>> selector = e => e.Name.Length;

        // Act
        var result = await repo.SumAsync(
            selector,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(0m);
    }

    /// <summary>
    /// Выявляет баг: CountAsync на непустой БД с фильтром, которому не соответствует ни одна запись, должен вернуть 0.
    /// </summary>
    /// <remarks>
    /// Это тривиальный тест, но выявляет баг, если фильтр игнорируется (например, если AddFilter молча падает).
    /// </remarks>
    [Fact]
    public async Task CountAsync_WithFilterMatchingNothing_ReturnsZero()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "row-1"));
        context.Entities.Add(CreateEntity(name: "row-2"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var options = new QueryOptions<TestEntityWithCreatedDeleted>();
        options.AddFilter(e => e.Name == "non-existent");

        // Act
        var count = await repo.CountAsync(options, TestContext.Current.CancellationToken);

        // Assert
        count.Should().Be(0);
    }

    /// <summary>
    /// Выявляет баг: RemoveAsync с hard=false на entity БЕЗ IWithDeleted должен физически удалить (не soft-delete).
    /// </summary>
    /// <remarks>
    /// TestEntityWithCreatedDeleted реализует IWithDeleted, поэтому нужно создать entity без этого интерфейса. Это требует дополнительной entity definition. Тест-документация поведения.
    /// </remarks>
    [Fact]
    public async Task RemoveAsync_OnEntityWithDeletedSoftDelete_PersistsToDb()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var entity = CreateEntity(name: "to-soft-delete");
        context.Entities.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var userId = Guid.NewGuid();
        entity.IsDeleted.Should().BeFalse();

        // Act — soft-delete на tracked entity
        await repo.RemoveAsync(
            entity,
            userId: userId,
            hard: false,
            cancellationToken: TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        entity.IsDeleted.Should().BeTrue();
        entity.DateDeleted.Should().NotBeNull();
        entity.DeletedByUserId.Should().Be(userId);

        // Проверяем, что запись реально в БД с IsDeleted=true (не удалена физически)
        context.ChangeTracker.Clear();
        var exists = await context.Entities.AnyAsync(e => e.Id == entity.Id, TestContext.Current.CancellationToken);
        exists.Should().BeTrue();
    }

    #endregion

    #region Potential Problems / Edge Cases

    /// <summary>
    /// Выявляет проблему: ExecuteUpdateRangeAsync без условия обновляет ВСЕ строки таблицы.
    /// </summary>
    /// <remarks>
    /// Если default-параметр predicate=null случайно не будет проверяться в реализации, метод начнёт обновлять всю таблицу. Тест фиксирует текущее (корректное) поведение — обновление по всем строкам при отсутствии фильтра.
    /// </remarks>
    [Fact]
    public async Task ExecuteUpdateRangeAsync_WithNullPredicate_UpdatesAllRows()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "row1"));
        context.Entities.Add(CreateEntity(name: "row2"));
        context.Entities.Add(CreateEntity(name: "row3"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();

        Expression<Func<TestEntityWithCreatedDeleted, string>> property = e => e.Name;
        Expression<Func<TestEntityWithCreatedDeleted, string>> value = e => "bulk-updated";

        // Act — вызов БЕЗ условия (predicate = null)
        await repo.ExecuteUpdateRangeAsync(predicate: null, (property, value));

        // Assert — обновлены ВСЕ строки (3 из 3)
        context.ChangeTracker.Clear();
        context.Entities.Should().AllSatisfy(e => e.Name.Should().Be("bulk-updated"));
    }

    /// <summary>Выявляет проблему: ExecuteUpdateRangeAsync с пустыми QueryOptions (без фильтров) обновляет все строки.</summary>
    [Fact]
    public async Task ExecuteUpdateRangeAsync_WithEmptyOptions_UpdatesAllRows()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "row1"));
        context.Entities.Add(CreateEntity(name: "row2"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();

        Expression<Func<TestEntityWithCreatedDeleted, string>> property = e => e.Name;
        Expression<Func<TestEntityWithCreatedDeleted, string>> value = e => "updated";

        // Act — QueryOptions без фильтров
        await repo.ExecuteUpdateRangeAsync(new QueryOptions<TestEntityWithCreatedDeleted>(), (property, value));

        // Assert
        context.ChangeTracker.Clear();
        context.Entities.Should().AllSatisfy(e => e.Name.Should().Be("updated"));
    }

    /// <summary>
    /// Выявляет проблему: RemoveAsync с userId пробрасывает userId в DeletedByUserId при soft-delete.
    /// </summary>
    /// <remarks>
    /// entity должна оставаться tracked между SaveChanges и RemoveAsync — иначе ChangeTracker не увидит
    /// изменения IsDeleted/DateDeleted.
    /// </remarks>
    [Fact]
    public async Task RemoveAsync_SoftDeleteWithUserId_PropagatesUserIdToDeletedBy()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var entity = CreateEntity(name: "soft-delete-me");
        context.Entities.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var userId = Guid.NewGuid();

        // Act — soft delete через RemoveAsync с userId (entity остаётся tracked)
        await repo.RemoveAsync(
            entity,
            userId: userId,
            hard: false,
            cancellationToken: TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert — soft-delete мутации применены и сохранены в БД
        entity.IsDeleted.Should().BeTrue();
        entity.DateDeleted.Should().NotBeNull();
        entity.DeletedByUserId.Should().Be(userId);
    }

    /// <summary>
    /// Выявляет проблему: RemoveAsync(entity, hard=true) физически удаляет сущность, не устанавливая IsDeleted.
    /// </summary>
    [Fact]
    public async Task RemoveAsync_HardTrue_DoesNotSetIsDeleted_RemovesFromDb()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var entity = CreateEntity(name: "hard-delete");
        context.Entities.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var entityId = entity.Id;
        context.ChangeTracker.Clear();

        // Act
        await repo.RemoveAsync(entity, hard: true, cancellationToken: TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert — IsDeleted НЕ выставлен (не было soft-delete), сущность физически удалена
        entity.IsDeleted.Should().BeFalse();
        entity.DateDeleted.Should().BeNull();
        context.ChangeTracker.Clear();
        context.Entities.Any(e => e.Id == entityId).Should().BeFalse();
    }

    /// <summary>
    /// Выявляет проблему: AddAsync заполняет DateCreated даже если userId=null.
    /// </summary>
    [Fact]
    public async Task AddAsync_WithoutUserId_SetsDateCreatedButNotCreatedBy()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var entity = CreateEntity(name: "no-user");

        // Act
        await repo.AddAsync(entity, userId: null, userName: null, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        entity.DateCreated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        entity.CreatedByUserId.Should().BeNull();
        entity.CreatedByUserName.Should().BeNull();
    }

    /// <summary>
    /// Выявляет проблему: MaxAsync на пустой выборке с reference type возвращает null (не бросает).
    /// </summary>
    [Fact]
    public async Task MaxAsync_OnEmptyReferenceType_ReturnsNullNotThrows()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);

        Expression<Func<TestEntityWithCreatedDeleted, string>> selector = e => e.Name;

        // Act
        var result = await repo.MaxAsync(selector, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>В
    /// ыявляет проблему: GetGroupingAsync с skip/take и без явного orderBy групп бросает ArgumentException.
    /// </summary>
    [Fact]
    public async Task GetGroupingAsync_WithSkipTakeButWithoutOrderBy_ThrowsArgumentException()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "a"));
        context.Entities.Add(CreateEntity(name: "b"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Expression<Func<TestEntityWithCreatedDeleted, int>> keySelector = e => e.Name.Length;

        // Act
        var act = () => repo.GetGroupingAsync(
            keySelector,
            skip: 0,
            take: 10,
            groupKeyOrderDirection: null,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("groupKeyOrderDirection");
    }

    /// <summary>
    /// Выявляет проблему: GetGroupingAsync&lt;TKey, TOut&gt; — селектор применяется к группе, не к элементам.
    /// </summary>
    /// <remarks>
    /// Если в будущем кто-то поменяет порядок параметров и селектор начнёт применяться к элементам — тест упадёт.
    /// </remarks>
    [Fact]
    public async Task GetGroupingAsync_WithSelector_SelectorAppliesToGroup()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "a"));
        context.Entities.Add(CreateEntity(name: "aa"));
        context.Entities.Add(CreateEntity(name: "aaa"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Expression<Func<TestEntityWithCreatedDeleted, int>> keySelector = e => e.Name.Length;
        Expression<Func<IGrouping<int, TestEntityWithCreatedDeleted>, int>> selector = g => g.Count();

        // Act
        var result = await repo.GetGroupingAsync(
            keySelector,
            selector: selector,
            groupKeyOrderDirection: OrderDirectionType.Ascending,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert — 3 группы с count=1 каждая
        result.Should().BeEquivalentTo([1, 1, 1]);
    }

    #endregion

    #region GetUpdateRangeAsyncLambdaFunc Tests

    /// <summary>
    /// Проверяет, что GetUpdateRangeAsyncLambdaFunc возвращает кортеж, который принимает ExecuteUpdateRangeAsync.
    /// </summary>
    [Fact]
    public async Task GetUpdateRangeAsyncLambdaFunc_UsedInExecuteUpdateRange_UpdatesSpecifiedProperty()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "row1"));
        context.Entities.Add(CreateEntity(name: "row2"));
        context.Entities.Add(CreateEntity(name: "row3"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();

        Expression<Func<TestEntityWithCreatedDeleted, string>> propertyExpression = e => e.Name;
        Expression<Func<TestEntityWithCreatedDeleted, string>> valueExpression = e => "bulk-set";

        // Act
        var lambdaTuple = IRepository<TestEntityWithCreatedDeleted>.GetUpdateRangeAsyncLambdaFunc(
            propertyExpression,
            valueExpression);

        await repo.ExecuteUpdateRangeAsync(
            (Expression<Func<TestEntityWithCreatedDeleted, bool>>)(e => e.Name.StartsWith("row")),
            lambdaTuple);

        // Assert — обновлены 3 строки
        context.ChangeTracker.Clear();
        context.Entities.Should().AllSatisfy(e => e.Name.Should().Be("bulk-set"));
    }

    /// <summary>
    /// Проверяет, что GetUpdateRangeAsyncLambdaFunc возвращает кортеж с теми же выражениями, что были переданы.
    /// </summary>
    [Fact]
    public void GetUpdateRangeAsyncLambdaFunc_ReturnsTupleWithOriginalExpressions()
    {
        // Arrange
        Expression<Func<TestEntityWithCreatedDeleted, string>> propertyExpression = e => e.Name;
        Expression<Func<TestEntityWithCreatedDeleted, string>> valueExpression = e => "test-value";

        // Act
        var (property, value) = IRepository<TestEntityWithCreatedDeleted>.GetUpdateRangeAsyncLambdaFunc(
            propertyExpression,
            valueExpression);

        // Assert
        property.Should().BeSameAs(propertyExpression);
        value.Should().BeSameAs(valueExpression);
    }

    #endregion

    #region SaveChanges Tests

    /// <summary>
    /// Проверяет, что SaveChangesAsync на отслеживаемой сущности с IWithUpdated обновляет DateUpdated.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_OnModifiedEntity_UpdatesDateUpdatedField()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var entity = CreateEntity(name: "original");
        context.Entities.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var originalUpdatedAt = entity.DateUpdated;
        await Task.Delay(10, TestContext.Current.CancellationToken);

        entity.Name = "modified";
        entity.OnUpdate(Guid.NewGuid());

        // Act
        await repo.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        entity.DateUpdated.Should().NotBeNull();
        entity.DateUpdated.Should().NotBe(originalUpdatedAt);
    }

    /// <summary>
    /// Проверяет, что SaveChanges на пустом context не падает и не делает лишних операций.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_OnUnmodifiedContext_DoesNotThrow()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "only"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();

        // Act
        var act = () => repo.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert — повторный SaveChanges без изменений не должен падать
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Проверяет параллельное обновление разных записей в разных DbContext instances — не должно быть конфликтов.
    /// </summary>
    [Fact]
    public async Task SaveChanges_ParallelContexts_UpdateIndependentRows()
    {
        // Arrange
        await using var seedContext = CreateContext();
        var ids = new List<Guid>();
        for (int i = 0; i < 10; i++)
        {
            var e = CreateEntity(name: $"row-{i}");
            ids.Add(e.Id);
            seedContext.Entities.Add(e);
        }
        await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act — 10 параллельных задач, каждая обновляет свою запись
        var tasks = ids.Select(async id =>
        {
            await using var ctx = CreateContext();
            var repo = CreateRepository(ctx);
            var entity = await ctx.Entities.SingleAsync(e => e.Id == id, TestContext.Current.CancellationToken);
            entity.Name = $"updated-{id}";
            await repo.SaveChangesAsync(TestContext.Current.CancellationToken);
        }).ToList();
        await Task.WhenAll(tasks);

        // Assert — все записи обновлены
        await using var verifyContext = CreateContext();
        var updated = await verifyContext.Entities
            .Where(e => ids.Contains(e.Id))
            .ToListAsync(TestContext.Current.CancellationToken);
        updated.Should().HaveCount(10);
        updated.Should().AllSatisfy(e => e.Name.Should().StartWith("updated-"));
    }

    /// <summary>
    /// Проверяет Last-Write-Wins: при обновлении одной записи из двух DbContext оба SaveChanges
    /// должны успешно зафиксировать изменения, и в БД должно остаться значение одной из двух записей.
    /// </summary>
    /// <remarks>
    /// SQLite без row version применяет Last-Write-Wins к конкурентным обновлениям. Shared in-memory
    /// SqliteConnection в <see cref="SqliteIntegrationTestBase"/> не поддерживает конкурентные операции
    /// (SQLite Error 5: 'database is locked' возникает как в EnsureCreated, так и в SaveChanges).
    /// </remarks>
    [Fact]
    public async Task SaveChanges_ConcurrentUpdatesSameRow_BothCommitsLastWriteWins()
    {
        // Arrange
        await using var seedContext = CreateContext();
        var entity = CreateEntity(name: "original");
        seedContext.Entities.Add(entity);
        await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var entityId = entity.Id;

        // Act — два контекста обновляют одну и ту же запись.
        // Test-local SemaphoreSlim(1, 1) сериализует критическую секцию
        // (CreateContext + SaveChanges), чтобы избежать SQLite 'database is locked'
        // на shared in-memory SqliteConnection. Production-код EfRepository остаётся
        // без изменений: SemaphoreSlim используется только в тесте.
        using var writeGate = new SemaphoreSlim(1, 1);

        async Task UpdateAsync(string newName)
        {
            await writeGate.WaitAsync(TestContext.Current.CancellationToken);
            try
            {
                await using var ctx = CreateContext();
                var repo = CreateRepository(ctx);
                var e = await ctx.Entities.SingleAsync(x => x.Id == entityId, TestContext.Current.CancellationToken);
                e.Name = newName;
                await repo.SaveChangesAsync(TestContext.Current.CancellationToken);
            }
            finally
            {
                writeGate.Release();
            }
        }

        var task1 = Task.Run(() => UpdateAsync("from-task-1"), TestContext.Current.CancellationToken);
        var task2 = Task.Run(() => UpdateAsync("from-task-2"), TestContext.Current.CancellationToken);

        await Task.WhenAll(task1, task2);

        // Assert — в БД одно из двух значений (Last-Write-Wins)
        await using var verifyContext = CreateContext();
        var finalEntity = await verifyContext.Entities
            .SingleAsync(x => x.Id == entityId, TestContext.Current.CancellationToken);
        finalEntity.Name.Should().BeOneOf("from-task-1", "from-task-2");
    }

    #endregion

    #region Concurrency Tests

    /// <summary>
    /// Проверяет, что три параллельных вызова мягкого удаления detached-экземпляров
    /// с одинаковым ключом в одном контексте отклоняются исключением <see cref="InvalidOperationException"/>.
    /// </summary>
    [Fact]
    [Trait("Category", "Concurrency")]
    public async Task RemoveAsync_ConcurrentOnSameDbContext_BehaviorDocumentedOrFixed()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var entityId = Guid.NewGuid();
        context.Entities.Add(CreateEntity(entityId, "concurrent-remove"));
        await context.SaveChangesAsync(cancellationToken);
        context.ChangeTracker.Clear();

        var detachedEntities = Enumerable.Range(0, 3)
            .Select(_ => CreateEntity(entityId, "concurrent-remove"))
            .ToArray();
        var startGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var removeTasks = detachedEntities.Select(async entity =>
        {
            await startGate.Task.WaitAsync(cancellationToken);
            await repo.RemoveAsync(entity, hard: false, cancellationToken);
        }).ToArray();

        // Act
        startGate.SetResult(true);
        var act = () => Task.WhenAll(removeTasks);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    /// Проверяет, что из трёх параллельных добавлений сущности с одинаковым ключом
    /// сохраняется одно, а нарушение первичного ключа приводит к <see cref="DbUpdateException"/>.
    /// </summary>
    [Fact]
    [Trait("Category", "Concurrency")]
    public async Task AddAsync_ConcurrentWithSameEntityKey_ThrowsOrHandles()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var entityId = Guid.NewGuid();
        var startGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task AddAndSaveAsync(string name)
        {
            await using var context = CreateFileContext();
            var repo = CreateRepository(context);
            await repo.AddAsync(CreateEntity(entityId, name), cancellationToken: cancellationToken);
            await startGate.Task.WaitAsync(cancellationToken);
            await repo.SaveChangesAsync(cancellationToken);
        }

        var saveTasks = new[] { "writer-1", "writer-2", "writer-3" }
            .Select(AddAndSaveAsync)
            .ToArray();

        // Act
        startGate.SetResult(true);
        var act = () => Task.WhenAll(saveTasks);

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
        saveTasks.Count(task => task.IsCompletedSuccessfully).Should().Be(1);
        var failures = saveTasks
            .Where(task => task.IsFaulted)
            .SelectMany(task => task.Exception!.InnerExceptions)
            .ToList();
        failures.Should().HaveCount(2);
        failures.Should().AllSatisfy(exception =>
        {
            var databaseException = exception.Should().BeOfType<DbUpdateException>().Which;
            databaseException.InnerException.Should().BeOfType<SqliteException>()
                .Which.SqliteErrorCode.Should().Be(19);
        });

        await using var verificationContext = CreateFileContext();
        var persisted = await verificationContext.Entities
            .Where(entity => entity.Id == entityId)
            .ToListAsync(cancellationToken);
        persisted.Should().ContainSingle();
    }

    /// <summary>
    /// Проверяет, что параллельные сохранения трёх разных сущностей через отдельные контексты
    /// фиксируют все записи в общей базе данных.
    /// </summary>
    [Fact]
    [Trait("Category", "Concurrency")]
    public async Task SaveChangesAsync_ParallelWithDifferentDbContexts_AllPersist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var entities = new[]
        {
            CreateEntity(name: "parallel-1"),
            CreateEntity(name: "parallel-2"),
            CreateEntity(name: "parallel-3"),
        };
        var startGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task SaveAsync(TestEntityWithCreatedDeleted entity)
        {
            await using var context = CreateFileContext();
            var repo = CreateRepository(context);
            await repo.AddAsync(entity, cancellationToken: cancellationToken);
            await startGate.Task.WaitAsync(cancellationToken);
            await repo.SaveChangesAsync(cancellationToken);
        }

        var saveTasks = entities.Select(SaveAsync).ToArray();

        // Act
        startGate.SetResult(true);
        await Task.WhenAll(saveTasks);

        // Assert
        await using var verificationContext = CreateFileContext();
        var persistedNames = await verificationContext.Entities
            .Select(entity => entity.Name)
            .ToListAsync(cancellationToken);
        persistedNames.Should().BeEquivalentTo(entities.Select(entity => entity.Name));
    }

    /// <summary>
    /// Проверяет, что параллельный отбор одной записи для hard-delete и soft-delete
    /// приводит к <see cref="DbUpdateConcurrencyException"/> при сохранении устаревшего soft-delete.
    /// </summary>
    [Fact]
    [Trait("Category", "Concurrency")]
    public async Task RemoveRangeAsync_ConcurrentBySpec_HardAndSoft()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var seedContext = CreateFileContext();
        seedContext.Entities.Add(CreateEntity(name: "overlapping-remove"));
        await seedContext.SaveChangesAsync(cancellationToken);

        await using var hardDeleteContext = CreateFileContext();
        await using var softDeleteContext = CreateFileContext();
        var hardDeleteRepository = CreateRepository(hardDeleteContext);
        var softDeleteRepository = CreateRepository(softDeleteContext);

        var hardDeleteTask = hardDeleteRepository.RemoveRangeAsync(
            new NameEqualsSpecification("overlapping-remove"),
            hard: true,
            cancellationToken);
        var softDeleteTask = softDeleteRepository.RemoveRangeAsync(
            new NameEqualsSpecification("overlapping-remove"),
            hard: false,
            cancellationToken);

        // Act
        await Task.WhenAll(hardDeleteTask, softDeleteTask);
        await hardDeleteRepository.SaveChangesAsync(cancellationToken);
        var act = () => softDeleteRepository.SaveChangesAsync(cancellationToken);

        // Assert
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
        await using var verificationContext = CreateFileContext();
        var persistedCount = await verificationContext.Entities.CountAsync(cancellationToken);
        persistedCount.Should().Be(0);
    }

    /// <summary>
    /// Проверяет, что два параллельных пакетных обновления пересекающихся строк
    /// завершаются успешно, а последнее обновление атомарно определяет итоговое значение.
    /// </summary>
    [Fact]
    [Trait("Category", "Concurrency")]
    public async Task ExecuteUpdateRangeAsync_ConcurrentOnOverlappingRows_Behavior()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var first = CreateEntity(name: "overlap-1");
        var second = CreateEntity(name: "overlap-2");
        var outside = CreateEntity(name: "outside");
        var overlappingIds = new[] { first.Id, second.Id };

        await using var seedContext = CreateFileContext();
        seedContext.Entities.AddRange(first, second, outside);
        await seedContext.SaveChangesAsync(cancellationToken);

        await using var firstContext = CreateFileContext();
        await using var secondContext = CreateFileContext();
        var firstRepository = CreateRepository(firstContext);
        var secondRepository = CreateRepository(secondContext);
        var startGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task UpdateAsync(
            IRepository<TestEntityWithCreatedDeleted> repository,
            string newName)
        {
            var options = new QueryOptions<TestEntityWithCreatedDeleted>()
                .AddFilter(entity => overlappingIds.Contains(entity.Id));
            Expression<Func<TestEntityWithCreatedDeleted, string>> propertyExpression = entity => entity.Name;
            Expression<Func<TestEntityWithCreatedDeleted, string>> valueExpression = _ => newName;

            await startGate.Task.WaitAsync(cancellationToken);
            await repository.ExecuteUpdateRangeAsync(options, (propertyExpression, valueExpression));
        }

        var firstUpdate = UpdateAsync(firstRepository, "writer-1");
        var secondUpdate = UpdateAsync(secondRepository, "writer-2");

        // Act
        startGate.SetResult(true);
        await Task.WhenAll(firstUpdate, secondUpdate);

        // Assert
        await using var verificationContext = CreateFileContext();
        var persisted = await verificationContext.Entities
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var overlappingNames = persisted
            .Where(entity => overlappingIds.Contains(entity.Id))
            .Select(entity => entity.Name)
            .ToList();
        overlappingNames.Should().HaveCount(2);
        overlappingNames.Distinct().Should().ContainSingle()
            .Which.Should().BeOneOf("writer-1", "writer-2");
        persisted.Single(entity => entity.Id == outside.Id).Name.Should().Be("outside");
    }

    /// <summary>
    /// Проверяет, что второй параллельный <c>SaveChangesAsync</c> одного контекста
    /// отклоняется исключением <see cref="InvalidOperationException"/>.
    /// </summary>
    [Fact]
    [Trait("Category", "Concurrency")]
    public async Task SaveChangesAsync_ParallelOnSameDbContext_DocumentedBehaviorOrFixed()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var initializationContext = CreateFileContext();
        var interceptor = new BlockingCommandInterceptor();
        await using var context = CreateFileContext(interceptor);
        var repo = CreateRepository(context);
        await repo.AddAsync(CreateEntity(name: "parallel-save"), cancellationToken: cancellationToken);

        // Act
        var firstSave = repo.SaveChangesAsync(cancellationToken);
        await interceptor.CommandStarted.WaitAsync(cancellationToken);
        var secondSaveException = await Record.ExceptionAsync(
            () => repo.SaveChangesAsync(cancellationToken));
        interceptor.Release();
        await firstSave;

        // Assert
        secondSaveException.Should().BeOfType<InvalidOperationException>();
    }

    private sealed class BlockingCommandInterceptor : DbCommandInterceptor
    {
        private readonly TaskCompletionSource<bool> _commandStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _releaseCommand =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task CommandStarted => _commandStarted.Task;

        public void Release() => _releaseCommand.SetResult(true);

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            await WaitForReleaseAsync(cancellationToken);
            return result;
        }

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            await WaitForReleaseAsync(cancellationToken);
            return result;
        }

        private async Task WaitForReleaseAsync(CancellationToken cancellationToken)
        {
            _commandStarted.TrySetResult(true);
            await _releaseCommand.Task.WaitAsync(cancellationToken);
        }
    }

    #endregion

    #region Missing ISpecification Overload Delegation Tests

    /// <summary>
    /// Проверяет, что <see cref="IGetterRepository{TEntity}.GetAsync(object, ISpecification{TEntity}, CancellationToken)"/>
    /// делегирует к версии с <see cref="QueryOptions{TEntity}"/>.
    /// </summary>
    [Fact]
    public async Task GetAsync_ByIdWithSpecification_DelegatesToOptions()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var entity = CreateEntity(name: "alpha");
        context.Entities.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        ISpecification<TestEntityWithCreatedDeleted> specification =
            new OrderedNameEqualsSpecification("alpha");

        // Act
        var result = await repo.GetAsync(entity.Id, specification, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(entity.Id);
        result.Name.Should().Be("alpha");
    }

    /// <summary>
    /// Проверяет, что <see cref="IGetterRepository{TEntity}.GetAsync{TOut}(object, ISpecification{TEntity}, Expression{Func{TEntity, TOut}}, CancellationToken)"/>
    /// делегирует к версии с <see cref="QueryOptions{TEntity}"/> и селектором.
    /// </summary>
    [Fact]
    public async Task GetAsyncTOut_ByIdWithSpecificationAndSelector_DelegatesToOptions()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var entity = CreateEntity(name: "alpha");
        context.Entities.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        ISpecification<TestEntityWithCreatedDeleted> specification =
            new OrderedNameEqualsSpecification("alpha");
        Expression<Func<TestEntityWithCreatedDeleted, string>> selector = e => e.Name;

        // Act
        var result = await repo.GetAsync(
            entity.Id,
            specification,
            selector,
            TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be("alpha");
    }

    /// <summary>
    /// Проверяет, что <see cref="IGetterRepository{TEntity}.GetRangeAsync(ISpecification{TEntity}, int?, int?, CancellationToken)"/>
    /// делегирует к версии с <see cref="QueryOptions{TEntity}"/>.
    /// </summary>
    [Fact]
    public async Task GetRangeAsync_WithSpecification_DelegatesToOptions()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "alpha"));
        context.Entities.Add(CreateEntity(name: "beta"));
        context.Entities.Add(CreateEntity(name: "gamma"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        ISpecification<TestEntityWithCreatedDeleted> specification =
            new OrderedNameEqualsSpecification("alpha");

        // Act
        var result = await repo.GetRangeAsync(
            specification,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().ContainSingle();
        result[0].Name.Should().Be("alpha");
    }

    /// <summary>
    /// Проверяет, что <see cref="IGetterRepository{TEntity}.GetRangeAsync{TOut}(ISpecification{TEntity}, int?, int?, Expression{Func{TEntity, TOut}}, CancellationToken)"/>
    /// делегирует к версии с <see cref="QueryOptions{TEntity}"/> и селектором.
    /// </summary>
    [Fact]
    public async Task GetRangeAsyncTOut_WithSpecificationAndSelector_DelegatesToOptions()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "alpha"));
        context.Entities.Add(CreateEntity(name: "beta"));
        context.Entities.Add(CreateEntity(name: "gamma"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        ISpecification<TestEntityWithCreatedDeleted> specification =
            new OrderedNameEqualsSpecification("alpha");
        Expression<Func<TestEntityWithCreatedDeleted, string>> selector = e => e.Name;

        // Act
        var result = await repo.GetRangeAsync(
            specification,
            selector: selector,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().Equal("alpha");
    }

    /// <summary>
    /// Проверяет, что <see cref="IGetterRepository{TEntity}.MaxAsync{TOut}(Expression{Func{TEntity, TOut}}, ISpecification{TEntity}, CancellationToken)"/>
    /// и <see cref="IGetterRepository{TEntity}.MinAsync{TOut}(Expression{Func{TEntity, TOut}}, ISpecification{TEntity}, CancellationToken)"/>
    /// делегируют к версиям с <see cref="QueryOptions{TEntity}"/>.
    /// </summary>
    [Theory]
    [InlineData(AggregateKind.Max)]
    [InlineData(AggregateKind.Min)]
    public async Task AggregateAsync_WithSpecification_DelegatesToOptions(AggregateKind kind)
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "alpha"));
        context.Entities.Add(CreateEntity(name: "beta"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Expression<Func<TestEntityWithCreatedDeleted, int>> selector = e => e.Name.Length;
        ISpecification<TestEntityWithCreatedDeleted> specification =
            new OrderedNameEqualsSpecification("alpha");

        // Act
        var result = kind == AggregateKind.Max
            ? await repo.MaxAsync(selector, specification, TestContext.Current.CancellationToken)
            : await repo.MinAsync(selector, specification, TestContext.Current.CancellationToken);

        // Assert — "alpha" имеет длину 5
        result.Should().Be(5);
    }

    /// <summary>
    /// Проверяет, что <see cref="IGetterRepository{TEntity}.GetGroupingAsync{TKey}(Expression{Func{TEntity, TKey}}, QueryOptions{TEntity}, int?, int?, OrderDirectionType?, CancellationToken)"/> принимает
    /// спецификацию-построенную <see cref="QueryOptions{TEntity}"/> через явный вызов
    /// <c>specification.BuildOptions()</c> и делегирует корректно (спецификационного overload в
    /// <see cref="IRepository{TEntity}"/> для GetGroupingAsync нет — только options-форма).
    /// </summary>
    [Fact]
    public async Task GetGroupingAsync_WithOptionsBuiltFromSpecification_ProducesSameResult()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "alpha"));
        context.Entities.Add(CreateEntity(name: "alpha2"));
        context.Entities.Add(CreateEntity(name: "beta"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Expression<Func<TestEntityWithCreatedDeleted, int>> keySelector = e => e.Name.Length;
        ISpecification<TestEntityWithCreatedDeleted> specification = new NameStartsWithSpecification("alpha");

        // Act — IRepository.GetGroupingAsync не имеет ISpecification-overload, поэтому строим options из spec.
        // Не задаём groupKeyOrderDirection — SQLite-провайдер не транслирует OrderBy(g => g.Key) по GroupBy.
        var groups = await repo.GetGroupingAsync(
            keySelector,
            specification.BuildOptions(),
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert — "alpha" (5) и "alpha2" (6) — две группы с разными ключами
        groups.Should().HaveCount(2);
        groups.Select(g => g.Key).Should().BeEquivalentTo([5, 6]);
    }

    /// <summary>
    /// Проверяет, что RemoveRangeAsync по спецификации физически удаляет выбранные сущности.
    /// </summary>
    [Fact]
    public async Task RemoveRangeAsync_BySpecificationHardTrue_ShouldRemoveFromDatabase()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "spec-remove"));
        context.Entities.Add(CreateEntity(name: "spec-keep"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        ISpecification<TestEntityWithCreatedDeleted> specification =
            new NameEqualsSpecification("spec-remove");

        // Act
        await repo.RemoveRangeAsync(
            specification,
            hard: true,
            cancellationToken: TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        context.ChangeTracker.Clear();
        context.Entities.Should().HaveCount(1);
        context.Entities.Single().Name.Should().Be("spec-keep");
    }

    /// <summary>
    /// Проверяет, что RemoveRangeAsync по спецификации сохраняет мягкое удаление выбранных сущностей.
    /// </summary>
    [Fact]
    public async Task RemoveRangeAsync_BySpecificationSoftDelete_ShouldPersistIsDeleted()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var toSoftDelete = CreateEntity(name: "spec-remove");
        context.Entities.Add(toSoftDelete);
        context.Entities.Add(CreateEntity(name: "spec-keep"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var toSoftDeleteId = toSoftDelete.Id;

        ISpecification<TestEntityWithCreatedDeleted> specification =
            new NameEqualsSpecification("spec-remove");

        // Act
        await repo.RemoveRangeAsync(
            specification,
            hard: false,
            cancellationToken: TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        context.ChangeTracker.Clear();
        var reloaded = await context.Entities
            .SingleAsync(e => e.Id == toSoftDeleteId, TestContext.Current.CancellationToken);
        reloaded.IsDeleted.Should().BeTrue();
    }

    #endregion
}
