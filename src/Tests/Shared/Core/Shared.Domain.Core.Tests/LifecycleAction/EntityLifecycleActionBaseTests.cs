using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Domain.Core.Tests.LifecycleAction;

/// <summary>
/// Тесты для базового класса событий сущности, проверяющие включение/отключение и автоматическую деактивацию после обработки.
/// </summary>
public sealed class EntityLifecycleActionBaseTests
{
    /// <summary>
    /// Проверяет, что при включённом событии ExecuteAsync вызывает ProcessActionAsync.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenEnabled_CallsProcessActionAsync()
    {
        // Arrange
        var stub = new EntityLifecycleActionBaseStub(TestEnum.BeforeCreate);
        stub.Enable();

        // Act
        await stub.ExecuteAsync(LifecycleHookType.BeforeSave, null!, [], CancellationToken.None);

        // Assert
        stub.ProcessActionAsyncCalled.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что при отключённом событии ExecuteAsync не вызывает ProcessActionAsync.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenDisabled_DoesNotCallExecuteActionAsync()
    {
        // Arrange
        var stub = new EntityLifecycleActionBaseStub(TestEnum.BeforeCreate);
        stub.Disable();

        // Act
        await stub.ExecuteAsync(LifecycleHookType.BeforeSave, null!, [], CancellationToken.None);

        // Assert
        stub.ProcessActionAsyncCalled.Should().BeFalse();
    }

    /// <summary>
    /// Проверяет, что после первого вызова ExecuteAsync событие автоматически отключается.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_AfterFirstCall_AutoDisables()
    {
        // Arrange
        var stub = new EntityLifecycleActionBaseStub(TestEnum.BeforeCreate);
        stub.Enable();

        // Act
        await stub.ExecuteAsync(LifecycleHookType.BeforeSave, null!, [], CancellationToken.None);
        stub.ProcessActionAsyncCalled = false;

        await stub.ExecuteAsync(LifecycleHookType.BeforeSave, null!, [], CancellationToken.None);

        // Assert
        stub.ProcessActionAsyncCalled.Should().BeFalse();
    }

    /// <summary>
    /// Проверяет, что ExecuteAsync вызывает DisableEntitiesActions с корректными параметрами.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_CallsDisableEntitiesActions()
    {
        // Arrange
        var stub = new EntityLifecycleActionBaseStub(TestEnum.BeforeCreate);
        stub.Enable();

        // Act
        await stub.ExecuteAsync(LifecycleHookType.AfterSave, null!, [], CancellationToken.None);

        // Assert
        stub.DisableEntitiesActionsCalled.Should().BeTrue();
        stub.LastHookType.Should().Be(LifecycleHookType.AfterSave);
        stub.LastEntities.Should().NotBeNull();
    }

    /// <summary>
    /// Проверяет, что вызов Enable повторно включает обработку после отключения.
    /// </summary>
    [Fact]
    public async Task Enable_ReenablesProcessing()
    {
        // Arrange
        var stub = new EntityLifecycleActionBaseStub(TestEnum.BeforeCreate);
        stub.Disable();

        await stub.ExecuteAsync(LifecycleHookType.BeforeSave, null!, [], CancellationToken.None);
        stub.ProcessActionAsyncCalled.Should().BeFalse();

        stub.Enable();
        stub.ProcessActionAsyncCalled = false;

        // Act
        await stub.ExecuteAsync(LifecycleHookType.BeforeSave, null!, [], CancellationToken.None);

        // Assert
        stub.ProcessActionAsyncCalled.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что Disable() и DisableEntitiesActions() вызываются даже когда действие отключено.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenDisabled_StillCallsDisableAndDisableEntitiesActions()
    {
        // Arrange
        var stub = new EntityLifecycleActionBaseStub(TestEnum.BeforeCreate);
        stub.Disable();

        // Act
        await stub.ExecuteAsync(LifecycleHookType.AfterSave, null!, [], CancellationToken.None);

        // Assert
        stub.ProcessActionAsyncCalled.Should().BeFalse();
        stub.DisableEntitiesActionsCalled.Should().BeTrue();
        stub.LastHookType.Should().Be(LifecycleHookType.AfterSave);
    }
}