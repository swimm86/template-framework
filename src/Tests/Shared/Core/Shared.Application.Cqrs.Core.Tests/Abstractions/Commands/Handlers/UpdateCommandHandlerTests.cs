using FluentValidation;
using Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;
using Shared.Domain.Core.Exceptions.Models;
using Shared.Testing.Doubles.Mapping;
using Shared.Testing.Doubles.Repository;
using Shared.Testing.Entities;

namespace Shared.Application.Cqrs.Core.Tests.Abstractions.Commands.Handlers;

public sealed class UpdateCommandHandlerTests
{
    private static (
        TestUpdateCommandHandler Handler,
        FakeMapper Mapper,
        FakeUnitOfWork UnitOfWork,
        FakeUserProvider UserProvider,
        FakeRepository<TestEntity> Repository)
        CreateSut()
    {
        var mapper = new FakeMapper();
        mapper.RegisterMap<TestEntity, object>(e => new { e.Id, e.Name });

        var uow = new FakeUnitOfWork();
        var repo = uow.GetOrCreateRepository<TestEntity>();
        var userProvider = new FakeUserProvider();
        var loggerFactory = new FakeLoggerFactory();
        var validators = Array.Empty<IValidator<TestEntity>>();

        var handler = new TestUpdateCommandHandler(loggerFactory, mapper, uow, validators, userProvider);

        return (handler, mapper, uow, userProvider, repo);
    }

    [Fact]
    public async Task Handle_EntityFound_UpdatesEntity()
    {
        var (handler, mapper, uow, userProvider, repo) = CreateSut();
        var entityId = Guid.NewGuid();
        var entity = new TestEntity { Id = entityId, Name = "original" };
        repo.AddDirect(entity);

        var command = new TestUpdateCommand(entityId, new object());

        await handler.Handle(command, CancellationToken.None);

        mapper.MapInPlaceCallCount.Should().Be(1);
        entity.UpdatedByUserId.Should().Be(userProvider.UserId);
        uow.SaveChangesAsyncCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_EntityNotFound_ThrowsNotFoundException()
    {
        var (handler, _, _, _, _) = CreateSut();
        var command = new TestUpdateCommand(Guid.NewGuid(), new object());

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public void Handle_ConstructOptions_ReturnsWithTrackingTrue()
    {
        var (handler, _, _, _, _) = CreateSut();
        var command = new TestUpdateCommand(Guid.NewGuid(), new object());

        var options = handler.ConstructOptions(command);

        options.WithTracking.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToSaveChanges()
    {
        var (handler, _, uow, _, repo) = CreateSut();
        var entityId = Guid.NewGuid();
        repo.AddDirect(new TestEntity { Id = entityId, Name = "test" });

        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var command = new TestUpdateCommand(entityId, new object());

        await handler.Handle(command, token);

        uow.LastSaveChangesCancellationToken.Should().Be(token);
    }

    [Fact]
    public async Task Handle_ReturnsCorrectResponse()
    {
        var (handler, _, _, _, repo) = CreateSut();
        var entityId = Guid.NewGuid();
        repo.AddDirect(new TestEntity { Id = entityId, Name = "updated" });

        var command = new TestUpdateCommand(entityId, new object());

        var result = await handler.Handle(command, CancellationToken.None);

        result.Key.Should().Be(entityId);
        result.StatusCode.Should().Be(200);
        result.Payload.Should().NotBeNull();
    }
}
