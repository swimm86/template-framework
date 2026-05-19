using Microsoft.Extensions.Logging;
using Shared.Application.Cqrs.Core.Abstractions;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Testing.Entities;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public class TestEntityRequestHandler(IUnitOfWork unitOfWork, ILoggerFactory loggerFactory)
    : EntityRequestHandler<GetTestEntityRequest, TestEntity, TestEntity>(unitOfWork, loggerFactory)
{
    public new IRepository<TestEntity> Repository => base.Repository;

    public new QueryOptions<TestEntity> ConstructOptions(GetTestEntityRequest request)
        => base.ConstructOptions(request);

    public new QueryOptions<TestEntity> ConstructOptions(GetTestEntityRequest request, bool withDeletable)
        => base.ConstructOptions(request, withDeletable);

    public Task CallProcessEntityAsync(TestEntity entity, GetTestEntityRequest request)
        => base.ProcessEntityAsync(entity, request);

    public Task CallProcessResponseAsync(TestEntity response, GetTestEntityRequest request)
        => base.ProcessResponseAsync(response, request);

    public Task CallProcessEntitiesAsync(ICollection<TestEntity> entities, GetTestEntityRequest request)
        => base.ProcessEntitiesAsync(entities, request);

    public override Task<TestEntity> Handle(GetTestEntityRequest query, CancellationToken cancellationToken)
        => Task.FromResult(new TestEntity());
}

public sealed class TestEntityRequestHandlerWithTracking(IUnitOfWork unitOfWork, ILoggerFactory loggerFactory)
    : TestEntityRequestHandler(unitOfWork, loggerFactory)
{
    protected override bool WithTracking => true;
}

public sealed class TestEntityRequestHandlerWithSplitQuery(IUnitOfWork unitOfWork, ILoggerFactory loggerFactory)
    : TestEntityRequestHandler(unitOfWork, loggerFactory)
{
    protected override bool AsSplitQuery => true;
}

public sealed class TestEntityRequestHandlerWithProcessEntity(IUnitOfWork unitOfWork, ILoggerFactory loggerFactory)
    : EntityRequestHandler<GetTestEntityRequest, TestEntity, TestEntity>(unitOfWork, loggerFactory)
{
    public bool ProcessEntityCalled { get; private set; }

    protected override async Task ProcessEntityAsync(TestEntity entity, GetTestEntityRequest request)
    {
        ProcessEntityCalled = true;
        await base.ProcessEntityAsync(entity, request);
    }

    public Task CallProcessEntityAsync(TestEntity entity, GetTestEntityRequest request)
        => ProcessEntityAsync(entity, request);

    public override Task<TestEntity> Handle(GetTestEntityRequest query, CancellationToken cancellationToken)
        => Task.FromResult(new TestEntity());
}

public sealed class TestEntityRequestHandlerWithProcessResponse(IUnitOfWork unitOfWork, ILoggerFactory loggerFactory)
    : EntityRequestHandler<GetTestEntityRequest, TestEntity, TestEntity>(unitOfWork, loggerFactory)
{
    public bool ProcessResponseCalled { get; private set; }

    protected override async Task ProcessResponseAsync(TestEntity response, GetTestEntityRequest request)
    {
        ProcessResponseCalled = true;
        await base.ProcessResponseAsync(response, request);
    }

    public Task CallProcessResponseAsync(TestEntity response, GetTestEntityRequest request)
        => ProcessResponseAsync(response, request);

    public override Task<TestEntity> Handle(GetTestEntityRequest query, CancellationToken cancellationToken)
        => Task.FromResult(new TestEntity());
}

public sealed class TestEntityRequestHandlerWithProcessEntities(IUnitOfWork unitOfWork, ILoggerFactory loggerFactory)
    : EntityRequestHandler<GetTestEntityRequest, TestEntity, TestEntity>(unitOfWork, loggerFactory)
{
    public bool ProcessEntitiesCalled { get; private set; }

    protected override async Task ProcessEntitiesAsync(ICollection<TestEntity> entities, GetTestEntityRequest request)
    {
        ProcessEntitiesCalled = true;
        await base.ProcessEntitiesAsync(entities, request);
    }

    public Task CallProcessEntitiesAsync(ICollection<TestEntity> entities, GetTestEntityRequest request)
        => ProcessEntitiesAsync(entities, request);

    public override Task<TestEntity> Handle(GetTestEntityRequest query, CancellationToken cancellationToken)
        => Task.FromResult(new TestEntity());
}

public sealed class TestEntityWithoutDeletedHandler(IUnitOfWork unitOfWork, ILoggerFactory loggerFactory)
    : EntityRequestHandler<GetTestEntityWithoutDeletedRequest, TestEntityWithoutDeleted, TestEntityWithoutDeleted>(
        unitOfWork,
        loggerFactory)
{
    public new QueryOptions<TestEntityWithoutDeleted> ConstructOptions(GetTestEntityWithoutDeletedRequest request)
        => base.ConstructOptions(request);

    public override Task<TestEntityWithoutDeleted> Handle(GetTestEntityWithoutDeletedRequest query, CancellationToken cancellationToken)
        => Task.FromResult(new TestEntityWithoutDeleted());
}
