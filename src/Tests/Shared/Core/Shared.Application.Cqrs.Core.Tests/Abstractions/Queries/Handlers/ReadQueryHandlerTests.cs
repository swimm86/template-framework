using Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;
using Shared.Domain.Core.Exceptions.Models;
using Shared.Testing.Doubles.Logging;
using Shared.Testing.Doubles.Mapping;
using Shared.Testing.Doubles.Repository;
using Shared.Testing.Entities;

namespace Shared.Application.Cqrs.Core.Tests.Abstractions.Queries.Handlers;

public sealed class ReadQueryHandlerTests
{
    private static FakeLoggerFactory CreateLoggerFactory() => new();

    [Fact]
    public async Task Handle_EntityFound_MapsToDtoAndReturns()
    {
        var mapper = new FakeMapper();
        mapper.RegisterMap<TestEntity, TestEntity>(e => e);

        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "test-entity" };
        var repository = new FakeRepository<TestEntity>();
        repository.AddDirect(entity);

        var unitOfWork = new FakeUnitOfWork();
        unitOfWork.GetOrCreateRepository<TestEntity>().AddDirect(entity);

        var sut = new TestReadQueryHandler(CreateLoggerFactory(), mapper, unitOfWork);
        var query = new TestReadByKeyQuery(entity.Id);

        var result = await sut.Handle(query, CancellationToken.None);

        mapper.MapCallCount.Should().Be(1);
        result.Should().Be(entity);
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToGetAsync()
    {
        CancellationToken? capturedToken = null;
        var callbackUow = new CallbackUnitOfWork(ct => capturedToken = ct);

        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "test-entity" };
        var repository = (CallbackRepository<TestEntity>)callbackUow.GetRepository<TestEntity>();
        repository.Inner.AddDirect(entity);

        var mapper = new FakeMapper();
        mapper.RegisterMap<TestEntity, TestEntity>(e => e);

        var sut = new TestReadQueryHandler(CreateLoggerFactory(), mapper, callbackUow);
        var query = new TestReadByKeyQuery(entity.Id);

        using var cts = new CancellationTokenSource();
        await sut.Handle(query, cts.Token);

        // TODO BUG (#4): GetAsync called without cancellationToken
        capturedToken.Should().Be(cts.Token, "GetAsync should receive the CancellationToken from Handle");
    }

    [Fact]
    public async Task Handle_GuardAsyncOverrideThrows_Propagates()
    {
        var mapper = new FakeMapper();
        var unitOfWork = new FakeUnitOfWork();
        var expectedException = new InvalidOperationException("guard-failed");

        var sut = new GuardThrowingReadQueryHandler(CreateLoggerFactory(), mapper, unitOfWork, expectedException);
        var query = new TestReadByKeyQuery("any-key");

        var act = () => sut.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("guard-failed");
    }

    [Fact]
    public async Task Handle_EntityNotFound_ThrowsNotFoundException()
    {
        var mapper = new FakeMapper();
        mapper.RegisterMap<TestEntity, TestEntity>(e => e);

        var repository = new FakeRepository<TestEntity>();
        var unitOfWork = new FakeUnitOfWork();
        unitOfWork.GetOrCreateRepository<TestEntity>();

        var sut = new TestReadQueryHandler(CreateLoggerFactory(), mapper, unitOfWork);
        var query = new TestReadByKeyQuery("nonexistent-key");

        var act = () => sut.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
