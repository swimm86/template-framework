using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Domain.Core.Tests.Event;

public sealed class EntityEventBaseTests
{
    [Fact]
    public async Task ProcessAsync_WhenEnabled_CallsProcessActionAsync()
    {
        var stub = new EntityEventBaseStub(TestEnum.BeforeCreate);
        stub.Enable();

        await stub.ProcessAsync(DomainEventType.BeforeSave, null!, [], CancellationToken.None);

        stub.ProcessActionAsyncCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessAsync_WhenDisabled_DoesNotCallProcessActionAsync()
    {
        var stub = new EntityEventBaseStub(TestEnum.BeforeCreate);
        stub.Disable();

        await stub.ProcessAsync(DomainEventType.BeforeSave, null!, [], CancellationToken.None);

        stub.ProcessActionAsyncCalled.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessAsync_AfterFirstCall_AutoDisables()
    {
        var stub = new EntityEventBaseStub(TestEnum.BeforeCreate);
        stub.Enable();

        await stub.ProcessAsync(DomainEventType.BeforeSave, null!, [], CancellationToken.None);
        stub.ProcessActionAsyncCalled = false;

        await stub.ProcessAsync(DomainEventType.BeforeSave, null!, [], CancellationToken.None);

        stub.ProcessActionAsyncCalled.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessAsync_CallsDisableEntitiesEvents()
    {
        var stub = new EntityEventBaseStub(TestEnum.BeforeCreate);
        stub.Enable();

        await stub.ProcessAsync(DomainEventType.AfterSave, null!, [], CancellationToken.None);

        stub.DisableEntitiesEventsCalled.Should().BeTrue();
        stub.LastEventType.Should().Be(DomainEventType.AfterSave);
        stub.LastEntities.Should().NotBeNull();
    }

    [Fact]
    public async Task Enable_ReenablesProcessing()
    {
        var stub = new EntityEventBaseStub(TestEnum.BeforeCreate);
        stub.Disable();

        await stub.ProcessAsync(DomainEventType.BeforeSave, null!, [], CancellationToken.None);
        stub.ProcessActionAsyncCalled.Should().BeFalse();

        stub.Enable();
        stub.ProcessActionAsyncCalled = false;

        await stub.ProcessAsync(DomainEventType.BeforeSave, null!, [], CancellationToken.None);

        stub.ProcessActionAsyncCalled.Should().BeTrue();
    }
}
