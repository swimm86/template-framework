using Shared.Domain.Core.Interfaces;
using Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Domain.Core.Tests.Event;

public sealed class CustomDomainEventTests
{
    [Fact]
    public void Constructor_AssignsKey()
    {
        var domainEvent = new CustomDomainEventStub(TestEnum.AfterCreate, (_, _, _) => Task.CompletedTask);

        domainEvent.Key.Should().Be(TestEnum.AfterCreate);
    }

    [Fact]
    public async Task ProcessActionAsync_CallsConfiguredDelegate()
    {
        var called = false;
        var domainEvent = new CustomDomainEventStub(
            TestEnum.BeforeUpdate,
            (_, _, _) =>
            {
                called = true;
                return Task.CompletedTask;
            });

        await domainEvent.CallProcessActionAsync(null!, [], CancellationToken.None);

        called.Should().BeTrue();
    }

    [Fact]
    public void Key_MatchesConstructorParameter()
    {
        var expectedKey = TestEnum.AfterUpdate;

        var domainEvent = new CustomDomainEventStub(expectedKey, (_, _, _) => Task.CompletedTask);

        domainEvent.Key.Should().Be(expectedKey);
    }
}
