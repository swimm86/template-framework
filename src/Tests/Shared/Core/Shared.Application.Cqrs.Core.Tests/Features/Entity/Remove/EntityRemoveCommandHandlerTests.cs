using Shared.Application.Cqrs.Core.Features.Entity.Remove;
using Shared.Application.Cqrs.Core.Features.Entity.Remove.Request;
using Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;
using Shared.Testing.Doubles.Repository;
using Shared.Testing.Entities;

namespace Shared.Application.Cqrs.Core.Tests.Features.Entity.Remove;

public sealed class EntityRemoveCommandHandlerTests
{
    private static (
        EntityRemoveCommandHandler<TestEntity> Handler,
        FakeUnitOfWork UnitOfWork,
        FakeUserProvider UserProvider,
        FakeRepository<TestEntity> Repository)
        CreateSut()
    {
        var uow = new FakeUnitOfWork();
        var repo = uow.GetOrCreateRepository<TestEntity>();
        var userProvider = new FakeUserProvider();
        var loggerFactory = new FakeLoggerFactory();

        var handler = new EntityRemoveCommandHandler<TestEntity>(uow, loggerFactory, userProvider);

        return (handler, uow, userProvider, repo);
    }

    [Fact]
    public async Task Handle_RemovesEntityByKey()
    {
        var (handler, uow, _, repo) = CreateSut();
        var entityId = Guid.NewGuid();
        repo.AddDirect(new TestEntity { Id = entityId });

        var command = new EntityRemoveCommand<TestEntity>(new EntityRemoveRequest { Id = entityId });

        await handler.Handle(command, CancellationToken.None);

        repo.RemoveCallCount.Should().Be(1);
        uow.SaveChangesAsyncCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ForwardsCancellationToken()
    {
        var (handler, uow, _, repo) = CreateSut();
        var entityId = Guid.NewGuid();
        repo.AddDirect(new TestEntity { Id = entityId });

        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var command = new EntityRemoveCommand<TestEntity>(new EntityRemoveRequest { Id = entityId });

        await handler.Handle(command, token);

        uow.LastSaveChangesCancellationToken.Should().Be(token);
    }
}
