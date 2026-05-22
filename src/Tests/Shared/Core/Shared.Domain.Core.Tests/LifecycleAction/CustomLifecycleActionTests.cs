using Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Domain.Core.Tests.LifecycleAction;

/// <summary>
/// Тесты для пользовательских доменных событий, проверяющие корректность конструктора и вызова делегатов.
/// </summary>
public sealed class CustomLifecycleActionTests
{
    /// <summary>
    /// Проверяет, что конструктор присваивает переданный ключ свойству Key.
    /// </summary>
    [Fact]
    public void Constructor_AssignsKey()
    {
        // Arrange

        // Act
        var lifecycleAction = new CustomLifecycleActionStub(TestEnum.AfterCreate, (_, _, _) => Task.CompletedTask);

        // Assert
        lifecycleAction.Key.Should().Be(TestEnum.AfterCreate);
    }

    /// <summary>
    /// Проверяет, что ProcessActionAsync вызывает сконфигурированный делегат.
    /// </summary>
    [Fact]
    public async Task ProcessActionAsync_CallsConfiguredDelegate()
    {
        // Arrange
        var called = false;
        var lifecycleAction = new CustomLifecycleActionStub(
            TestEnum.BeforeUpdate,
            (_, _, _) =>
            {
                called = true;
                return Task.CompletedTask;
            });

        // Act
        await lifecycleAction.CallExecuteActionAsync(null!, [], CancellationToken.None);

        // Assert
        called.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что свойство Key соответствует значению, переданному в конструктор.
    /// </summary>
    [Fact]
    public void Key_MatchesConstructorParameter()
    {
        // Arrange
        var expectedKey = TestEnum.AfterUpdate;

        // Act
        var lifecycleAction = new CustomLifecycleActionStub(expectedKey, (_, _, _) => Task.CompletedTask);

        // Assert
        lifecycleAction.Key.Should().Be(expectedKey);
    }
}
