// ----------------------------------------------------------------------------------------------
// <copyright file="EfRepository.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Shared.Common.Extensions;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Dal.Specification.Interfaces;
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
    IQueryEvaluator evaluator)
    : IRepository<TEntity>
    where TEntity : class, IEntity
{
    /// <summary>
    /// Сет данных по сущности.
    /// </summary>
    protected DbSet<TEntity> DbSet => dbContext.Set<TEntity>();

    /// <inheritdoc/>
    public Task<TEntity?> GetAsync(
        object? id,
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default)
    {
        options = GetFirstOrDefaultOptions(id, options);
        return evaluator
            .Build(DbSet, options)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<TOut?> GetAsync<TOut>(
        object? id,
        QueryOptions<TEntity>? options = null,
        Expression<Func<TEntity, TOut>>? selector = default,
        CancellationToken cancellationToken = default)
    {
        options = GetFirstOrDefaultOptions(id, options);
        return GetQuery(options, selector).FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<List<TEntity>> GetRangeAsync(
        QueryOptions<TEntity>? options = null,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default) =>
        evaluator
            .Build(DbSet, options)
            .GetRange(skip, take)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public Task<List<TOut>> GetRangeAsync<TOut>(
        QueryOptions<TEntity>? options = null,
        int? skip = null,
        int? take = null,
        Expression<Func<TEntity, TOut>>? selector = default,
        CancellationToken cancellationToken = default) =>
        GetQuery(options, selector)
            .GetRange(skip, take)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public Task<TEntity?> FirstOrDefaultAsync(
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default)
    {
        return evaluator
            .Build(DbSet, options)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<TOut?> FirstOrDefaultAsync<TOut>(
        QueryOptions<TEntity>? options = null,
        Expression<Func<TEntity, TOut>>? selector = default,
        CancellationToken cancellationToken = default)
    {
        return evaluator
            .BuildWithTransform<TEntity, TOut>(DbSet, options)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<TEntity?> SingleOrDefaultAsync(
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default)
    {
        return evaluator
            .Build(DbSet, options)
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<TOut?> SingleOrDefaultAsync<TOut>(
        QueryOptions<TEntity>? options = null,
        Expression<Func<TEntity, TOut>>? selector = default,
        CancellationToken cancellationToken = default)
    {
        return GetQuery(options, selector)
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<TEntity?> LastOrDefaultAsync(
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default)
    {
        return evaluator
            .Build(DbSet, options)
            .LastOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<TOut?> LastOrDefaultAsync<TOut>(
        QueryOptions<TEntity>? options = null,
        Expression<Func<TEntity, TOut>>? selector = default,
        CancellationToken cancellationToken = default)
    {
        return GetQuery(options, selector)
            .LastOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<int> CountAsync(
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default)
    {
        return evaluator.Build(DbSet, options).CountAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> AnyAsync(
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default)
    {
        return evaluator.Build(DbSet, options).AnyAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<decimal> SumAsync(
        Expression<Func<TEntity, decimal>> selector,
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default)
    {
        return evaluator.Build(DbSet, options).SumAsync(selector, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<TEntity> AddAsync(
        TEntity entity,
        Guid? userId,
        string? userName,
        CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);

        if (entity is IWithCreated entityWithCreated)
        {
            entityWithCreated.OnCreate(userId, userName);
        }

        return entity;
    }

    /// <inheritdoc/>
    public async Task AddRangeAsync(
        IEnumerable<TEntity> entities,
        Guid? userId,
        string? userName,
        CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities)
        {
            await AddAsync(entity, userId, userName, cancellationToken);
        }
    }

    /// <inheritdoc/>
    public Task UpdateRangeAsync(
        Expression<Func<TEntity, bool>>? condition = default,
        params (LambdaExpression propertyExpression, LambdaExpression valueExpression)[] updateData)
    {
        var options = new QueryOptions<TEntity>();
        if (condition != default)
        {
            options.AddFilter(condition);
        }

        return UpdateRangeAsync(options, updateData);
    }

    /// <inheritdoc/>
    public Task UpdateRangeAsync(
        QueryOptions<TEntity> options,
        params (LambdaExpression propertyExpression, LambdaExpression valueExpression)[] updateData)
    {
        var query = evaluator.Build(DbSet, options);
        var parameter = Expression.Parameter(typeof(SetPropertyCalls<TEntity>), "x");
        Expression setPropertyCalls = parameter;

        foreach (var (propertyExpr, valueExpr) in updateData)
        {
            var propertyType = propertyExpr.ReturnType;
            var setPropertyMethod = typeof(SetPropertyCalls<TEntity>).GetMethods()
                .FirstOrDefault(m => m is { Name: nameof(SetPropertyCalls<TEntity>.SetProperty), IsGenericMethod: true })
                ?.MakeGenericMethod(propertyType)!;

            setPropertyCalls = Expression.Call(
                setPropertyCalls,
                setPropertyMethod,
                propertyExpr,
                valueExpr);
        }

        var updateExpression = Expression.Lambda<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>>(setPropertyCalls, parameter);
        return query.ExecuteUpdateAsync(updateExpression);
    }

    /// <inheritdoc/>
    public Task UpdateRangeAsync(
        ISpecification<TEntity> specification,
        params (LambdaExpression propertyExpression, LambdaExpression valueExpression)[] updateData)
    {
        return UpdateRangeAsync(specification.BuildOptions(), updateData);
    }

    /// <inheritdoc/>
    public Task RemoveAsync(
        TEntity entity,
        Guid? userId,
        bool hard = false,
        CancellationToken cancellationToken = default)
    {
        if (!hard && entity is IWithDeleted deletable)
        {
            deletable.SetIsDeleted();
            deletable.OnDelete(userId);
        }
        else
        {
            DbSet.Remove(entity);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task RemoveAsync(
        TEntity entity,
        bool hard = false,
        CancellationToken cancellationToken = default)
    {
        return RemoveAsync(entity, null, hard, cancellationToken);
    }

    /// <inheritdoc/>
    public Task RemoveRangeAsync(
        IEnumerable<TEntity> entities,
        bool hard = false,
        CancellationToken cancellationToken = default)
    {
        return entities.ForeachAsync(
            entity => RemoveAsync(entity, null, hard, cancellationToken),
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task RemovePermanentRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        return entities.ForeachAsync(
            entity => RemoveAsync(entity, null, true, cancellationToken),
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RemoveRangeAsync(
        QueryOptions<TEntity> options,
        bool hard = false,
        CancellationToken cancellationToken = default)
    {
        var entities = await GetRangeAsync(options, cancellationToken: cancellationToken);
        await RemoveRangeAsync(entities, hard, cancellationToken);
    }

    /// <inheritdoc/>
    public Task RemoveRangeAsync(
        Expression<Func<TEntity, bool>> conditions,
        bool hard = false,
        CancellationToken cancellationToken = default)
    {
        var options = new QueryOptions<TEntity>(true);
        options.AddFilter(conditions);
        return RemoveRangeAsync(options, hard, cancellationToken);
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
        bool useTransaction = false,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = useTransaction
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        var result = await process();
        if (transaction == null)
        {
            return result;
        }

        try
        {
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <inheritdoc/>
    public IQueryable<TEntity> Set(QueryOptions<TEntity>? options = null) =>
        evaluator.Build(DbSet, options);

    /// <inheritdoc/>
    public void SaveChanges() =>
        dbContext.SaveChanges();

    /// <inheritdoc/>
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<TOut> GetQuery<TOut>(
        QueryOptions<TEntity>? options,
        Expression<Func<TEntity, TOut>>? selector) =>
        selector is null
            ? evaluator.BuildWithTransform<TEntity, TOut>(DbSet, options)
            : evaluator.Build(DbSet, options).Select(selector);

    private QueryOptions<TEntity> GetFirstOrDefaultOptions(
        object? id,
        QueryOptions<TEntity>? options)
    {
        options ??= new QueryOptions<TEntity>();
        if (id is not null)
        {
            options.AddFilter(x => id.Equals(x.Id));
        }

        return options;
    }
}
