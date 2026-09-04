// ----------------------------------------------------------------------------------------------
// <copyright file="JobCorrelationContextConcurrencyTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.CorrelationId;

namespace Shared.Application.Core.Tests.CorrelationId;

/// <summary>
/// Concurrency-тесты <see cref="JobCorrelationContext"/>: проверка изоляции <see cref="AsyncLocal{T}"/>
/// между параллельными задачами и корректности распространения по <see cref="System.Threading.ExecutionContext"/>.
/// </summary>
public sealed class JobCorrelationContextConcurrencyTests
{
    private const int TaskCount = 10;

    private const int ReaderCount = 100;

    /// <summary>
    /// Параллельные задачи, каждая из которых вызывает <see cref="JobCorrelationContext.TrySetCorrelationId"/>
    /// и затем читает значение через <see cref="JobCorrelationContext.GetCorrelationId"/>;
    /// каждая задача должна видеть установленный идентификатор, и все идентификаторы должны быть попарно различны.
    /// </summary>
    [Fact]
    public async Task TrySetCorrelationId_ConcurrentTasks_EachSeesOwnValue()
    {
        // Arrange
        var tasks = new Task<Guid?>[TaskCount];

        // Act
        for (var i = 0; i < TaskCount; i++)
        {
            tasks[i] = Task.Run(
                () =>
                {
                    JobCorrelationContext.TrySetCorrelationId();
                    return JobCorrelationContext.GetCorrelationId();
                },
                TestContext.Current.CancellationToken);
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().NotContainNulls();
        results.Should().OnlyHaveUniqueItems();
    }

    /// <summary>
    /// В рамке без <see cref="JobCorrelationContext.TrySetCorrelationId"/> метод
    /// <see cref="JobCorrelationContext.GetCorrelationId"/> возвращает <see langword="null"/>.
    /// </summary>
    [Fact]
    public void GetCorrelationId_NoSetInScope_ReturnsNull()
    {
        // Arrange
        JobCorrelationContext.ClearCorrelationId();

        // Act
        var actual = JobCorrelationContext.GetCorrelationId();

        // Assert
        actual.Should().BeNull();
    }

    /// <summary>
    /// Последовательные <see cref="JobCorrelationContext.TrySetCorrelationId"/> и
    /// <see cref="JobCorrelationContext.ClearCorrelationId"/> в одной рамке переключают значение
    /// с <see langword="null"/> на установленный идентификатор и обратно.
    /// </summary>
    [Fact]
    public void TrySetCorrelationId_AndClear_TogglesValue()
    {
        // Arrange
        JobCorrelationContext.ClearCorrelationId();

        // Act
        var beforeSet = JobCorrelationContext.GetCorrelationId();
        var firstSetResult = JobCorrelationContext.TrySetCorrelationId();
        var afterSet = JobCorrelationContext.GetCorrelationId();
        var secondSetResult = JobCorrelationContext.TrySetCorrelationId();
        JobCorrelationContext.ClearCorrelationId();
        var afterClear = JobCorrelationContext.GetCorrelationId();

        // Assert
        beforeSet.Should().BeNull();
        firstSetResult.Should().BeTrue();
        afterSet.Should().NotBeNull();
        secondSetResult.Should().BeFalse();
        afterClear.Should().BeNull();
    }

    /// <summary>
    /// Установка идентификатора в родительской задаче видна дочерним задачам, запущенным через
    /// <see cref="Task.Run(System.Action)"/>; значение у всех читателей согласовано и не искажено.
    /// </summary>
    [Fact]
    public async Task TrySetCorrelationId_ExecutionContextFlow_ReadersSeeParentValue()
    {
        // Arrange
        JobCorrelationContext.ClearCorrelationId();
        JobCorrelationContext.TrySetCorrelationId();
        var expected = JobCorrelationContext.GetCorrelationId();
        expected.Should().NotBeNull();

        var readers = new Task<Guid?>[ReaderCount];

        // Act
        for (var i = 0; i < ReaderCount; i++)
        {
            readers[i] = Task.Run(
                () => JobCorrelationContext.GetCorrelationId(),
                TestContext.Current.CancellationToken);
        }

        var results = await Task.WhenAll(readers);

        // Assert
        results.Should().NotContainNulls();
        results.Should().NotContain(Guid.Empty);
        results.Should().AllSatisfy(actual => actual.Should().Be(expected));
    }

    /// <summary>
    /// Параллельные записи из разных задач изолированы: каждая задача видит свой собственный
    /// идентификатор, и собранная коллекция содержит ровно <see cref="TaskCount"/> попарно различных значений.
    /// </summary>
    [Fact]
    public async Task TrySetCorrelationId_ConcurrentWrites_EachTaskHoldsUniqueValue()
    {
        // Arrange
        JobCorrelationContext.ClearCorrelationId();
        var tasks = new Task<Guid?>[TaskCount];

        // Act
        for (var i = 0; i < TaskCount; i++)
        {
            tasks[i] = Task.Run(
                async () =>
                {
                    JobCorrelationContext.TrySetCorrelationId();
                    await Task.Yield();
                    return JobCorrelationContext.GetCorrelationId();
                },
                TestContext.Current.CancellationToken);
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().NotContainNulls();
        results.Should().NotContain(Guid.Empty);
        results.Should().HaveCount(TaskCount);
        results.Should().OnlyHaveUniqueItems();
    }
}
