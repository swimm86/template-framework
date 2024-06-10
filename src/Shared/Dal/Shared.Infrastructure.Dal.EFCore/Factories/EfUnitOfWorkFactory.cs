using Shared.Application.Core.Dal.Repository.Interfaces;
using Shared.Application.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Interfaces;

namespace Shared.Infrastructure.Dal.EFCore.Factories;

/// <summary>
/// Реализация интерфейса <see cref="IUnitOfWorkFactory"/> на основе ORM "Entity Framework Core".
/// </summary>
/// <param name="factory"></param>
public class EfUnitOfWorkFactory(
    IDbContextFactory<DbContextBase> factory
) : IUnitOfWorkFactory
{
    /// <inheritdoc />
    public TResult Execute<TEntity, TResult>(Func<IRepository<TEntity>, TResult> process, bool useTransaction = false)
        where TEntity : class, IEntity
    {
        using var ctxt = factory.CreateDbContext();
        return ctxt.Execute(process, useTransaction);
    }

    /// <inheritdoc />
    public async Task<TResult> ExecuteAsync<TEntity, TResult>(
        Func<IRepository<TEntity>, Task<TResult>> process,
        CancellationToken cancellationToken,
        bool useTransaction = false
    ) where TEntity : class, IEntity
    {
        await using var ctxt = await factory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        return await ctxt
            .ExecuteAsync(process, cancellationToken, useTransaction)
            .ConfigureAwait(false);
    }
}
