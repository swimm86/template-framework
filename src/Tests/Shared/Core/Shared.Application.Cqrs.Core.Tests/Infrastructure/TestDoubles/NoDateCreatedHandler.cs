using Microsoft.Extensions.Logging;
using Shared.Application.Cqrs.Core.Abstractions.Queries.Handlers;
using Shared.Application.Cqrs.Core.Abstractions.Queries.Requests;
using Shared.Application.Core.Dto.Requests;
using Shared.Application.Core.Dto.Responses;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public class NoDateCreatedReadListQuery(NoDateCreatedPageableRequest request)
    : ReadListQuery<NoDateCreatedPageableRequest, TestListFilter, NoDateCreatedPageableResponse>(request);

public record NoDateCreatedPageableRequest : PageableRequest<TestListFilter>;

public record NoDateCreatedPageableResponse : PageableResponse<ICollection<TestEntityWithoutDateCreated>>;

public class NoDateCreatedReadListQueryHandler(
    ILoggerFactory loggerFactory,
    IUnitOfWork unitOfWork)
    : ReadListQueryHandler<
        NoDateCreatedReadListQuery,
        NoDateCreatedPageableRequest,
        TestListFilter,
        NoDateCreatedPageableResponse,
        TestEntityWithoutDateCreated,
        TestEntityWithoutDateCreated>(loggerFactory, unitOfWork);
