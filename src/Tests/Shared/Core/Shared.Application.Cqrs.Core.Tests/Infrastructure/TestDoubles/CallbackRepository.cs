using System.Linq.Expressions;
using Shared.Domain.Core.Dal;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Interfaces;
using Shared.Testing.Doubles.Repository;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public sealed class CallbackRepository<TEntity>(
    FakeRepository<TEntity> inner,
    Action<CancellationToken>? onGetAsyncCalled = null)
    : IRepository<TEntity>
    where TEntity : class, IEntity
{
    public FakeRepository<TEntity> Inner => inner;

    public Task<TEntity?> GetAsync(
        object? id,
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default)
    {
        onGetAsyncCalled?.Invoke(cancellationToken);
        return inner.GetAsync(id, options, cancellationToken);
    }

    public Task<TOut?> GetAsync<TOut>(
        object? id,
        QueryOptions<TEntity>? options = null,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default)
        => inner.GetAsync(id, options, selector, cancellationToken);

    public Task<List<TEntity>> GetRangeAsync(
        QueryOptions<TEntity>? options = null,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default)
        => inner.GetRangeAsync(options, skip, take, cancellationToken);

    public Task<List<TOut>> GetRangeAsync<TOut>(
        QueryOptions<TEntity>? options = null,
        int? skip = null,
        int? take = null,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default)
        => inner.GetRangeAsync(options, skip, take, selector, cancellationToken);

    public Task<TEntity?> FirstOrDefaultAsync(
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default)
        => inner.FirstOrDefaultAsync(options, cancellationToken);

    public Task<TOut?> FirstOrDefaultAsync<TOut>(
        QueryOptions<TEntity>? options = null,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default)
        => inner.FirstOrDefaultAsync(options, selector, cancellationToken);

    public Task<TEntity?> SingleOrDefaultAsync(
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default)
        => inner.SingleOrDefaultAsync(options, cancellationToken);

    public Task<TOut?> SingleOrDefaultAsync<TOut>(
        QueryOptions<TEntity>? options = null,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default)
        => inner.SingleOrDefaultAsync(options, selector, cancellationToken);

    public Task<TEntity?> LastOrDefaultAsync(
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default)
        => inner.LastOrDefaultAsync(options, cancellationToken);

    public Task<TOut?> LastOrDefaultAsync<TOut>(
        QueryOptions<TEntity>? options = null,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default)
        => inner.LastOrDefaultAsync(options, selector, cancellationToken);

    public Task<int> CountAsync(
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default)
        => inner.CountAsync(options, cancellationToken);

    public Task<bool> AnyAsync(
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default)
        => inner.AnyAsync(options, cancellationToken);

    public Task<decimal> SumAsync(
        Expression<Func<TEntity, decimal>> selector,
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default)
        => inner.SumAsync(selector, options, cancellationToken);

    public Task<TEntity> AddAsync(
        TEntity entity,
        Guid? userId = null,
        string? userName = null,
        CancellationToken cancellationToken = default)
        => inner.AddAsync(entity, userId, userName, cancellationToken);

    public Task AddRangeAsync(
        IEnumerable<TEntity> entities,
        Guid? userId = null,
        string? userName = null,
        CancellationToken cancellationToken = default)
        => inner.AddRangeAsync(entities, userId, userName, cancellationToken);

    public Task ExecuteUpdateRangeAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        params (LambdaExpression propertyExpression, LambdaExpression valueExpression)[] updateData)
        => inner.ExecuteUpdateRangeAsync(predicate, updateData);

    public Task ExecuteUpdateRangeAsync(
        QueryOptions<TEntity> options,
        params (LambdaExpression propertyExpression, LambdaExpression valueExpression)[] updateData)
        => inner.ExecuteUpdateRangeAsync(options, updateData);

    public Task<List<IGrouping<TKey, TEntity>>> GetGroupingAsync<TKey>(
        Expression<Func<TEntity, TKey>> keySelector,
        QueryOptions<TEntity>? options = null,
        int? skip = null,
        int? take = null,
        OrderDirectionType? groupKeyOrderDirection = null,
        CancellationToken cancellationToken = default)
        => inner.GetGroupingAsync(keySelector, options, skip, take, groupKeyOrderDirection, cancellationToken);

    public Task<List<TOut>> GetGroupingAsync<TKey, TOut>(
        Expression<Func<TEntity, TKey>> keySelector,
        QueryOptions<TEntity>? options = null,
        int? skip = null,
        int? take = null,
        Expression<Func<IGrouping<TKey, TEntity>, TOut>>? selector = null,
        OrderDirectionType? groupKeyOrderDirection = null,
        CancellationToken cancellationToken = default)
        => inner.GetGroupingAsync(keySelector, options, skip, take, selector, groupKeyOrderDirection, cancellationToken);

    public Task<int> CountGroupsAsync<TKey>(
        Expression<Func<TEntity, TKey>> keySelector,
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default)
        => inner.CountGroupsAsync(keySelector, options, cancellationToken);

    public Task<TOut?> MaxAsync<TOut>(
        Expression<Func<TEntity, TOut>> selector,
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default)
        => inner.MaxAsync(selector, options, cancellationToken);

    public Task<TOut?> MinAsync<TOut>(
        Expression<Func<TEntity, TOut>> selector,
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default)
        => inner.MinAsync(selector, options, cancellationToken);

    public Task RemoveAsync(
        TEntity entity,
        Guid? userId,
        bool hard = false,
        CancellationToken cancellationToken = default)
        => inner.RemoveAsync(entity, userId, hard, cancellationToken);

    public Task RemoveAsync(
        TEntity entity,
        bool hard = false,
        CancellationToken cancellationToken = default)
        => inner.RemoveAsync(entity, hard, cancellationToken);

    public Task RemoveRangeAsync(
        IEnumerable<TEntity> entities,
        bool hard = false,
        CancellationToken cancellationToken = default)
        => inner.RemoveRangeAsync(entities, hard, cancellationToken);

    public Task RemovePermanentRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
        => inner.RemovePermanentRangeAsync(entities, cancellationToken);

    public Task RemoveRangeAsync(
        QueryOptions<TEntity> options,
        bool hard = false,
        CancellationToken cancellationToken = default)
        => inner.RemoveRangeAsync(options, hard, cancellationToken);

    public Task RemoveRangeAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool hard = false,
        CancellationToken cancellationToken = default)
        => inner.RemoveRangeAsync(predicate, hard, cancellationToken);

    public Task ExecuteRemoveRangeAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => inner.ExecuteRemoveRangeAsync(predicate, cancellationToken);

    public Task ExecuteRemoveRangeAsync(
        QueryOptions<TEntity> options,
        CancellationToken cancellationToken = default)
        => inner.ExecuteRemoveRangeAsync(options, cancellationToken);

    public void Execute(
        Action process,
        bool useTransaction = false) =>
        inner.Execute(process, useTransaction);

    public TResult Execute<TResult>(
        Func<TResult> process,
        bool useTransaction = false)
        => inner.Execute(process, useTransaction);

    public Task ExecuteAsync(
        Func<Task> process,
        bool useTransaction = false,
        CancellationToken cancellationToken = default) =>
        inner.ExecuteAsync(process, useTransaction, cancellationToken);

    public Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> process,
        bool useTransaction = false,
        CancellationToken cancellationToken = default) =>
        inner.ExecuteAsync(process, useTransaction, cancellationToken);

    public IQueryable<TEntity> Set(
        QueryOptions<TEntity>? options = null) =>
        inner.Set(options);

    public void SaveChanges() =>
        inner.SaveChanges();

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default) =>
        inner.SaveChangesAsync(cancellationToken);
}
