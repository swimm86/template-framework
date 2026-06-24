// ----------------------------------------------------------------------------------------------
// <copyright file="EfQueryEvaluatorTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Core.Dal;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Infrastructure.Dal.EFCore.Repository;
using Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;
using Shared.Testing.Doubles.Mapping;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Repository;

/// <summary>Тесты для класса <see cref="EfQueryEvaluator"/>.</summary>
public sealed class EfQueryEvaluatorTests
{
    private static EfQueryEvaluator CreateEvaluator() => new(new FakeMapper());

    private static TestEfRepositoryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestEfRepositoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new TestEfRepositoryDbContext(options);
    }

    private static TestEntityWithCreatedDeleted CreateEntity(Guid? id = null, string name = "test")
    {
        return new TestEntityWithCreatedDeleted
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
        };
    }

    #region Build Tests

    /// <summary>Проверяет что Build с null опциями возвращает исходный IQueryable.</summary>
    [Fact]
    public void Build_NullOptions_ReturnsOriginalQueryable()
    {
        // Arrange
        using var context = CreateContext();
        var evaluator = CreateEvaluator();
        var queryable = context.Entities.AsQueryable();

        // Act
        var result = evaluator.Build(queryable, options: null);

        // Assert
        result.Should().BeSameAs(queryable);
    }

    /// <summary>Проверяет что Build с фильтром применяет фильтрацию.</summary>
    [Fact]
    public void Build_WithFilter_AppliesFilter()
    {
        // Arrange
        using var context = CreateContext();
        var evaluator = CreateEvaluator();
        context.Entities.Add(CreateEntity(name: "alpha"));
        context.Entities.Add(CreateEntity(name: "beta"));
        context.SaveChanges();

        var options = new QueryOptions<TestEntityWithCreatedDeleted>();
        options.AddFilter(e => e.Name == "alpha");
        var queryable = context.Entities.AsQueryable();

        // Act
        var result = evaluator.Build(queryable, options);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("alpha");
    }

    /// <summary>Проверяет что Build с несколькими фильтрами применяет все фильтры.</summary>
    [Fact]
    public void Build_WithMultipleFilters_AppliesAllFilters()
    {
        // Arrange
        using var context = CreateContext();
        var evaluator = CreateEvaluator();
        context.Entities.Add(CreateEntity(name: "alpha-1"));
        context.Entities.Add(CreateEntity(name: "alpha-2"));
        context.Entities.Add(CreateEntity(name: "beta"));
        context.SaveChanges();

        var options = new QueryOptions<TestEntityWithCreatedDeleted>();
        options.AddFilter(e => e.Name.StartsWith("alpha"));
        options.AddFilter(e => e.Name.Contains("1"));
        var queryable = context.Entities.AsQueryable();

        // Act
        var result = evaluator.Build(queryable, options);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("alpha-1");
    }

    /// <summary>Проверяет что Build с WithTracking=false применяет AsNoTracking.</summary>
    [Fact]
    public void Build_WithTrackingFalse_AppliesAsNoTracking()
    {
        // Arrange
        using var context = CreateContext();
        var evaluator = CreateEvaluator();
        var entity = CreateEntity();
        context.Entities.Add(entity);
        context.SaveChanges();

        var options = new QueryOptions<TestEntityWithCreatedDeleted>(withTracking: false);
        var queryable = context.Entities.AsQueryable();

        // Act
        var result = evaluator.Build(queryable, options).ToList();

        // Assert
        result.Should().HaveCount(1);
        context.Entry(result[0]).State.Should().Be(EntityState.Detached);
    }

    /// <summary>Проверяет что Build с OrderBy применяет сортировку.</summary>
    [Fact]
    public void Build_WithOrderBy_AppliesOrdering()
    {
        // Arrange
        using var context = CreateContext();
        var evaluator = CreateEvaluator();
        context.Entities.Add(CreateEntity(name: "zebra"));
        context.Entities.Add(CreateEntity(name: "alpha"));
        context.Entities.Add(CreateEntity(name: "middle"));
        context.SaveChanges();

        var options = new QueryOptions<TestEntityWithCreatedDeleted>();
        options.AddOrderBy(e => e.Name, OrderDirectionType.Ascending);
        var queryable = context.Entities.AsQueryable();

        // Act
        var result = evaluator.Build(queryable, options);

        // Assert
        result.First().Name.Should().Be("alpha");
        result.Last().Name.Should().Be("zebra");
    }

    /// <summary>Проверяет что Build с несколькими OrderBy применяет ThenBy.</summary>
    [Fact]
    public void Build_WithOrderByMultiple_AppliesThenBy()
    {
        // Arrange
        using var context = CreateContext();
        var evaluator = CreateEvaluator();
        context.Entities.Add(CreateEntity(name: "b"));
        context.Entities.Add(CreateEntity(name: "a"));
        context.Entities.Add(CreateEntity(name: "a"));
        context.SaveChanges();

        var options = new QueryOptions<TestEntityWithCreatedDeleted>();
        options.AddOrderBy(e => e.Name, OrderDirectionType.Ascending);
        options.AddOrderBy(e => e.Id, OrderDirectionType.Descending);
        var queryable = context.Entities.AsQueryable();

        // Act
        var result = evaluator.Build(queryable, options).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].Name.Should().Be("a");
        result[1].Name.Should().Be("a");
        result[0].Id.CompareTo(result[1].Id).Should().BeGreaterThan(0);
    }

    /// <summary>Проверяет что Build с AsSplitQuery применяет AsSplitQuery.</summary>
    [Fact]
    public void Build_WithSplitQuery_AppliesAsSplitQuery()
    {
        // Arrange
        using var context = CreateContext();
        var evaluator = CreateEvaluator();
        context.Entities.Add(CreateEntity(name: "test"));
        context.SaveChanges();

        var options = new QueryOptions<TestEntityWithCreatedDeleted>(asSplitQuery: true);
        options.AddOrderBy(e => e.Id, OrderDirectionType.Ascending);
        var queryable = context.Entities.AsQueryable();

        // Act
        var result = evaluator.Build(queryable, options);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }

    /// <summary>
    /// Проверяет что Build с AsSplitQuery без OrderBy И с Includes добавляет сортировку по Id.
    /// Ветка срабатывает только при условии: AsSplitQuery=true И OrderBy пустой И Includes.Any()=true.
    /// </summary>
    [Fact]
    public void Build_WithSplitQueryNoOrderByAndIncludes_AddsDefaultOrderByById()
    {
        // Arrange — use TestIncludeDbContext that has navigation properties
        var includeOptions = new DbContextOptionsBuilder<TestIncludeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestIncludeDbContext(includeOptions);

        var id1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var id2 = Guid.Parse("00000000-0000-0000-0000-000000000002");

        context.Parents.Add(new TestParentEntity { Id = id1, Name = "first" });
        context.Parents.Add(new TestParentEntity { Id = id2, Name = "second" });
        context.SaveChanges();

        var evaluator = CreateEvaluator();

        var options = new QueryOptions<TestParentEntity>(asSplitQuery: true);
        // Add an Include so that options.Includes.Any() == true — this triggers the default OrderBy
        options.AddInclude<TestChildEntity?>(e => e.Child);

        var queryable = context.Parents.AsQueryable();

        // Act
        var result = evaluator.Build(queryable, options).ToList();

        // Assert — default OrderBy by Id ascending was applied
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(id1, "default OrderBy ascending by Id is applied");
        result[1].Id.Should().Be(id2);

        // OrderBy was added to options (not empty anymore)
        options.OrderBy.Should().HaveCount(1, "default order was injected into options");
    }

    /// <summary>
    /// Проверяет что Build с AsSplitQuery без OrderBy и без Includes НЕ добавляет сортировку.
    /// </summary>
    [Fact]
    public void Build_WithSplitQueryNoOrderByAndNoIncludes_DoesNotAddDefaultOrderBy()
    {
        // Arrange
        using var context = CreateContext();
        var evaluator = CreateEvaluator();
        var entity = CreateEntity(name: "test");
        context.Entities.Add(entity);
        context.SaveChanges();

        var options = new QueryOptions<TestEntityWithCreatedDeleted>(asSplitQuery: true);
        // No includes added — default OrderBy must NOT be injected
        var queryable = context.Entities.AsQueryable();

        // Act
        evaluator.Build(queryable, options);

        // Assert — OrderBy should still be empty (condition was not met)
        options.OrderBy.Should().BeEmpty("default order is only added when Includes.Any() is true");
    }

    /// <summary>Проверяет что Build с CustomQueryBeforeProcesses применяет пользовательское преобразование.</summary>
    [Fact]
    public void Build_WithCustomQueryBeforeProcesses_AppliesCustomQuery()
    {
        // Arrange
        using var context = CreateContext();
        var evaluator = CreateEvaluator();
        context.Entities.Add(CreateEntity(name: "visible"));
        context.Entities.Add(CreateEntity(name: "hidden"));
        context.SaveChanges();

        var options = new QueryOptions<TestEntityWithCreatedDeleted>();
        options.CustomQueryBeforeProcesses.Add(q => q.Where(e => e.Name == "visible"));
        var queryable = context.Entities.AsQueryable();

        // Act
        var result = evaluator.Build(queryable, options);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("visible");
    }

    /// <summary>Проверяет что Build с CustomQueryPostProcesses применяет пользовательское пост-преобразование.</summary>
    [Fact]
    public void Build_WithCustomQueryPostProcesses_AppliesCustomQuery()
    {
        // Arrange
        using var context = CreateContext();
        var evaluator = CreateEvaluator();
        context.Entities.Add(CreateEntity(name: "keep"));
        context.Entities.Add(CreateEntity(name: "remove"));
        context.SaveChanges();

        var options = new QueryOptions<TestEntityWithCreatedDeleted>();
        options.CustomQueryPostProcesses.Add(q => q.Where(e => e.Name == "keep"));
        var queryable = context.Entities.AsQueryable();

        // Act
        var result = evaluator.Build(queryable, options);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("keep");
    }

    /// <summary>
    /// Проверяет что Build с DistinctBy группирует по булевому предикату
    /// и берёт первую сущность из каждой группы.
    /// DistinctBy = e =&gt; e.Name == "alpha" создаёт две группы (true/false),
    /// возвращает по одной сущности из каждой — итого 2 результата.
    /// </summary>
    [Fact]
    public void Build_WithDistinctBy_GroupsByPredicateAndSelectsFirst()
    {
        // Arrange
        using var context = CreateContext();
        var evaluator = CreateEvaluator();
        context.Entities.Add(CreateEntity(name: "alpha"));
        context.Entities.Add(CreateEntity(name: "alpha"));
        context.Entities.Add(CreateEntity(name: "beta"));
        context.SaveChanges();

        var options = new QueryOptions<TestEntityWithCreatedDeleted>();
        options.DistinctBy = e => e.Name == "alpha";
        var queryable = context.Entities.AsQueryable();

        // Act
        var result = evaluator.Build(queryable, options).ToList();

        // Assert — one from the "alpha" group (true) and one from the "beta" group (false)
        result.Should().HaveCount(2);
    }

    /// <summary>
    /// Проверяет что Build с DistinctBy при одной группе возвращает одну сущность.
    /// </summary>
    [Fact]
    public void Build_WithDistinctBy_AllSameGroup_ReturnsSingleResult()
    {
        // Arrange
        using var context = CreateContext();
        var evaluator = CreateEvaluator();
        context.Entities.Add(CreateEntity(name: "alpha"));
        context.Entities.Add(CreateEntity(name: "alpha"));
        context.SaveChanges();

        var options = new QueryOptions<TestEntityWithCreatedDeleted>();
        options.DistinctBy = e => e.Name == "alpha";
        var queryable = context.Entities.AsQueryable();

        // Act
        var result = evaluator.Build(queryable, options).ToList();

        // Assert — only one group (all true), one result
        result.Should().HaveCount(1);
    }

    #endregion

    #region BuildWithTransform Tests

    /// <summary>Проверяет что BuildWithTransform с postBuildProcess применяет промежуточное преобразование.</summary>
    [Fact]
    public void BuildWithTransform_WithPostBuildProcess_AppliesTransformation()
    {
        // Arrange
        using var context = CreateContext();
        var evaluator = CreateEvaluator();
        context.Entities.Add(CreateEntity(name: "test-entity"));
        context.SaveChanges();

        var options = new QueryOptions<TestEntityWithCreatedDeleted>();
        var queryable = context.Entities.AsQueryable();
        Func<IQueryable<TestEntityWithCreatedDeleted>, IQueryable<string>> postBuildProcess =
            q => q.Select(e => e.Name.ToUpper());

        // Act
        var result = evaluator.BuildWithTransform<TestEntityWithCreatedDeleted, string, string>(
            queryable,
            postBuildProcess,
            options);

        // Assert
        result.Should().ContainSingle("TEST-ENTITY");
    }

    #endregion


}
