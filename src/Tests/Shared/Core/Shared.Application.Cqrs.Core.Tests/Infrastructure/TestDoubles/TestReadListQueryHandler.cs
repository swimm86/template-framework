using Microsoft.Extensions.Logging;
using Shared.Application.Cqrs.Core.Abstractions.Queries.Handlers;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Testing.Entities;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public class TestReadListQueryHandler(
    ILoggerFactory loggerFactory,
    IUnitOfWork unitOfWork)
    : ReadListQueryHandler<
        TestReadListQuery,
        TestPageableRequest,
        TestListFilter,
        TestPageableResponse,
        TestEntity,
        TestEntity>(loggerFactory, unitOfWork);
