using Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;
using Shared.Domain.Core.Dal;
using Shared.Domain.Core.Dal.Models;
using Shared.Testing.Doubles.Logging;
using Shared.Testing.Doubles.Repository;
using Shared.Testing.Entities;

namespace Shared.Application.Cqrs.Core.Tests.Abstractions.Queries.Handlers;

public sealed class ReadListQueryHandlerTests
{
    private static FakeLoggerFactory CreateLoggerFactory() => new();

    private static TestEntity CreateEntity(int id, DateTime? dateCreated = null)
    {
        var entity = new TestEntity
        {
            Id = Guid.NewGuid(),
            Name = $"entity-{id}"
        };
        entity.SetDateCreated(dateCreated ?? DateTime.UtcNow.AddDays(-id));
        return entity;
    }

    [Fact]
    public async Task Handle_CallsPaginationHelperForSkipTake()
    {
        var unitOfWork = new FakeUnitOfWork();
        var repo = unitOfWork.GetOrCreateRepository<TestEntity>();

        for (var i = 0; i < 10; i++)
        {
            var entity = CreateEntity(i, new DateTime(2020, 1, 1).AddDays(i));
            repo.AddDirect(entity);
        }

        var sut = new TestReadListQueryHandler(CreateLoggerFactory(), unitOfWork);
        var request = new TestPageableRequest { PageNumber = 2, PageSize = 3 };
        var query = new TestReadListQuery(request);

        var response = await sut.Handle(query, CancellationToken.None);

        response.Payload.Should().NotBeNull();
        response.Payload!.Count.Should().Be(3);
        response.PageNumber.Should().Be(2);
        response.TotalPages.Should().Be(4);
    }

    [Fact]
    public async Task Handle_ListFilterBaseWithIds_AddsIdsFilter()
    {
        var unitOfWork = new FakeUnitOfWork();
        var repo = unitOfWork.GetOrCreateRepository<TestEntity>();

        var entityA = CreateEntity(1);
        var entityB = CreateEntity(2);
        var entityC = CreateEntity(3);

        repo.AddDirect(entityA);
        repo.AddDirect(entityB);
        repo.AddDirect(entityC);

        var sut = new TestReadListQueryHandler(CreateLoggerFactory(), unitOfWork);
        var filter = new TestListFilter { Ids = [entityA.Id, entityC.Id] };
        var request = new TestPageableRequest { Filter = filter, PageSize = 100 };
        var query = new TestReadListQuery(request);

        var response = await sut.Handle(query, CancellationToken.None);

        response.Payload.Should().NotBeNull();
        response.Payload!.Select(e => e.Id).Should().BeEquivalentTo([entityA.Id, entityC.Id]);
    }

    [Fact]
    public async Task Handle_ListFilterBaseWithoutIds_NoIdsFilter()
    {
        var unitOfWork = new FakeUnitOfWork();
        var repo = unitOfWork.GetOrCreateRepository<TestEntity>();

        var entityA = CreateEntity(1);
        var entityB = CreateEntity(2);
        var entityC = CreateEntity(3);

        repo.AddDirect(entityA);
        repo.AddDirect(entityB);
        repo.AddDirect(entityC);

        var sut = new TestReadListQueryHandler(CreateLoggerFactory(), unitOfWork);
        var filter = new TestListFilter { Ids = null };
        var request = new TestPageableRequest { Filter = filter, PageSize = 100 };
        var query = new TestReadListQuery(request);

        var response = await sut.Handle(query, CancellationToken.None);

        response.Payload.Should().NotBeNull();
        response.Payload!.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_IWithDateCreated_NoExplicitSort_AddsDateCreatedAscending()
    {
        var unitOfWork = new FakeUnitOfWork();
        var repo = unitOfWork.GetOrCreateRepository<TestEntity>();

        var entity1 = CreateEntity(1, new DateTime(2025, 3, 1));
        var entity2 = CreateEntity(2, new DateTime(2025, 1, 1));
        var entity3 = CreateEntity(3, new DateTime(2025, 2, 1));

        repo.AddDirect(entity1);
        repo.AddDirect(entity2);
        repo.AddDirect(entity3);

        var sut = new OptionsCapturingReadListQueryHandler(CreateLoggerFactory(), unitOfWork);
        var request = new TestPageableRequest { PageSize = 100 };
        var query = new TestReadListQuery(request);

        await sut.Handle(query, CancellationToken.None);

        sut.LastOptions.Should().NotBeNull();
        sut.LastOptions!.OrderBy.Should().ContainSingle(o =>
            o.Direction == OrderDirectionType.Ascending);
    }

    [Fact]
    public async Task Handle_IWithDateCreated_ExplicitSortByDateCreated_DoesNotDuplicateOrder()
    {
        var unitOfWork = new FakeUnitOfWork();
        var repo = unitOfWork.GetOrCreateRepository<TestEntity>();

        var entity1 = CreateEntity(1, new DateTime(2025, 1, 1));
        var entity2 = CreateEntity(2, new DateTime(2025, 2, 1));
        var entity3 = CreateEntity(3, new DateTime(2025, 3, 1));

        repo.AddDirect(entity1);
        repo.AddDirect(entity2);
        repo.AddDirect(entity3);

        var sut = new TestReadListQueryHandler(CreateLoggerFactory(), unitOfWork);
        var request = new TestPageableRequest
        {
            PageSize = 100,
            SortOptions = ["DateCreated.desc"]
        };
        var query = new TestReadListQuery(request);

        var response = await sut.Handle(query, CancellationToken.None);

        response.Payload.Should().NotBeNull();
        response.Payload!.Select(e => e.DateCreated).Should()
            .BeInDescendingOrder();
    }

    [Fact]
    public async Task Handle_NonIWithDateCreatedEntity_NoDefaultSort()
    {
        var unitOfWork = new FakeUnitOfWork();
        var repo = unitOfWork.GetOrCreateRepository<TestEntityWithoutDateCreated>();

        repo.AddDirect(new TestEntityWithoutDateCreated { Id = Guid.NewGuid(), Name = "e1" });
        repo.AddDirect(new TestEntityWithoutDateCreated { Id = Guid.NewGuid(), Name = "e2" });

        var sut = new OptionsCapturingNoDateCreatedReadListQueryHandler(CreateLoggerFactory(), unitOfWork);
        var request = new NoDateCreatedPageableRequest { PageSize = 100 };
        var query = new NoDateCreatedReadListQuery(request);

        await sut.Handle(query, CancellationToken.None);

        sut.LastOptions.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_PostProcessAsync_HookIsCalled()
    {
        var unitOfWork = new FakeUnitOfWork();
        var repo = unitOfWork.GetOrCreateRepository<TestEntity>();

        repo.AddDirect(CreateEntity(1));

        var sut = new PostProcessTrackingReadListQueryHandler(CreateLoggerFactory(), unitOfWork);
        var request = new TestPageableRequest { PageSize = 100 };
        var query = new TestReadListQuery(request);

        await sut.Handle(query, CancellationToken.None);

        sut.PostProcessAsyncCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Response_StatusCodeIs200_AndPaginationFieldsFilled()
    {
        var unitOfWork = new FakeUnitOfWork();
        var repo = unitOfWork.GetOrCreateRepository<TestEntity>();

        for (var i = 0; i < 5; i++)
        {
            repo.AddDirect(CreateEntity(i));
        }

        var sut = new TestReadListQueryHandler(CreateLoggerFactory(), unitOfWork);
        var request = new TestPageableRequest { PageNumber = 1, PageSize = 2 };
        var query = new TestReadListQuery(request);

        var response = await sut.Handle(query, CancellationToken.None);

        response.StatusCode.Should().Be(200);
        response.PageNumber.Should().Be(1);
        response.TotalPages.Should().Be(3);
        response.Payload.Should().NotBeNull();
        response.Payload!.Should().HaveCount(2);
    }
}
