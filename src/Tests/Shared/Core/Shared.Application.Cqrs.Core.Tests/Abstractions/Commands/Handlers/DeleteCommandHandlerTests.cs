using Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;
using Shared.Domain.Core.Exceptions.Models;
using Shared.Testing.Doubles.Repository;
using Shared.Testing.Entities;

namespace Shared.Application.Cqrs.Core.Tests.Abstractions.Commands.Handlers;

public sealed class DeleteCommandHandlerTests
{
    private static (
        TestDeleteCommandHandler Handler,
        FakeUnitOfWork UnitOfWork,
        FakeUserProvider UserProvider,
        FakeRepository<TestEntity> Repository)
        CreateSut()
    {
        var uow = new FakeUnitOfWork();
        var repo = uow.GetOrCreateRepository<TestEntity>();
        var userProvider = new FakeUserProvider();
        var loggerFactory = new FakeLoggerFactory();

        var handler = new TestDeleteCommandHandler(uow, loggerFactory, userProvider);

        return (handler, uow, userProvider, repo);
    }

    [Fact]
    public async Task Handle_EntityFound_CallsRemoveAndSaveChanges()
    {
        var (handler, uow, _, repo) = CreateSut();
        var entityId = Guid.NewGuid();
        repo.AddDirect(new TestEntity { Id = entityId });

        var command = new TestDeleteCommand(entityId);

        var result = await handler.Handle(command, CancellationToken.None);

        repo.RemoveCallCount.Should().Be(1);
        uow.SaveChangesAsyncCallCount.Should().Be(1);
        result.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Handle_EntityNotFound_ThrowsNotFoundException()
    {
        var (handler, _, _, _) = CreateSut();
        var command = new TestDeleteCommand(Guid.NewGuid());

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToSaveChanges()
    {
        // TODO BUG (#2): SaveChangesAsync(default) instead of cancellationToken
        var (handler, uow, _, repo) = CreateSut();
        var entityId = Guid.NewGuid();
        repo.AddDirect(new TestEntity { Id = entityId });

        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var command = new TestDeleteCommand(entityId);

        await handler.Handle(command, token);

        uow.LastSaveChangesCancellationToken.Should().Be(token);
    }
}
