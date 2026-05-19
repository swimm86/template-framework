using Microsoft.Extensions.Logging;
using Shared.Application.Cqrs.Core.Abstractions.Queries.Handlers;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Mapping.Interfaces;
using Shared.Testing.Entities;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public sealed class GuardThrowingReadQueryHandler(
    ILoggerFactory loggerFactory,
    IMapper mapper,
    IUnitOfWork unitOfWork,
    Exception exceptionToThrow)
    : ReadQueryHandler<TestReadByKeyQuery, TestEntity, TestEntity>(loggerFactory, mapper, unitOfWork)
{
    protected override Task GuardAsync(TestReadByKeyQuery request, CancellationToken cancellationToken)
    {
        throw exceptionToThrow;
    }
}
