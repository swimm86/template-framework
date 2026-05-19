using Microsoft.Extensions.Logging;
using Shared.Application.Cqrs.Core.Abstractions;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Testing.Entities;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public class TestEntityRequestHandler(
    IUnitOfWork unitOfWork,
    ILoggerFactory loggerFactory)
    : EntityRequestHandler<GetTestEntityRequest, TestEntity, TestEntity>(
        unitOfWork,
        loggerFactory)
{
    public new IRepository<TestEntity> Repository => base.Repository;

    public new QueryOptions<TestEntity> ConstructOptions(GetTestEntityRequest request)
        => base.ConstructOptions(request);

    public new QueryOptions<TestEntity> ConstructOptions(
        GetTestEntityRequest request,
        bool withDeletable)
        => base.ConstructOptions(request, withDeletable);

    public Task CallProcessEntityAsync(
        TestEntity entity,
        GetTestEntityRequest request,
        CancellationToken cancellationToken)
        => base.ProcessEntityAsync(entity, request, cancellationToken);

    public Task CallProcessResponseAsync(
        TestEntity response,
        GetTestEntityRequest request,
        CancellationToken cancellationToken)
        => base.ProcessResponseAsync(response, request, cancellationToken);

    public Task CallProcessEntitiesAsync(
        ICollection<TestEntity> entities,
        GetTestEntityRequest request,
        CancellationToken cancellationToken)
        => base.ProcessEntitiesAsync(entities, request, cancellationToken);

    public override Task<TestEntity> Handle(
        GetTestEntityRequest query,
        CancellationToken cancellationToken)
        => Task.FromResult(new TestEntity());
}

public sealed class TestEntityRequestHandlerWithTracking(
    IUnitOfWork unitOfWork,
    ILoggerFactory loggerFactory)
    : TestEntityRequestHandler(unitOfWork, loggerFactory)
{
    protected override bool WithTracking => true;
}

public sealed class TestEntityRequestHandlerWithSplitQuery(
    IUnitOfWork unitOfWork,
    ILoggerFactory loggerFactory)
    : TestEntityRequestHandler(
        unitOfWork,
        loggerFactory)
{
    protected override bool AsSplitQuery => true;
}

public sealed class TestEntityRequestHandlerWithProcessEntity(
    IUnitOfWork unitOfWork,
    ILoggerFactory loggerFactory)
    : EntityRequestHandler<GetTestEntityRequest, TestEntity, TestEntity>(
        unitOfWork,
        loggerFactory)
{
    public bool ProcessEntityCalled { get; private set; }

    protected override Task ProcessEntityAsync(
        TestEntity entity,
        GetTestEntityRequest request,
        CancellationToken cancellationToken)
    {
        ProcessEntityCalled = true;
        return base.ProcessEntityAsync(entity, request, cancellationToken);
    }

    public Task CallProcessEntityAsync(
        TestEntity entity,
        GetTestEntityRequest request,
        CancellationToken cancellationToken)
        => ProcessEntityAsync(entity, request, cancellationToken);

    public override Task<TestEntity> Handle(
        GetTestEntityRequest query,
        CancellationToken cancellationToken)
        => Task.FromResult(new TestEntity());
}

public sealed class TestEntityRequestHandlerWithProcessResponse(
    IUnitOfWork unitOfWork,
    ILoggerFactory loggerFactory)
    : EntityRequestHandler<GetTestEntityRequest, TestEntity, TestEntity>(
        unitOfWork,
        loggerFactory)
{
    public bool ProcessResponseCalled { get; private set; }

    protected override Task ProcessResponseAsync(
        TestEntity response,
        GetTestEntityRequest request,
        CancellationToken cancellationToken)
    {
        ProcessResponseCalled = true;
        return base.ProcessResponseAsync(response, request, cancellationToken);
    }

    public Task CallProcessResponseAsync(
        TestEntity response,
        GetTestEntityRequest request,
        CancellationToken cancellationToken)
        => ProcessResponseAsync(response, request, cancellationToken);

    public override Task<TestEntity> Handle(
        GetTestEntityRequest query,
        CancellationToken cancellationToken)
        => Task.FromResult(new TestEntity());
}

public sealed class TestEntityRequestHandlerWithProcessEntities(
    IUnitOfWork unitOfWork,
    ILoggerFactory loggerFactory)
    : EntityRequestHandler<GetTestEntityRequest, TestEntity, TestEntity>(
        unitOfWork,
        loggerFactory)
{
    public bool ProcessEntitiesCalled { get; private set; }

    protected override Task ProcessEntitiesAsync(
        ICollection<TestEntity> entities,
        GetTestEntityRequest request,
        CancellationToken cancellationToken)
    {
        ProcessEntitiesCalled = true;
        return base.ProcessEntitiesAsync(entities, request, cancellationToken);
    }

    public Task CallProcessEntitiesAsync(
        ICollection<TestEntity> entities,
        GetTestEntityRequest request,
        CancellationToken cancellationToken)
        => ProcessEntitiesAsync(entities, request, cancellationToken);

    public override Task<TestEntity> Handle(
        GetTestEntityRequest query,
        CancellationToken cancellationToken)
        => Task.FromResult(new TestEntity());
}

public sealed class TestEntityWithoutDeletedHandler(
    IUnitOfWork unitOfWork,
    ILoggerFactory loggerFactory)
    : EntityRequestHandler<GetTestEntityWithoutDeletedRequest, TestEntityWithoutDeleted, TestEntityWithoutDeleted>(
        unitOfWork,
        loggerFactory)
{
    public new QueryOptions<TestEntityWithoutDeleted> ConstructOptions(GetTestEntityWithoutDeletedRequest request)
        => base.ConstructOptions(request);

    public override Task<TestEntityWithoutDeleted> Handle(
        GetTestEntityWithoutDeletedRequest query,
        CancellationToken cancellationToken)
        => Task.FromResult(new TestEntityWithoutDeleted());
}
