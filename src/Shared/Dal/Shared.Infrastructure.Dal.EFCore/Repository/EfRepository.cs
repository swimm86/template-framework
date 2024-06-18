// ----------------------------------------------------------------------------------------------
// <copyright file="EfRepository.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dal.Repository.Interfaces;
using Shared.Application.Core.Dal.Repository.Models;
using Shared.Common.Extensions;
using Shared.Domain.Core.Interfaces;

namespace Shared.Infrastructure.Dal.EFCore.Repository;

/// <summary>
/// Реализация интерфейса <see cref="IRepository{TEntity}"/> на основе ORM "Entity Framework Core"
/// </summary>
/// <param name="dbContext"><see cref="DbContext"/>.</param>
/// <param name="evaluator"><see cref="IQueryEvaluator"/>.</param>
/// <typeparam name="TEntity">Тип сущности.</typeparam>
public class EfRepository<TEntity>(
    DbContext dbContext,
    IQueryEvaluator evaluator
) : IRepository<TEntity>
    where TEntity : class, IEntity
{
    /// <summary>
    /// Сет данных по сущности.
    /// </summary>
    protected DbSet<TEntity> DbSet => dbContext.Set<TEntity>();

    /// <inheritdoc/>
    public Task<TEntity?> GetAsync(object id, QueryOptions<TEntity>? options = null)
    {
        return options == default
            ? DbSet.FirstOrDefaultAsync(x => id.Equals(x.Id))
            : evaluator.Build(DbSet, options).FirstOrDefaultAsync(x => id.Equals(x.Id));
    }

    /// <inheritdoc/>
    public Task<List<TEntity>> GetRangeAsync(QueryOptions<TEntity>? options = null, int? skip = null, int? take = null) =>
        evaluator.Build(DbSet, options).GetRange(skip, take).ToListAsync();

    /// <inheritdoc/>
    public Task<List<TOut>> GetRangeAsync<TOut>(
        QueryOptions<TEntity>? options = null,
        int? skip = null,
        int? take = null) =>
        evaluator.BuildWithTransform<TEntity, TOut>(DbSet, options).GetRange(skip, take).ToListAsync();

    /// <inheritdoc/>
    public Task<TEntity?> FirstOrDefaultAsync(QueryOptions<TEntity>? options = null)
    {
        return evaluator.Build(DbSet, options).FirstOrDefaultAsync();
    }

    /// <inheritdoc/>
    public Task<TEntity?> SingleOrDefaultAsync(QueryOptions<TEntity>? options = null)
    {
        return evaluator.Build(DbSet, options).SingleOrDefaultAsync();
    }

    /// <inheritdoc/>
    public Task<TEntity?> LastOrDefaultAsync(QueryOptions<TEntity>? options = null)
    {
        return evaluator.Build(DbSet, options).LastOrDefaultAsync();
    }

    /// <inheritdoc/>
    public Task<int> CountAsync(QueryOptions<TEntity>? options = null)
    {
        return evaluator.Build(DbSet, options).CountAsync();
    }

    /// <inheritdoc/>
    public async Task<TEntity> AddAsync(TEntity entity)
    {
        await DbSet.AddAsync(entity).ConfigureAwait(false);
        return entity;
    }

    /// <inheritdoc/>
    public Task AddRangeAsync(IEnumerable<TEntity> entities)
    {
        return DbSet.AddRangeAsync(entities);
    }

    /// <inheritdoc/>
    public Task RemoveAsync(TEntity entity)
    {
        DbSet.Remove(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task RemoveRangeAsync(IEnumerable<TEntity> entities)
    {
        DbSet.RemoveRange(entities);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task RemoveRangeAsync(QueryOptions<TEntity> spec)
    {
        var entities = await GetRangeAsync(spec).ConfigureAwait(false);
        await RemoveRangeAsync(entities).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public TResult Execute<TResult>(Func<TResult> process, bool useTransaction = false)
    {
        using var transaction = useTransaction ? dbContext.Database.BeginTransaction() : null;
        var result = process();
        if (transaction == null)
        {
            return result;
        }

        try
        {
            transaction.Commit();
            return result;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> process,
        CancellationToken token,
        bool useTransaction = false)
    {
        await using var transaction =
            useTransaction ? await dbContext.Database.BeginTransactionAsync(token).ConfigureAwait(false) : null;
        var result = await process().ConfigureAwait(false);
        if (transaction == null)
        {
            return result;
        }

        try
        {
            await transaction.CommitAsync(token).ConfigureAwait(false);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(token).ConfigureAwait(false);
            throw;
        }
    }

    /// <inheritdoc/>
    public IQueryable<TEntity> Set() =>
        dbContext.Set<TEntity>();

    /// <inheritdoc/>
    public void SaveChanges() =>
        dbContext.SaveChanges();

    /// <inheritdoc/>
    public Task SaveChangesAsync() =>
        dbContext.SaveChangesAsync();
}
