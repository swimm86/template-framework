using Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;
using Shared.Testing.Doubles.Repository;
using Shared.Testing.Entities;

namespace Shared.Application.Cqrs.Core.Tests.Abstractions;

public sealed class EntityRequestHandlerTests
{
    private static FakeLoggerFactory LoggerFactory => new();
    private static FakeUnitOfWork UnitOfWork => new();

    [Fact]
    public void Repository_CallsUnitOfWorkGetRepository()
    {
        var uow = new CountingUnitOfWork();
        var handler = new TestEntityRequestHandler(uow, LoggerFactory);

        _ = handler.Repository;

        uow.GetRepositoryCallCount.Should().Be(1);
    }

    [Fact]
    public void Repository_DoubleAccess_CallsGetRepositoryTwice()
    {
        var uow = new CountingUnitOfWork();
        var handler = new TestEntityRequestHandler(uow, LoggerFactory);

        _ = handler.Repository;
        _ = handler.Repository;

        uow.GetRepositoryCallCount.Should().Be(2);
    }

    [Fact]
    public void ConstructOptions_DefaultWithTracking_IsFalse()
    {
        var handler = new TestEntityRequestHandler(UnitOfWork, LoggerFactory);

        var options = handler.ConstructOptions(new GetTestEntityRequest());

        options.WithTracking.Should().BeFalse();
    }

    [Fact]
    public void ConstructOptions_DefaultAsSplitQuery_IsFalse()
    {
        var handler = new TestEntityRequestHandler(UnitOfWork, LoggerFactory);

        var options = handler.ConstructOptions(new GetTestEntityRequest());

        options.AsSplitQuery.Should().BeFalse();
    }

    [Fact]
    public void ConstructOptions_OverriddenWithTracking_IsTrue()
    {
        var handler = new TestEntityRequestHandlerWithTracking(UnitOfWork, LoggerFactory);

        var options = handler.ConstructOptions(new GetTestEntityRequest());

        options.WithTracking.Should().BeTrue();
    }

    [Fact]
    public void ConstructOptions_OverriddenAsSplitQuery_IsTrue()
    {
        var handler = new TestEntityRequestHandlerWithSplitQuery(UnitOfWork, LoggerFactory);

        var options = handler.ConstructOptions(new GetTestEntityRequest());

        options.AsSplitQuery.Should().BeTrue();
    }

    [Fact]
    public void ConstructOptions_IWithDeletedEntity_AddsIsDeletedFilter()
    {
        var handler = new TestEntityRequestHandler(UnitOfWork, LoggerFactory);

        var options = handler.ConstructOptions(new GetTestEntityRequest());

        options.Filters.Should().HaveCount(1);
    }

    [Fact]
    public void ConstructOptions_NonIWithDeletedEntity_NoFilter()
    {
        var handler = new TestEntityWithoutDeletedHandler(UnitOfWork, LoggerFactory);

        var options = handler.ConstructOptions(new GetTestEntityWithoutDeletedRequest());

        options.Filters.Should().BeEmpty();
    }

    [Fact]
    public void ConstructOptions_WithDeletableTrue_SkipsIsDeletedFilter()
    {
        var handler = new TestEntityRequestHandler(UnitOfWork, LoggerFactory);

        var options = handler.ConstructOptions(new GetTestEntityRequest(), withDeletable: true);

        options.Filters.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessEntityAsync_DefaultIsNoOp_OverrideIsCalled()
    {
        var handler = new TestEntityRequestHandlerWithProcessEntity(UnitOfWork, LoggerFactory);

        await handler.CallProcessEntityAsync(
            new TestEntity(),
            new GetTestEntityRequest(),
            TestContext.Current.CancellationToken);

        handler.ProcessEntityCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessResponseAsync_DefaultIsNoOp_OverrideIsCalled()
    {
        var handler = new TestEntityRequestHandlerWithProcessResponse(UnitOfWork, LoggerFactory);

        await handler.CallProcessResponseAsync(
            new TestEntity(),
            new GetTestEntityRequest(),
            TestContext.Current.CancellationToken);

        handler.ProcessResponseCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessEntitiesAsync_DefaultIsNoOp_OverrideIsCalled()
    {
        var handler = new TestEntityRequestHandlerWithProcessEntities(UnitOfWork, LoggerFactory);

        await handler.CallProcessEntitiesAsync(
            new List<TestEntity> { new() },
            new GetTestEntityRequest(),
            TestContext.Current.CancellationToken);

        handler.ProcessEntitiesCalled.Should().BeTrue();
    }
}
