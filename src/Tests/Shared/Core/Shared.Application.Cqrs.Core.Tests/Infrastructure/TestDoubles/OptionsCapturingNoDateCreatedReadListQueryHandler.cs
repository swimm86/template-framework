using Microsoft.Extensions.Logging;
using Shared.Domain.Core.Dal;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public class OptionsCapturingNoDateCreatedReadListQueryHandler : NoDateCreatedReadListQueryHandler
{
    public QueryOptions<TestEntityWithoutDateCreated>? LastOptions { get; private set; }

    public OptionsCapturingNoDateCreatedReadListQueryHandler(ILoggerFactory loggerFactory, IUnitOfWork unitOfWork)
        : base(loggerFactory, unitOfWork)
    {
    }

    protected override async Task<ICollection<TestEntityWithoutDateCreated>> GetPayloadAsync(
        IRepository<TestEntityWithoutDateCreated> repository,
        QueryOptions<TestEntityWithoutDateCreated> options,
        int? skip,
        int? take)
    {
        LastOptions = options;
        return await base.GetPayloadAsync(repository, options, skip, take);
    }
}
