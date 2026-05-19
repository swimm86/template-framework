using FluentValidation;
using Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;
using Shared.Domain.Core.Exceptions.Models;
using Shared.Testing.Doubles.Mapping;
using Shared.Testing.Doubles.Repository;
using Shared.Testing.Entities;

namespace Shared.Application.Cqrs.Core.Tests.Abstractions.Commands.Handlers;

public sealed class CloneCommandHandlerTests
{
    private static (
        TestCloneCommandHandler Handler,
        FakeMapper Mapper,
        FakeUnitOfWork UnitOfWork,
        FakeUserProvider UserProvider,
        FakeRepository<TestEntity> Repository)
        CreateSut()
    {
        var mapper = new FakeMapper();
        mapper.RegisterMap<TestEntity, TestEntity>(e => new TestEntity { Id = Guid.NewGuid(), Name = $"Clone of {e.Name}" });
        mapper.RegisterMap<TestEntity, object>(e => new { e.Id, e.Name });

        var uow = new FakeUnitOfWork();
        var repo = uow.GetOrCreateRepository<TestEntity>();
        var userProvider = new FakeUserProvider();
        var loggerFactory = new FakeLoggerFactory();
        var validators = Array.Empty<IValidator<TestEntity>>();

        var handler = new TestCloneCommandHandler(mapper, uow, loggerFactory, validators, userProvider);

        return (handler, mapper, uow, userProvider, repo);
    }

    [Fact]
    public async Task Handle_EntityFound_ClonesViaMapperAndAdds()
    {
        var (handler, mapper, uow, _, repo) = CreateSut();
        var entityId = Guid.NewGuid();
        repo.AddDirect(new TestEntity { Id = entityId, Name = "source" });

        var command = new TestCloneCommand(entityId, new object());

        var result = await handler.Handle(command, CancellationToken.None);

        mapper.MapCallCount.Should().Be(2);
        repo.AddCallCount.Should().Be(1);
        uow.SaveChangesAsyncCallCount.Should().Be(1);
        result.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Handle_EntityNotFound_ThrowsNotFoundException()
    {
        var (handler, _, _, _, _) = CreateSut();
        var command = new TestCloneCommand(Guid.NewGuid(), new object());

        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_PropagatesCancellationToken()
    {
        var (handler, _, uow, _, repo) = CreateSut();
        var entityId = Guid.NewGuid();
        repo.AddDirect(new TestEntity { Id = entityId, Name = "source" });

        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var command = new TestCloneCommand(entityId, new object());

        await handler.Handle(command, token);

        uow.LastSaveChangesCancellationToken.Should().Be(token);
    }
}
