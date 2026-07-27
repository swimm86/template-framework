// ----------------------------------------------------------------------------------------------
// <copyright file="FakeRepositoryConcurrencyTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using FluentAssertions;
using Shared.Testing.Builders;
using Shared.Testing.Entities;
using Xunit;

namespace Shared.Testing.Doubles.Repository;

/// <summary>
/// Тесты локализации race-condition в <see cref="FakeRepository{TEntity}"/>.
/// </summary>
/// <remarks>
/// Цель — зафиксировать наличие или отсутствие гонок в concurrent-сценариях.
/// Никаких правок production-кода: только наблюдение за поведением.
/// </remarks>
public sealed class FakeRepositoryConcurrencyTests
{
    private const int TotalWriters = 100;
    private const int TotalReaders = 50;
    private const int TotalMixedOps = 100;

    /// <summary>
    /// Параллельные вызовы <c>AddAsync</c> из 100 задач сохраняют все 100 entities.
    /// </summary>
    [Fact]
    public async Task Add_ConcurrentCallsFromManyTasks_AllEntitiesAdded()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = unitOfWork.GetOrCreateRepository<TestEntity>();
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var tasks = Enumerable.Range(0, TotalWriters)
            .Select(_ => repo.AddAsync(
                TestEntityBuilder.Valid(),
                cancellationToken: cancellationToken))
            .ToArray();
        await Task.WhenAll(tasks);

        // Assert
        repo.Items.Count.Should().Be(TotalWriters);
    }

    /// <summary>
    /// Конкурентные <c>AddAsync</c> (writers) и <c>GetRangeAsync</c> (readers)
    /// не приводят к corrupted state.
    /// </summary>
    [Fact]
    public async Task Add_And_Get_Concurrent_RaceConditionOnEntities()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = unitOfWork.GetOrCreateRepository<TestEntity>();
        var cancellationToken = TestContext.Current.CancellationToken;
        var readErrors = new ConcurrentBag<Exception>();

        // Act
        var writers = Enumerable.Range(0, TotalWriters)
            .Select(_ => Task.Run(
                async () => await repo.AddAsync(
                    TestEntityBuilder.Valid(),
                    cancellationToken: cancellationToken),
                cancellationToken))
            .ToArray();

        var readers = Enumerable.Range(0, TotalReaders)
            .Select(_ => Task.Run(
                async () =>
                {
                    try
                    {
                        var snapshot = await repo.GetRangeAsync(cancellationToken: cancellationToken);
                        _ = snapshot.Count;
                    }
                    catch (Exception ex)
                    {
                        readErrors.Add(ex);
                    }
                },
                cancellationToken))
            .ToArray();

        await Task.WhenAll(readers);
        await Task.WhenAll(writers);

        // Assert
        readErrors.Should().BeEmpty("параллельное чтение через GetRangeAsync не должно бросать");
        repo.Items.Count.Should().Be(TotalWriters);
    }

    /// <summary>
    /// Параллельные <c>AddRangeAsync</c> из 10 задач сохраняют все 100 уникальных entities
    /// без дубликатов ключей.
    /// </summary>
    [Fact]
    public async Task AddRange_ConcurrentFromMultipleTasks_NoDuplicateCorruption()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = unitOfWork.GetOrCreateRepository<TestEntity>();
        var cancellationToken = TestContext.Current.CancellationToken;
        const int taskCount = 10;
        const int entitiesPerTask = 10;
        var totalEntities = taskCount * entitiesPerTask;

        // Act
        var tasks = Enumerable.Range(0, taskCount)
            .Select(_ => Task.Run(
                async () =>
                {
                    var batch = Enumerable.Range(0, entitiesPerTask)
                        .Select(__ => TestEntityBuilder.Valid())
                        .ToArray();
                    await repo.AddRangeAsync(batch, cancellationToken: cancellationToken);
                },
                cancellationToken))
            .ToArray();
        await Task.WhenAll(tasks);

        // Assert
        repo.Items.Count.Should().Be(totalEntities);
    }

    /// <summary>
    /// Параллельные <c>AddAsync</c> и <c>RemoveAsync</c> сохраняют целостность storage.
    /// </summary>
    [Fact]
    public async Task Remove_ConcurrentWith_Add_NoCorruptedState()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = unitOfWork.GetOrCreateRepository<TestEntity>();
        var cancellationToken = TestContext.Current.CancellationToken;

        var existing = Enumerable.Range(0, TotalMixedOps)
            .Select(_ => TestEntityBuilder.Valid())
            .ToArray();
        foreach (var entity in existing)
        {
            await repo.AddAsync(entity, cancellationToken: cancellationToken);
        }

        var newEntities = Enumerable.Range(0, TotalMixedOps)
            .Select(_ => TestEntityBuilder.Valid())
            .ToArray();

        // Act
        var mixed = Enumerable.Range(0, TotalMixedOps * 2)
            .Select(i => Task.Run(
                async () =>
                {
                    if (i % 2 == 0)
                    {
                        await repo.AddAsync(
                            newEntities[i / 2],
                            cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await repo.RemoveAsync(
                            existing[i / 2],
                            hard: true,
                            cancellationToken: cancellationToken);
                    }
                },
                cancellationToken))
            .ToArray();
        await Task.WhenAll(mixed);

        // Assert
        var finalCount = repo.Items.Count;
        finalCount.Should().Be(TotalMixedOps,
            "100 предзагруженных + 100 новых − 100 удалённых = 100 entities");
    }

    /// <summary>
    /// Параллельные <c>GetRangeAsync</c> не приводят к исключениям или data corruption.
    /// </summary>
    [Fact]
    public async Task GetRange_Concurrent_NoExceptionLeak()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = unitOfWork.GetOrCreateRepository<TestEntity>();
        var cancellationToken = TestContext.Current.CancellationToken;
        const int seedCount = 10;
        const int readerCount = 100;

        for (var i = 0; i < seedCount; i++)
        {
            await repo.AddAsync(
                TestEntityBuilder.Valid(),
                cancellationToken: cancellationToken);
        }

        var errors = new ConcurrentBag<Exception>();

        // Act
        var tasks = Enumerable.Range(0, readerCount)
            .Select(_ => Task.Run(
                async () =>
                {
                    try
                    {
                        var items = await repo.GetRangeAsync(cancellationToken: cancellationToken);
                        _ = items.Count;
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex);
                    }
                },
                cancellationToken))
            .ToArray();
        await Task.WhenAll(tasks);

        // Assert
        errors.Should().BeEmpty();
        repo.Items.Count.Should().Be(seedCount);
    }

    /// <summary>
    /// Параллельные снимки <c>Items</c> и модификации не приводят к inconsistent state
    /// и не бросают исключений, связанных с concurrent modification.
    /// </summary>
    [Fact]
    public async Task Snapshot_ConcurrentWith_Modifications_NoInconsistentState()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = unitOfWork.GetOrCreateRepository<TestEntity>();
        var cancellationToken = TestContext.Current.CancellationToken;
        const int writerCount = 50;
        const int snapshotCount = 50;

        var errors = new ConcurrentBag<Exception>();

        // Act
        var writers = Enumerable.Range(0, writerCount)
            .Select(_ => Task.Run(
                async () =>
                {
                    try
                    {
                        await repo.AddAsync(
                            TestEntityBuilder.Valid(),
                            cancellationToken: cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex);
                    }
                },
                cancellationToken))
            .ToArray();

        var snapshots = Enumerable.Range(0, snapshotCount)
            .Select(_ => Task.Run(
                () =>
                {
                    try
                    {
                        var snapshot = repo.Items;
                        _ = snapshot.Count;
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex);
                    }
                },
                cancellationToken))
            .ToArray();

        await Task.WhenAll(snapshots);
        await Task.WhenAll(writers);

        // Assert
        errors.Should().BeEmpty(
            "параллельные Items-снимки и Add не должны бросать исключений");
        repo.Items.Count.Should().Be(writerCount);
    }
}
