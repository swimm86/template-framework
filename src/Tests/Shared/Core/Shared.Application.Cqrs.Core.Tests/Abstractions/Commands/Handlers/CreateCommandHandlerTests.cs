using FluentValidation;
using Microsoft.AspNetCore.Http;
using Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;
using Shared.Testing.Doubles.Mapping;
using Shared.Testing.Doubles.Repository;
using Shared.Testing.Entities;

namespace Shared.Application.Cqrs.Core.Tests.Abstractions.Commands.Handlers;

public sealed class CreateCommandHandlerTests
{
    private static (
        TestCreateCommandHandler Handler,
        FakeMapper Mapper,
        FakeUnitOfWork UnitOfWork,
        FakeUserProvider UserProvider,
        FakeRepository<TestEntity> Repository)
        CreateSut()
    {
        var mapper = new FakeMapper();
        mapper.RegisterMap<object, TestEntity>(_ => new TestEntity { Id = Guid.NewGuid(), Name = "test" });
        mapper.RegisterMap<TestEntity, object>(e => new { e.Id, e.Name });

        var uow = new FakeUnitOfWork();
        var repo = uow.GetOrCreateRepository<TestEntity>();
        var userProvider = new FakeUserProvider();
        var loggerFactory = new FakeLoggerFactory();
        var validators = Array.Empty<IValidator<TestEntity>>();

        var handler = new TestCreateCommandHandler(loggerFactory, mapper, uow, validators, userProvider);

        return (handler, mapper, uow, userProvider, repo);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesEntityAndReturnsSuccess()
    {
        var (handler, mapper, uow, _, repo) = CreateSut();
        var command = new TestCreateCommand(new object());

        var result = await handler.Handle(command, CancellationToken.None);

        mapper.MapCallCount.Should().Be(2);
        repo.AddCallCount.Should().Be(1);
        uow.SaveChangesAsyncCallCount.Should().Be(1);
        result.Should().NotBeNull();
        result.Id.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ReturnsStatusCode201()
    {
        var (handler, _, _, _, _) = CreateSut();
        var command = new TestCreateCommand(new object());

        var result = await handler.Handle(command, CancellationToken.None);

        result.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    [Fact]
    public async Task Handle_ValidationFails_ThrowsValidationException()
    {
        var (handler, mapper, uow, userProvider, _) = CreateSut();
        mapper.RegisterMap<object, TestEntity>(_ => new TestEntity { Id = Guid.NewGuid(), Name = string.Empty });

        var validator = new InlineValidator<TestEntity>();
        validator.RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");

        var sut = new TestCreateCommandHandler(
            new FakeLoggerFactory(), mapper, uow, new[] { validator }, userProvider);
        var command = new TestCreateCommand(new object());

        var act = () => sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_MapperThrows_PropagatesException()
    {
        var (handler, mapper, uow, userProvider, _) = CreateSut();
        mapper.RegisterMap<object, TestEntity>(_ => throw new InvalidOperationException("Map failed"));

        var sut = new TestCreateCommandHandler(
            new FakeLoggerFactory(), mapper, uow, Array.Empty<IValidator<TestEntity>>(), userProvider);
        var command = new TestCreateCommand(new object());

        var act = () => sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Map failed");
    }
}
