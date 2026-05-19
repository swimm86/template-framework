// ----------------------------------------------------------------------------------------------
// <copyright file="LinqExtensionTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Common.Extensions;

namespace Shared.Common.Tests.Extensions;

/// <summary>
/// Тесты для класса расширения LINQ <see cref="LinqExtension"/>.
/// </summary>
public sealed class LinqExtensionTests
{
    #region ForEach Tests

    /// <summary>
    /// Проверяет выполнение <see cref="LinqExtension.ForEach{T}"/> над пустой коллекцией.
    /// </summary>
    [Fact]
    public void ForEach_EmptyCollection_DoesNotInvokeAction()
    {
        // Arrange
        var items = Array.Empty<int>();
        var invocations = 0;

        // Act
        items.ForEach(_ => invocations++);

        // Assert
        invocations.Should().Be(0);
    }

    /// <summary>
    /// Проверяет выполнение <see cref="LinqExtension.ForEach{T}"/> над коллекцией из одного элемента.
    /// </summary>
    [Fact]
    public void ForEach_SingleElement_InvokesActionOnce()
    {
        // Arrange
        var items = new[] { 42 };
        var invocations = 0;
        var captured = 0;

        // Act
        items.ForEach(i =>
        {
            invocations++;
            captured = i;
        });

        // Assert
        invocations.Should().Be(1);
        captured.Should().Be(42);
    }

    /// <summary>
    /// Проверяет выполнение <see cref="LinqExtension.ForEach{T}"/> над коллекцией из нескольких элементов.
    /// </summary>
    [Fact]
    public void ForEach_MultipleElements_InvokesActionForEach()
    {
        // Arrange
        var items = new[] { 1, 2, 3, 4, 5 };
        var processed = new List<int>();

        // Act
        items.ForEach(processed.Add);

        // Assert
        processed.Should().Equal(1, 2, 3, 4, 5);
    }

    /// <summary>
    /// Проверяет, что ForEach выбрасывает <see cref="ArgumentNullException"/> при null-action.
    /// </summary>
    [Fact]
    public void ForEach_NullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var items = new[] { 1, 2, 3 };
        Action<int>? action = null;

        // Act
        var act = () => items.ForEach(action!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region ForeachAsync Tests

    /// <summary>
    /// Проверяет асинхронное выполнение над коллекцией из нескольких элементов.
    /// </summary>
    [Fact]
    public async Task ForeachAsync_MultipleElements_ProcessesEachItem()
    {
        // Arrange
        var items = new[] { 1, 2, 3 };
        var processed = new List<int>();

        // Act
        await items.ForeachAsync(async i =>
        {
            await Task.Yield();
            processed.Add(i);
        }, TestContext.Current.CancellationToken);

        // Assert
        processed.Should().Equal(1, 2, 3);
    }

    /// <summary>
    /// Проверяет асинхронное выполнение над пустой коллекцией.
    /// </summary>
    [Fact]
    public async Task ForeachAsync_EmptyCollection_DoesNotInvokeFunc()
    {
        // Arrange
        var items = Array.Empty<int>();
        var invocations = 0;

        // Act
        await items.ForeachAsync(_ =>
        {
            invocations++;
            return Task.CompletedTask;
        }, TestContext.Current.CancellationToken);

        // Assert
        invocations.Should().Be(0);
    }

    /// <summary>
    /// Проверяет, что ForeachAsync выбрасывает <see cref="ArgumentNullException"/> при null-func.
    /// </summary>
    [Fact]
    public async Task ForeachAsync_NullFunc_ThrowsArgumentNullException()
    {
        // Arrange
        var items = new[] { 1, 2, 3 };
        Func<int, Task>? func = null;

        // Act
        var act = async () => await items.ForeachAsync(func!, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// Проверяет, что ForeachAsync выбрасывает <see cref="OperationCanceledException"/> при отменённом токене.
    /// </summary>
    [Fact]
    public async Task ForeachAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var items = new[] { 1, 2, 3 };
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
#pragma warning disable xUnit1051
        var act = async () => await items.ForeachAsync(_ => Task.CompletedTask, cts.Token);
#pragma warning restore xUnit1051

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region ForEachAsync Tests (IAsyncEnumerable)

    /// <summary>
    /// Проверяет асинхронное выполнение над <see cref="IAsyncEnumerable{T}"/>.
    /// </summary>
    [Fact]
    public async Task ForEachAsync_AsyncEnumerable_ProcessesEachItem()
    {
        // Arrange
        var processed = new List<int>();

        static async IAsyncEnumerable<int> GetItems()
        {
            await Task.Yield();
            yield return 1;
            yield return 2;
            yield return 3;
        }

        // Act
        await GetItems().ForEachAsync(async i =>
        {
            await Task.Yield();
            processed.Add(i);
        }, TestContext.Current.CancellationToken);

        // Assert
        processed.Should().Equal(1, 2, 3);
    }

    /// <summary>
    /// Проверяет, что ForEachAsync для IAsyncEnumerable выбрасывает <see cref="ArgumentNullException"/> при null-func.
    /// </summary>
    [Fact]
    public async Task ForEachAsync_AsyncEnumerable_NullFunc_ThrowsArgumentNullException()
    {
        // Arrange
        Func<int, Task>? func = null;

        static async IAsyncEnumerable<int> GetItems()
        {
            yield return 1;
        }

        // Act
        var act = async () => await GetItems().ForEachAsync(func!, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion
}
