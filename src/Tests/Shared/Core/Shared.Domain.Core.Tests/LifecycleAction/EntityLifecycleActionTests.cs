using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Domain.Core.Tests.LifecycleAction;

/// <summary>
/// Тесты для доменных событий сущности, проверяющие передачу ServiceProvider и CancellationToken в делегат.
/// </summary>
public sealed class EntityLifecycleActionTests
{
    /// <summary>
    /// Проверяет, что ProcessActionAsync вызывает делегат с переданным ServiceProvider.
    /// </summary>
    [Fact]
    public async Task ProcessActionAsync_CallsDelegateWithServiceProvider()
    {
        // Arrange
        IServiceProvider? receivedServiceProvider = null;
        var stub = new EntityLifecycleActionStub(
            TestEnum.BeforeCreate,
            (sp, _) =>
            {
                receivedServiceProvider = sp;
                return Task.CompletedTask;
            });

        var serviceProvider = new TestServiceProvider();

        // Act
        await stub.CallExecuteActionAsync(serviceProvider, [], CancellationToken.None);

        // Assert
        receivedServiceProvider.Should().BeSameAs(serviceProvider);
    }

    /// <summary>
    /// Проверяет, что ProcessActionAsync вызывает делегат с переданным CancellationToken.
    /// </summary>
    [Fact]
    public async Task ProcessActionAsync_CallsDelegateWithCancellationToken()
    {
        // Arrange
        CancellationToken receivedToken = default;
        var stub = new EntityLifecycleActionStub(
            TestEnum.BeforeCreate,
            (_, ct) =>
            {
                receivedToken = ct;
                return Task.CompletedTask;
            });

        using var cts = new CancellationTokenSource();

        // Act
        await stub.CallExecuteActionAsync(null!, [], cts.Token);

        // Assert
        receivedToken.Should().Be(cts.Token);
    }

    /// <summary>
    /// Проверяет, что вызов DisableEntitiesActions не выбрасывает исключений.
    /// </summary>
    [Fact]
    public void DisableEntitiesActions_DoesNothing()
    {
        // Arrange
        var stub = new EntityLifecycleActionStub(TestEnum.BeforeCreate, (_, _) => Task.CompletedTask);

        // Act
        var act = () => stub.CallDisableEntitiesActions(LifecycleHookType.BeforeSave, []);

        // Assert
        act.Should().NotThrow();
    }
}
