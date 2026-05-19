using Microsoft.Extensions.Logging;
using Shared.Domain.Core.Dal;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Testing.Entities;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public class OptionsCapturingReadListQueryHandler : TestReadListQueryHandler
{
    public QueryOptions<TestEntity>? LastOptions { get; private set; }

    public OptionsCapturingReadListQueryHandler(ILoggerFactory loggerFactory, IUnitOfWork unitOfWork)
        : base(loggerFactory, unitOfWork)
    {
    }

    protected override async Task<ICollection<TestEntity>> GetPayloadAsync(
        IRepository<TestEntity> repository,
        QueryOptions<TestEntity> options,
        int? skip,
        int? take)
    {
        LastOptions = options;
        return await base.GetPayloadAsync(repository, options, skip, take);
    }
}
