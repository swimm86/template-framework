// ----------------------------------------------------------------------------------------------
// <copyright file="CacheUpdateJobTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Job.Cache;

namespace Shared.Application.Core.Tests.Job;

/// <summary>
/// Тесты <see cref="CacheUpdateJob{TData}"/>: ленивая загрузка, кэширование и обновление данных.
/// </summary>
public sealed class CacheUpdateJobTests
{
    /// <summary>
    /// При первом вызове <see cref="CacheUpdateJob{TData}.GetCacheDataAsync"/> данные загружаются
    /// через <see cref="CacheUpdateJob{TData}.ExecuteAsync"/>, вызывающий
    /// <see cref="CacheUpdateJob{TData}.UpdateDataAsync"/>.
    /// </summary>
    [Fact]
    public async Task GetCacheDataAsync_FirstCall_InvokesUpdateDataAsync()
    {
        // Arrange
        var expected = "hello";
        var job = new TestCacheUpdateJob(expected);

        // Act
        var result = await job.GetCacheDataAsync();

        // Assert
        result.Should().Be(expected);
        job.UpdateDataCallCount.Should().Be(1);
    }

    /// <summary>
    /// При повторном вызове <see cref="CacheUpdateJob{TData}.GetCacheDataAsync"/> возвращаются
    /// закэшированные данные без повторного вызова
    /// <see cref="CacheUpdateJob{TData}.UpdateDataAsync"/>.
    /// </summary>
    [Fact]
    public async Task GetCacheDataAsync_SecondCall_ReturnsCachedDataWithoutReinvokingUpdate()
    {
        // Arrange
        var job = new TestCacheUpdateJob("hello");

        // Act
        var first = await job.GetCacheDataAsync();
        var second = await job.GetCacheDataAsync();

        // Assert
        first.Should().Be("hello");
        second.Should().Be("hello");
        job.UpdateDataCallCount.Should().Be(1);
    }

    /// <summary>
    /// <see cref="CacheUpdateJob{TData}.ExecuteAsync"/> обновляет кэш через
    /// <see cref="CacheUpdateJob{TData}.UpdateDataAsync"/> и последующий вызов
    /// <see cref="CacheUpdateJob{TData}.GetCacheDataAsync"/> возвращает свежие данные.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_RefreshesCache_GetCacheDataReturnsUpdatedValue()
    {
        // Arrange
        var job = new IncrementalTestCacheUpdateJob();

        // Act
        var first = await job.GetCacheDataAsync();
        await job.ExecuteAsync(CancellationToken.None);
        var second = await job.GetCacheDataAsync();

        // Assert
        first.Should().Be("v1");
        second.Should().Be("v2");
        job.UpdateDataCallCount.Should().Be(2);
    }

    /// <summary>
    /// <see cref="CacheUpdateJob{TData}.ExecuteAsync"/> пробрасывает
    /// <see cref="CancellationToken"/> в
    /// <see cref="CacheUpdateJob{TData}.UpdateDataAsync"/>.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_PassesCancellationTokenToUpdateDataAsync()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var job = new TokenAwareTestCacheUpdateJob();

        // Act
        await job.ExecuteAsync(cts.Token);

        // Assert
        job.ReceivedToken.Should().Be(cts.Token);
        job.UpdateDataCallCount.Should().Be(1);
    }

    /// <summary>
    /// После прямого вызова <see cref="CacheUpdateJob{TData}.ExecuteAsync"/> без вызова
    /// <see cref="CacheUpdateJob{TData}.GetCacheDataAsync"/>, кэш уже заполнен и последующий вызов
    /// <see cref="CacheUpdateJob{TData}.GetCacheDataAsync"/> возвращает данные без дополнительного
    /// вызова <see cref="CacheUpdateJob{TData}.UpdateDataAsync"/>.
    /// </summary>
    [Fact]
    public async Task GetCacheDataAsync_AfterExecuteAsync_ReturnsDataWithoutReinvokingUpdate()
    {
        // Arrange
        var job = new TestCacheUpdateJob("cached");

        // Act
        await job.ExecuteAsync(CancellationToken.None);
        var result = await job.GetCacheDataAsync();

        // Assert
        result.Should().Be("cached");
        job.UpdateDataCallCount.Should().Be(1);
    }

    /// <summary>
    /// Повторные вызовы <see cref="CacheUpdateJob{TData}.ExecuteAsync"/> каждый раз обновляют
    /// кэш — последующий <see cref="CacheUpdateJob{TData}.GetCacheDataAsync"/> возвращает
    /// результат последнего обновления.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_MultipleCalls_AlwaysUpdatesCache()
    {
        // Arrange
        var job = new IncrementalTestCacheUpdateJob();

        // Act
        await job.ExecuteAsync(CancellationToken.None);
        await job.ExecuteAsync(CancellationToken.None);
        await job.ExecuteAsync(CancellationToken.None);
        var result = await job.GetCacheDataAsync();

        // Assert
        result.Should().Be("v3");
        job.UpdateDataCallCount.Should().Be(3);
    }

    /// <summary>
    /// Параллельные вызовы <see cref="CacheUpdateJob{TData}.GetCacheDataAsync"/>
    /// не приводят к множественным вызовам <see cref="CacheUpdateJob{TData}.UpdateDataAsync"/> —
    /// <c>Lazy&lt;Task&lt;TData&gt;&gt;</c> гарантирует, что фабрика выполняется лишь однажды.
    /// </summary>
    [Fact]
    public async Task GetCacheDataAsync_ParallelCalls_InvokesUpdateDataAsyncOnce()
    {
        // Arrange
        var job = new DelayedTestCacheUpdateJob(TimeSpan.FromMilliseconds(50));

        // Act
        var results = await Task.WhenAll(
            Enumerable.Range(0, 10).Select(_ => job.GetCacheDataAsync()));

        // Assert
        results.Should().AllSatisfy(r => r.Should().Be("delayed"));
        job.UpdateDataCallCount.Should().Be(1, "Lazy гарантирует однократное выполнение фабрики");
    }

    /// <summary>
    /// Конкретная реализация <see cref="CacheUpdateJob{TData}"/> для тестов с предопределённым
    /// строковым значением и подсчётом вызовов.
    /// </summary>
    private sealed class TestCacheUpdateJob(string value)
        : CacheUpdateJob<string>
    {
        public int UpdateDataCallCount { get; private set; }

        protected override Task<string> UpdateDataAsync(CancellationToken cancellationToken = default)
        {
            UpdateDataCallCount++;
            return Task.FromResult(value);
        }
    }

    /// <summary>
    /// Конкретная реализация <see cref="CacheUpdateJob{TData}"/> для тестов с инкрементальным
    /// строковым значением — каждый вызов возвращает строку с номером на 1 больше.
    /// </summary>
    private sealed class IncrementalTestCacheUpdateJob
        : CacheUpdateJob<string>
    {
        private int _counter;

        public int UpdateDataCallCount { get; private set; }

        protected override Task<string> UpdateDataAsync(CancellationToken cancellationToken = default)
        {
            UpdateDataCallCount++;
            _counter++;
            return Task.FromResult($"v{_counter}");
        }
    }

    /// <summary>
    /// Конкретная реализация <see cref="CacheUpdateJob{TData}"/> для тестов, сохраняющая
    /// переданный <see cref="CancellationToken"/>.
    /// </summary>
    private sealed class TokenAwareTestCacheUpdateJob
        : CacheUpdateJob<string>
    {
        public CancellationToken ReceivedToken { get; private set; }
        public int UpdateDataCallCount { get; private set; }

        protected override Task<string> UpdateDataAsync(CancellationToken cancellationToken = default)
        {
            UpdateDataCallCount++;
            ReceivedToken = cancellationToken;
            return Task.FromResult(string.Empty);
        }
    }

    /// <summary>
    /// Конкретная реализация <see cref="CacheUpdateJob{TData}"/> для тестов race condition:
    /// возвращает значение с задержкой, чтобы параллельные вызовы
    /// <see cref="CacheUpdateJob{TData}.GetCacheDataAsync"/> конкурировали за фабрику <c>Lazy</c>.
    /// </summary>
    private sealed class DelayedTestCacheUpdateJob(TimeSpan delay)
        : CacheUpdateJob<string>
    {
        public int UpdateDataCallCount { get; private set; }

        protected override async Task<string> UpdateDataAsync(CancellationToken cancellationToken = default)
        {
            UpdateDataCallCount++;
            await Task.Delay(delay, cancellationToken);
            return "delayed";
        }
    }
}
