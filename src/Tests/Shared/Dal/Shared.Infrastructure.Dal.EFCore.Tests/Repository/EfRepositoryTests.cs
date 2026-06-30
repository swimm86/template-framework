// ----------------------------------------------------------------------------------------------
// <copyright file="EfRepositoryTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Interfaces;
using Shared.Infrastructure.Dal.EFCore.Repository;
using Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;
using Shared.Testing.Doubles.Mapping;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Repository;

/// <summary>Тесты для класса <see cref="EfRepository{TEntity}"/>.</summary>
public sealed class EfRepositoryTests
{
    private static TestEfRepositoryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestEfRepositoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new TestEfRepositoryDbContext(options);
    }

    private static IRepository<TestEntityWithCreatedDeleted> CreateRepository(
        TestEfRepositoryDbContext context)
    {
        var evaluator = new EfQueryEvaluator(new FakeMapper());
        return new EfRepository<TestEntityWithCreatedDeleted>(context, evaluator);
    }

    private static TestEntityWithCreatedDeleted CreateEntity(Guid? id = null, string name = "test")
    {
        return new TestEntityWithCreatedDeleted
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
        };
    }

    #region GetAsync Tests

    /// <summary>
    /// Проверяет что GetAsync возвращает сущность по идентификатору.
    /// </summary>
    [Fact]
    public async Task GetAsync_ById_ReturnsEntity()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var entity = CreateEntity();
        context.Entities.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await repo.GetAsync(entity.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(entity.Id);
    }

    /// <summary>
    /// Проверяет что GetAsync возвращает null при отсутствии сущности.
    /// </summary>
    [Fact]
    public async Task GetAsync_NotFound_ReturnsNull()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);

        // Act
        var result = await repo.GetAsync(Guid.NewGuid(), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Проверяет что GetAsync с селектором возвращает спроецированное значение.
    /// </summary>
    [Fact]
    public async Task GetAsync_WithSelector_ReturnsProjectedResult()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var entity = CreateEntity(name: "projected");
        context.Entities.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Expression<Func<TestEntityWithCreatedDeleted, string>> selector = e => e.Name;

        // Act
        var result = await repo.GetAsync(entity.Id, selector: selector, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be("projected");
    }

    #endregion

    #region GetRangeAsync Tests

    /// <summary>
    /// Проверяет что GetRangeAsync возвращает все сущности.
    /// </summary>
    [Fact]
    public async Task GetRangeAsync_ReturnsAllEntities()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        for (var i = 0; i < 5; i++)
        {
            context.Entities.Add(CreateEntity(name: $"entity-{i}"));
        }
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await repo.GetRangeAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(5);
    }

    /// <summary>
    /// Проверяет что GetRangeAsync корректно применяет пагинацию skip/take.
    /// </summary>
    [Fact]
    public async Task GetRangeAsync_WithSkipTake_ReturnsPaginatedResults()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        for (var i = 0; i < 10; i++)
        {
            context.Entities.Add(CreateEntity(name: $"entity-{i}"));
        }
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await repo.GetRangeAsync(skip: 2, take: 3, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(3);
    }

    /// <summary>
    /// Проверяет что GetRangeAsync с селектором возвращает спроецированные результаты.
    /// </summary>
    [Fact]
    public async Task GetRangeAsync_WithSelector_ReturnsProjectedResults()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "projected1"));
        context.Entities.Add(CreateEntity(name: "projected2"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Expression<Func<TestEntityWithCreatedDeleted, string>> selector = e => e.Name;

        // Act
        var result = await repo.GetRangeAsync(selector: selector, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEquivalentTo("projected1", "projected2");
    }

    /// <summary>
    /// Проверяет что GetRangeAsync с фильтром возвращает отфильтрованные результаты.
    /// </summary>
    [Fact]
    public async Task GetRangeAsync_WithFilter_ReturnsFilteredResults()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "alpha"));
        context.Entities.Add(CreateEntity(name: "beta"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var options = new QueryOptions<TestEntityWithCreatedDeleted>();
        options.AddFilter(e => e.Name == "alpha");

        // Act
        var result = await repo.GetRangeAsync(options, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("alpha");
    }

    /// <summary>
    /// Проверяет что GetRangeAsync возвращает пустой список при пустой БД.
    /// </summary>
    [Fact]
    public async Task GetRangeAsync_EmptyDb_ReturnsEmptyList()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);

        // Act
        var result = await repo.GetRangeAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region FirstOrDefaultAsync Tests

    /// <summary>
    /// Проверяет что FirstOrDefaultAsync возвращает первый элемент.
    /// </summary>
    [Fact]
    public async Task FirstOrDefaultAsync_ReturnsFirstEntity()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "first"));
        context.Entities.Add(CreateEntity(name: "second"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await repo.FirstOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
    }

    /// <summary>
    /// Проверяет что FirstOrDefaultAsync возвращает null при пустой БД.
    /// </summary>
    [Fact]
    public async Task FirstOrDefaultAsync_EmptyDb_ReturnsNull()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);

        // Act
        var result = await repo.FirstOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region SingleOrDefaultAsync Tests

    /// <summary>
    /// Проверяет что SingleOrDefaultAsync возвращает единственную подходящую сущность.
    /// </summary>
    [Fact]
    public async Task SingleOrDefaultAsync_SingleMatch_ReturnsEntity()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var entity = CreateEntity();
        context.Entities.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var options = new QueryOptions<TestEntityWithCreatedDeleted>();
        options.AddFilter(e => e.Id == entity.Id);

        // Act
        var result = await repo.SingleOrDefaultAsync(options, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(entity.Id);
    }

    /// <summary>
    /// Проверяет что SingleOrDefaultAsync возвращает null при отсутствии совпадений.
    /// </summary>
    [Fact]
    public async Task SingleOrDefaultAsync_NoMatch_ReturnsNull()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);

        var options = new QueryOptions<TestEntityWithCreatedDeleted>();
        options.AddFilter(e => e.Id == Guid.NewGuid());

        // Act
        var result = await repo.SingleOrDefaultAsync(options, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Проверяет что SingleOrDefaultAsync с селектором возвращает спроецированное значение.
    /// </summary>
    [Fact]
    public async Task SingleOrDefaultAsync_WithSelector_ReturnsProjectedValue()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "single"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Expression<Func<TestEntityWithCreatedDeleted, string>> selector = e => e.Name;

        // Act
        var result = await repo.SingleOrDefaultAsync(selector: selector, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be("single");
    }

    #endregion

    #region LastOrDefaultAsync Tests

    /// <summary>
    /// Проверяет что LastOrDefaultAsync возвращает последний элемент.
    /// </summary>
    [Fact]
    public async Task LastOrDefaultAsync_ReturnsLastEntity()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "first"));
        context.Entities.Add(CreateEntity(name: "last"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await repo.LastOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("last");
    }

    /// <summary>
    /// Проверяет что LastOrDefaultAsync с селектором возвращает спроецированное значение.
    /// </summary>
    [Fact]
    public async Task LastOrDefaultAsync_WithSelector_ReturnsProjectedValue()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "last-projected"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Expression<Func<TestEntityWithCreatedDeleted, string>> selector = e => e.Name;

        // Act
        var result = await repo.LastOrDefaultAsync(selector: selector, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be("last-projected");
    }

    #endregion

    #region CountAsync Tests

    /// <summary>
    /// Проверяет что CountAsync возвращает корректное количество сущностей.
    /// </summary>
    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        for (var i = 0; i < 5; i++)
        {
            context.Entities.Add(CreateEntity());
        }
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var count = await repo.CountAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        count.Should().Be(5);
    }

    /// <summary>
    /// Проверяет что CountAsync с фильтром возвращает отфильтрованное количество.
    /// </summary>
    [Fact]
    public async Task CountAsync_WithFilter_ReturnsFilteredCount()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "match"));
        context.Entities.Add(CreateEntity(name: "match"));
        context.Entities.Add(CreateEntity(name: "other"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var options = new QueryOptions<TestEntityWithCreatedDeleted>();
        options.AddFilter(e => e.Name == "match");

        // Act
        var count = await repo.CountAsync(options, TestContext.Current.CancellationToken);

        // Assert
        count.Should().Be(2);
    }

    /// <summary>
    /// Проверяет что CountAsync возвращает 0 при пустой БД.
    /// </summary>
    [Fact]
    public async Task CountAsync_EmptyDb_ReturnsZero()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);

        // Act
        var count = await repo.CountAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        count.Should().Be(0);
    }

    #endregion

    #region AnyAsync Tests

    /// <summary>
    /// Проверяет что AnyAsync возвращает true при наличии сущностей.
    /// </summary>
    [Fact]
    public async Task AnyAsync_WithEntities_ReturnsTrue()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity());
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await repo.AnyAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет что AnyAsync возвращает false при пустой БД.
    /// </summary>
    [Fact]
    public async Task AnyAsync_EmptyDb_ReturnsFalse()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);

        // Act
        var result = await repo.AnyAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Проверяет что AnyAsync с фильтром возвращает корректный результат.
    /// </summary>
    [Fact]
    public async Task AnyAsync_WithFilter_ReturnsCorrectResult()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "exists"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var options = new QueryOptions<TestEntityWithCreatedDeleted>();
        options.AddFilter(e => e.Name == "exists");

        // Act
        var result = await repo.AnyAsync(
            options,
            TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region SumAsync Tests

    /// <summary>
    /// Проверяет что SumAsync возвращает корректную сумму по селектору.
    /// </summary>
    [Fact]
    public async Task SumAsync_ReturnsCorrectSum()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "a"));
        context.Entities.Add(CreateEntity(name: "bb"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Expression<Func<TestEntityWithCreatedDeleted, decimal>> selector = e => e.Name.Length;

        // Act
        var sum = await repo.SumAsync(
            selector,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        sum.Should().Be(3);
    }

    #endregion

    #region AddAsync Tests

    /// <summary>
    /// Проверяет что AddAsync добавляет сущность в DbContext.
    /// </summary>
    [Fact]
    public async Task AddAsync_AddsEntityToDbContext()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var entity = CreateEntity();

        // Act
        await repo.AddAsync(
            entity,
            userId: null,
            userName: null,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        context.Entry(entity).State.Should().Be(EntityState.Added);
    }

    /// <summary>
    /// Проверяет что AddAsync для IWithCreated сущности устанавливает информацию о создании.
    /// </summary>
    [Fact]
    public async Task AddAsync_IWithCreatedEntity_SetsCreationInfo()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var entity = CreateEntity();
        var userId = Guid.NewGuid();
        var userName = "testuser";

        // Act
        await repo.AddAsync(
            entity,
            userId,
            userName,
            TestContext.Current.CancellationToken);

        // Assert
        entity.CreatedByUserId.Should().Be(userId);
        entity.CreatedByUserName.Should().Be(userName);
        entity.DateCreated.Should().NotBe(default);
    }

    /// <summary>
    /// Проверяет что AddAsync возвращает ту же сущность.
    /// </summary>
    [Fact]
    public async Task AddAsync_ReturnsSameEntity()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var entity = CreateEntity();

        // Act
        var result = await repo.AddAsync(
            entity,
            userId: null,
            userName: null,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeSameAs(entity);
    }

    #endregion

    #region AddRangeAsync Tests

    /// <summary>
    /// Проверяет что AddRangeAsync добавляет несколько сущностей.
    /// </summary>
    [Fact]
    public async Task AddRangeAsync_AddsMultipleEntities()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var entities = new[] { CreateEntity(), CreateEntity(), CreateEntity() };

        // Act
        await repo.AddRangeAsync(
            entities,
            userId: null,
            userName: null,
            cancellationToken: TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        context.Entities.Should().HaveCount(3);
    }

    /// <summary>
    /// Проверяет что AddRangeAsync для IWithCreated сущностей устанавливает информацию о создании.
    /// </summary>
    [Fact]
    public async Task AddRangeAsync_IWithCreatedEntities_SetsCreationInfo()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var userId = Guid.NewGuid();
        var entities = new[] { CreateEntity(), CreateEntity() };

        // Act
        await repo.AddRangeAsync(
            entities,
            userId,
            userName: "user",
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        entities.Should().AllSatisfy(e =>
        {
            e.CreatedByUserId.Should().Be(userId);
            e.DateCreated.Should().NotBe(default);
        });
    }

    #endregion

    #region RemoveAsync Tests

    /// <summary>
    /// Проверяет что RemoveAsync при мягком удалении устанавливает IsDeleted.
    /// </summary>
    [Fact]
    public async Task RemoveAsync_SoftDelete_SetsIsDeleted()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var entity = CreateEntity();
        context.Entities.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await repo.RemoveAsync(
            entity,
            hard: false,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        entity.IsDeleted.Should().BeTrue();
        entity.DateDeleted.Should().NotBeNull();
        context.Entry(entity).State.Should().Be(EntityState.Modified);
    }

    /// <summary>
    /// Проверяет что RemoveAsync при жёстком удалении удаляет сущность из DbContext.
    /// </summary>
    [Fact]
    public async Task RemoveAsync_HardDelete_RemovesFromDbContext()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var entity = CreateEntity();
        context.Entities.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await repo.RemoveAsync(
            entity,
            hard: true,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        context.Entry(entity).State.Should().Be(EntityState.Deleted);
    }

    /// <summary>
    /// Проверяет что RemoveAsync с userId устанавливает DeletedByUserId.
    /// </summary>
    [Fact]
    public async Task RemoveAsync_WithUserId_SetsDeletedByUserId()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var entity = CreateEntity();
        context.Entities.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var userId = Guid.NewGuid();

        // Act
        await repo.RemoveAsync(
            entity,
            userId,
            hard: false,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        entity.DeletedByUserId.Should().Be(userId);
    }

    /// <summary>
    /// Проверяет что RemoveAsync без userId оставляет DeletedByUserId равным null.
    /// </summary>
    [Fact]
    public async Task RemoveAsync_WithoutUserId_SetsNullDeletedByUserId()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var entity = CreateEntity();
        context.Entities.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await repo.RemoveAsync(
            entity,
            userId: null,
            hard: false,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        entity.DeletedByUserId.Should().BeNull();
    }

    #endregion

    #region RemoveRangeAsync Tests

    /// <summary>
    /// Проверяет что RemoveRangeAsync по сущностям мягко удаляет все.
    /// </summary>
    [Fact]
    public async Task RemoveRangeAsync_ByEntities_SoftDeletesAll()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var entities = new[] { CreateEntity(), CreateEntity() };
        foreach (var e in entities)
        {
            context.Entities.Add(e);
        }
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await repo.RemoveRangeAsync(entities, hard: false, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        entities.Should().AllSatisfy(e => e.IsDeleted.Should().BeTrue());
    }

    /// <summary>
    /// Проверяет что RemoveRangeAsync по условию удаляет подходящие сущности.
    /// </summary>
    [Fact]
    public async Task RemoveRangeAsync_ByCondition_RemovesMatchingEntities()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "remove"));
        context.Entities.Add(CreateEntity(name: "keep"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Expression<Func<TestEntityWithCreatedDeleted, bool>> condition = e => e.Name == "remove";

        // Act
        await repo.RemoveRangeAsync(condition, hard: true, cancellationToken: TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        context.Entities.Should().HaveCount(1);
    }

    #endregion

    #region RemovePermanentRangeAsync Tests

    /// <summary>
    /// Проверяет что RemovePermanentRangeAsync жёстко удаляет все сущности.
    /// </summary>
    [Fact]
    public async Task RemovePermanentRangeAsync_HardDeletesAll()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var entities = new[] { CreateEntity(), CreateEntity() };
        foreach (var e in entities)
        {
            context.Entities.Add(e);
        }
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await repo.RemovePermanentRangeAsync(entities, TestContext.Current.CancellationToken);

        // Assert
        entities.Should().AllSatisfy(e =>
            context.Entry(e).State.Should().Be(EntityState.Deleted));
    }

    #endregion

    #region Execute Tests

    /// <summary>
    /// Проверяет что Execute без транзакции возвращает результат.
    /// </summary>
    [Fact]
    public void Execute_WithoutTransaction_ReturnsResult()
    {
        // Arrange
        using var context = CreateContext();
        var repo = CreateRepository(context);

        // Act
        var result = repo.Execute(() => 42, useTransaction: false);

        // Assert
        result.Should().Be(42);
    }

    /// <summary>
    /// Проверяет что Execute с транзакцией фиксирует изменения при успехе.
    /// </summary>
    [Fact]
    public void Execute_WithTransaction_CommitsOnSuccess()
    {
        // Arrange
        using var context = CreateContext();
        var repo = CreateRepository(context);

        // Act
        var result = repo.Execute(() => 42, useTransaction: true);

        // Assert
        result.Should().Be(42);
    }

    /// <summary>
    /// Проверяет что Execute с транзакцией откатывает изменения при ошибке.
    /// </summary>
    [Fact]
    public void Execute_WithTransaction_RollbacksOnFailure()
    {
        // Arrange
        using var context = CreateContext();
        var repo = CreateRepository(context);

        // Act
        var act = () => repo.Execute<int>(() => throw new InvalidOperationException("fail"), useTransaction: true);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region ExecuteAsync Tests

    /// <summary>
    /// Проверяет что ExecuteAsync без транзакции возвращает результат.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithoutTransaction_ReturnsResult()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);

        // Act
        var result = await repo.ExecuteAsync(
            () => Task.FromResult(42),
            useTransaction: false,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(42);
    }

    /// <summary>
    /// Проверяет что ExecuteAsync с транзакцией фиксирует изменения при успехе.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithTransaction_CommitsOnSuccess()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);

        // Act
        var result = await repo.ExecuteAsync(
            () => Task.FromResult(42),
            useTransaction: true,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(42);
    }

    /// <summary>
    /// Проверяет что ExecuteAsync с транзакцией откатывает изменения при ошибке.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithTransaction_RollbacksOnFailure()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);

        // Act
        var act = () => repo.ExecuteAsync<int>(() => throw new InvalidOperationException("fail"), useTransaction: true);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

    #region Set Tests

    /// <summary>
    /// Проверяет что Set без опций возвращает IQueryable.
    /// </summary>
    [Fact]
    public void Set_WithoutOptions_ReturnsQueryable()
    {
        // Arrange
        using var context = CreateContext();
        var repo = CreateRepository(context);

        // Act
        var queryable = repo.Set();

        // Assert
        queryable.Should().NotBeNull();
        queryable.Should().BeAssignableTo<IQueryable<TestEntityWithCreatedDeleted>>();
    }

    /// <summary>
    /// Проверяет что Set с опциями возвращает отфильтрованный IQueryable.
    /// </summary>
    [Fact]
    public void Set_WithOptions_ReturnsFilteredQueryable()
    {
        // Arrange
        using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "visible"));
        context.Entities.Add(CreateEntity(name: "hidden"));
        context.SaveChanges();

        var options = new QueryOptions<TestEntityWithCreatedDeleted>();
        options.AddFilter(e => e.Name == "visible");

        // Act
        var queryable = repo.Set(options);

        // Assert
        queryable.Should().HaveCount(1);
    }

    #endregion

    #region SaveChanges Tests

    /// <summary>
    /// Проверяет что SaveChanges делегирует сохранение DbContext.
    /// </summary>
    [Fact]
    public void SaveChanges_DelegatesToDbContext()
    {
        // Arrange
        using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity());

        // Act
        repo.SaveChanges();

        // Assert
        context.Entities.Should().HaveCount(1);
    }

    /// <summary>Проверяет что SaveChangesAsync делегирует сохранение DbContext.</summary>
    [Fact]
    public async Task SaveChangesAsync_DelegatesToDbContext()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity());

        // Act
        await repo.SaveChangesAsync(CancellationToken.None);

        // Assert
        context.Entities.Should().HaveCount(1);
    }

    #endregion

    #region NoTracking Tests

    /// <summary>
    /// Проверяет что GetAsync с no-tracking отвязывает сущность от контекста.
    /// </summary>
    [Fact]
    public async Task GetAsync_NoTracking_DetachesEntity()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var entity = CreateEntity();
        context.Entities.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var options = new QueryOptions<TestEntityWithCreatedDeleted>(withTracking: false);

        // Act
        var result = await repo.GetAsync(entity.Id, options, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        context.Entry(result).State.Should().Be(EntityState.Detached);
    }

    #endregion

    #region MaxAsync / MinAsync Tests

    public enum AggregateKind { Max, Min }

    public enum ProjectionKind { IntLength, StringName }

    public enum Population { Empty, Populated }

    /// <summary>
    /// Проверяет MaxAsync и MinAsync для value/reference типов на пустой и заполненной выборке.
    /// </summary>
    [Theory]
    [InlineData(AggregateKind.Max, ProjectionKind.IntLength, Population.Empty, 0)]
    [InlineData(AggregateKind.Max, ProjectionKind.IntLength, Population.Populated, 4)]
    [InlineData(AggregateKind.Max, ProjectionKind.StringName, Population.Empty, null)]
    [InlineData(AggregateKind.Max, ProjectionKind.StringName, Population.Populated, "ccc")]
    [InlineData(AggregateKind.Min, ProjectionKind.IntLength, Population.Empty, 0)]
    [InlineData(AggregateKind.Min, ProjectionKind.IntLength, Population.Populated, 1)]
    [InlineData(AggregateKind.Min, ProjectionKind.StringName, Population.Empty, null)]
    [InlineData(AggregateKind.Min, ProjectionKind.StringName, Population.Populated, "a")]
    public async Task AggregateAsync_ReturnsExpected(
        AggregateKind kind,
        ProjectionKind projection,
        Population population,
        object? expected)
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);

        if (population == Population.Populated)
        {
            context.Entities.Add(CreateEntity(name: "a"));
            context.Entities.Add(CreateEntity(name: "bbbb"));
            context.Entities.Add(CreateEntity(name: "ccc"));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        object? result = (kind, projection) switch
        {
            (AggregateKind.Max, ProjectionKind.IntLength) => await repo.MaxAsync(
                e => e.Name.Length,
                cancellationToken: TestContext.Current.CancellationToken),
            (AggregateKind.Max, ProjectionKind.StringName) => await repo.MaxAsync(
                e => e.Name,
                cancellationToken: TestContext.Current.CancellationToken),
            (AggregateKind.Min, ProjectionKind.IntLength) => await repo.MinAsync(
                e => e.Name.Length,
                cancellationToken: TestContext.Current.CancellationToken),
            (AggregateKind.Min, ProjectionKind.StringName) => await repo.MinAsync(
                e => e.Name,
                cancellationToken: TestContext.Current.CancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region GetGroupingAsync / CountGroupsAsync Tests

    /// <summary>
    /// Проверяет GetGroupingAsync с пагинацией без явного orderBy бросает исключение.
    /// </summary>
    [Fact]
    public async Task GetGroupingAsync_WithSkipTakeWithoutOrder_Throws()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "a"));
        context.Entities.Add(CreateEntity(name: "bb"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Expression<Func<TestEntityWithCreatedDeleted, int>> keySelector = e => e.Name.Length;

        // Act
        var act = () => repo.GetGroupingAsync(
            keySelector,
            skip: 0,
            take: 10,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("groupKeyOrderDirection");
    }

    /// <summary>
    /// Проверяет GetGroupingAsync с проекцией возвращает спроецированные группы.
    /// </summary>
    [Fact]
    public async Task GetGroupingAsync_WithSelector_ReturnsProjectedGroups()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        context.Entities.Add(CreateEntity(name: "a"));
        context.Entities.Add(CreateEntity(name: "aa"));
        context.Entities.Add(CreateEntity(name: "bbb"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Expression<Func<TestEntityWithCreatedDeleted, int>> keySelector = e => e.Name.Length;
        Expression<Func<IGrouping<int, TestEntityWithCreatedDeleted>, GroupSummary>> selector =
            g => new GroupSummary(g.Key, g.Count());

        // Act
        var result = await repo.GetGroupingAsync(
            keySelector,
            selector: selector,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEquivalentTo([
            new GroupSummary(1, 1),
            new GroupSummary(2, 1),
            new GroupSummary(3, 1)
        ]);
    }

    private sealed record GroupSummary(int Key, int Count);

    /// <summary>
    /// Проверяет CountGroupsAsync возвращает количество уникальных ключей группировки.
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
        var count = await repo.CountGroupsAsync(
            keySelector,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        count.Should().Be(5);
    }

    /// <summary>
    /// Проверяет CountGroupsAsync с фильтром учитывает только подходящие сущности.
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
        var count = await repo.CountGroupsAsync(
            keySelector,
            options,
            TestContext.Current.CancellationToken);

        // Assert
        count.Should().Be(2);
    }

    /// <summary>
    /// Проверяет CountGroupsAsync на пустой БД возвращает 0.
    /// </summary>
    [Fact]
    public async Task CountGroupsAsync_EmptyDb_ReturnsZero()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = CreateRepository(context);

        Expression<Func<TestEntityWithCreatedDeleted, int>> keySelector = e => e.Name.Length;

        // Act
        var count = await repo.CountGroupsAsync(
            keySelector,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        count.Should().Be(0);
    }

    #endregion

    #region Add/Remove edge cases

    /// <summary>
    /// <see cref="EfRepository{TEntity}.AddRangeAsync"/> пробрасывает userId и userName во все сущности,
    /// если они реализуют <see cref="IWithCreated"/>.
    /// </summary>
    [Fact]
    public async Task AddRangeAsync_PassesUserIdAndUserNameToAllEntities()
    {
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var entities = new[]
        {
            CreateEntity(name: "a"),
            CreateEntity(name: "b"),
            CreateEntity(name: "c"),
        };
        var userId = Guid.NewGuid();

        await repo.AddRangeAsync(entities, userId, "user", TestContext.Current.CancellationToken);

        entities.Should().AllSatisfy(e => e.CreatedByUserId.Should().Be(userId));
        entities.Should().AllSatisfy(e => e.CreatedByUserName.Should().Be("user"));
    }

    /// <summary>
    /// <see cref="EfRepository{TEntity}.RemoveAsync(TEntity, bool, CancellationToken)"/>
    /// (overload без userId) для IWithDeleted сущности выполняет soft delete и
    /// записывает DeletedByUserId = null.
    /// </summary>
    [Fact]
    public async Task RemoveAsync_OverloadWithoutUserId_PerformsSoftDeleteWithNullDeletedBy()
    {
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var entity = CreateEntity();
        context.Entities.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await repo.RemoveAsync(entity, hard: false, TestContext.Current.CancellationToken);

        entity.IsDeleted.Should().BeTrue();
        entity.DeletedByUserId.Should().BeNull();
    }

    /// <summary>
    /// <see cref="EfRepository{TEntity}.RemovePermanentRangeAsync"/> всегда удаляет физически,
    /// даже если сущность реализует IWithDeleted (не выполняет soft delete).
    /// </summary>
    [Fact]
    public async Task RemovePermanentRangeAsync_IgnoresIWithDeleted_AndHardDeletes()
    {
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var entity = CreateEntity();
        context.Entities.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await repo.RemovePermanentRangeAsync(
            [entity],
            TestContext.Current.CancellationToken);

        entity.IsDeleted.Should().BeFalse();
    }

    /// <summary>
    /// <see cref="EfRepository{TEntity}.AddAsync(TEntity, Guid?, string?, CancellationToken)"/>
    /// с userId=null и userName=null для IWithCreated сущности: OnCreate вызывается с null-ами,
    /// но DateCreated всё равно заполняется.
    /// </summary>
    [Fact]
    public async Task AddAsync_WithNullUserIdAndUserName_StillCallsOnCreateWithNulls()
    {
        await using var context = CreateContext();
        var repo = CreateRepository(context);
        var entity = CreateEntity();

        await repo.AddAsync(
            entity,
            userId: null,
            userName: null,
            cancellationToken: TestContext.Current.CancellationToken);

        entity.CreatedByUserId.Should().BeNull();
        entity.CreatedByUserName.Should().BeNull();
        entity.DateCreated.Should().NotBe(default(DateTime));
    }

    /// <summary>
    /// <see cref="EfRepository{TEntity}.GetAsync(object, QueryOptions{TEntity}?, CancellationToken)"/>
    /// с null id: контракт требует <see cref="ArgumentNullException"/> (ThrowIfNull).
    /// </summary>
    [Fact]
    public async Task GetAsync_WithNullId_ThrowsArgumentNullException()
    {
        await using var context = CreateContext();
        var repo = CreateRepository(context);

        var act = () => repo.GetAsync(id: null!, cancellationToken: TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// <see cref="EfRepository{TEntity}.GetAsync{TOut}(object, QueryOptions{TEntity}?, Expression{Func{TEntity, TOut}}?, CancellationToken)"/>
    /// с null id: контракт требует <see cref="ArgumentNullException"/> (ThrowIfNull).
    /// </summary>
    [Fact]
    public async Task GetAsyncWithOut_WithNullId_ThrowsArgumentNullException()
    {
        await using var context = CreateContext();
        var repo = CreateRepository(context);

        var act = () => repo.GetAsync<string>(id: null!, cancellationToken: TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion
}
