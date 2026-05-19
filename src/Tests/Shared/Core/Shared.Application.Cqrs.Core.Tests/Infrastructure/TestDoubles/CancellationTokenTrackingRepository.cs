using Shared.Domain.Core.Dal;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.Specification.Interfaces;
using Shared.Domain.Core.Interfaces;
using Shared.Testing.Doubles.Repository;
using System.Linq.Expressions;
using Shared.Domain.Core.Dal.Repository.Models;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public sealed class CancellationTokenTrackingRepository<TEntity> : FakeRepository<TEntity>
    where TEntity : class, IEntity
{
    public CancellationToken LastCancellationToken { get; private set; }
    public int GetAsyncWithCancellationTokenCallCount { get; private set; }

    public new Task<TEntity?> GetAsync(object? id, QueryOptions<TEntity>? options = null, CancellationToken cancellationToken = default)
    {
        LastCancellationToken = cancellationToken;
        GetAsyncWithCancellationTokenCallCount++;
        return base.GetAsync(id, options, cancellationToken);
    }
}
