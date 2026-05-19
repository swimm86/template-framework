using Microsoft.Extensions.Logging;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Testing.Entities;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public class PostProcessTrackingReadListQueryHandler(
    ILoggerFactory loggerFactory,
    IUnitOfWork unitOfWork)
    : TestReadListQueryHandler(loggerFactory, unitOfWork)
{
    public int PostProcessAsyncCallCount { get; private set; }
    public ICollection<TestEntity>? ReceivedCollection { get; private set; }

    protected override Task PostProcessAsync(ICollection<TestEntity> dtoCollection, TestReadListQuery query)
    {
        PostProcessAsyncCallCount++;
        ReceivedCollection = dtoCollection;
        return Task.CompletedTask;
    }
}
