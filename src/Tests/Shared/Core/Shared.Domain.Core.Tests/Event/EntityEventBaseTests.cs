using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Domain.Core.Tests.Event;

/// <summary>
/// Тесты для базового класса событий сущности, проверяющие включение/отключение и автоматическую деактивацию после обработки.
/// </summary>
public sealed class EntityEventBaseTests
{
    /// <summary>
    /// Проверяет, что при включённом событии ProcessAsync вызывает ProcessActionAsync.
    /// </summary>
    [Fact]
    public async Task ProcessAsync_WhenEnabled_CallsProcessActionAsync()
    {
        // Arrange
        var stub = new EntityEventBaseStub(TestEnum.BeforeCreate);
        stub.Enable();

        // Act
        await stub.ProcessAsync(DomainEventType.BeforeSave, null!, [], CancellationToken.None);

        // Assert
        stub.ProcessActionAsyncCalled.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что при отключённом событии ProcessAsync не вызывает ProcessActionAsync.
    /// </summary>
    [Fact]
    public async Task ProcessAsync_WhenDisabled_DoesNotCallProcessActionAsync()
    {
        // Arrange
        var stub = new EntityEventBaseStub(TestEnum.BeforeCreate);
        stub.Disable();

        // Act
        await stub.ProcessAsync(DomainEventType.BeforeSave, null!, [], CancellationToken.None);

        // Assert
        stub.ProcessActionAsyncCalled.Should().BeFalse();
    }

    /// <summary>
    /// Проверяет, что после первого вызова ProcessAsync событие автоматически отключается.
    /// </summary>
    [Fact]
    public async Task ProcessAsync_AfterFirstCall_AutoDisables()
    {
        // Arrange
        var stub = new EntityEventBaseStub(TestEnum.BeforeCreate);
        stub.Enable();

        // Act
        await stub.ProcessAsync(DomainEventType.BeforeSave, null!, [], CancellationToken.None);
        stub.ProcessActionAsyncCalled = false;

        await stub.ProcessAsync(DomainEventType.BeforeSave, null!, [], CancellationToken.None);

        // Assert
        stub.ProcessActionAsyncCalled.Should().BeFalse();
    }

    /// <summary>
    /// Проверяет, что ProcessAsync вызывает DisableEntitiesEvents с корректными параметрами.
    /// </summary>
    [Fact]
    public async Task ProcessAsync_CallsDisableEntitiesEvents()
    {
        // Arrange
        var stub = new EntityEventBaseStub(TestEnum.BeforeCreate);
        stub.Enable();

        // Act
        await stub.ProcessAsync(DomainEventType.AfterSave, null!, [], CancellationToken.None);

        // Assert
        stub.DisableEntitiesEventsCalled.Should().BeTrue();
        stub.LastEventType.Should().Be(DomainEventType.AfterSave);
        stub.LastEntities.Should().NotBeNull();
    }

    /// <summary>
    /// Проверяет, что вызов Enable повторно включает обработку после отключения.
    /// </summary>
    [Fact]
    public async Task Enable_ReenablesProcessing()
    {
        // Arrange
        var stub = new EntityEventBaseStub(TestEnum.BeforeCreate);
        stub.Disable();

        await stub.ProcessAsync(DomainEventType.BeforeSave, null!, [], CancellationToken.None);
        stub.ProcessActionAsyncCalled.Should().BeFalse();

        stub.Enable();
        stub.ProcessActionAsyncCalled = false;

        // Act
        await stub.ProcessAsync(DomainEventType.BeforeSave, null!, [], CancellationToken.None);

        // Assert
        stub.ProcessActionAsyncCalled.Should().BeTrue();
    }
}
