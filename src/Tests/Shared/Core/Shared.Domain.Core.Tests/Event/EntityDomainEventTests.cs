using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;
using Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Domain.Core.Tests.Event;

public sealed class EntityDomainEventTests
{
    [Fact]
    public async Task ProcessActionAsync_CallsDelegateWithServiceProvider()
    {
        IServiceProvider? receivedServiceProvider = null;
        var stub = new EntityDomainEventStub(
            TestEnum.BeforeCreate,
            (sp, _) =>
            {
                receivedServiceProvider = sp;
                return Task.CompletedTask;
            });

        var serviceProvider = new TestServiceProvider();
        await stub.CallProcessActionAsync(serviceProvider, [], CancellationToken.None);

        receivedServiceProvider.Should().BeSameAs(serviceProvider);
    }

    [Fact]
    public async Task ProcessActionAsync_CallsDelegateWithCancellationToken()
    {
        CancellationToken receivedToken = default;
        var stub = new EntityDomainEventStub(
            TestEnum.BeforeCreate,
            (_, ct) =>
            {
                receivedToken = ct;
                return Task.CompletedTask;
            });

        using var cts = new CancellationTokenSource();
        await stub.CallProcessActionAsync(null!, [], cts.Token);

        receivedToken.Should().Be(cts.Token);
    }

    [Fact]
    public void DisableEntitiesEvents_DoesNothing()
    {
        var stub = new EntityDomainEventStub(TestEnum.BeforeCreate, (_, _) => Task.CompletedTask);

        var act = () => stub.CallDisableEntitiesEvents(DomainEventType.BeforeSave, []);

        act.Should().NotThrow();
    }
}
