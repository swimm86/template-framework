using System.Linq.Expressions;
using Shared.Domain.Core.Dal;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Dal.Specification.Interfaces;
using Shared.Domain.Core.Interfaces;
using Shared.Testing.Doubles.Repository;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public sealed class CallbackRepository<TEntity> : IRepository<TEntity>
    where TEntity : class, IEntity
{
    private readonly FakeRepository<TEntity> _inner;
    private readonly Action<CancellationToken>? _onGetAsyncCalled;

    public CallbackRepository(FakeRepository<TEntity> inner, Action<CancellationToken>? onGetAsyncCalled = null)
    {
        _inner = inner;
        _onGetAsyncCalled = onGetAsyncCalled;
    }

    public FakeRepository<TEntity> Inner => _inner;

    public Task<TEntity?> GetAsync(object? id, QueryOptions<TEntity>? options = null, CancellationToken cancellationToken = default)
    {
        _onGetAsyncCalled?.Invoke(cancellationToken);
        return _inner.GetAsync(id, options, cancellationToken);
    }

    public Task<TEntity?> GetAsync(object? id, ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
        => _inner.GetAsync(id, specification, cancellationToken);

    public Task<TOut?> GetAsync<TOut>(object? id, QueryOptions<TEntity>? options = null, Expression<Func<TEntity, TOut>>? selector = default, CancellationToken cancellationToken = default)
        => _inner.GetAsync(id, options, selector, cancellationToken);

    public Task<TOut?> GetAsync<TOut>(object? id, ISpecification<TEntity> specification, Expression<Func<TEntity, TOut>>? selector = default, CancellationToken cancellationToken = default)
        => _inner.GetAsync(id, specification, selector, cancellationToken);

    public Task<List<TEntity>> GetRangeAsync(QueryOptions<TEntity>? options = null, int? skip = null, int? take = null, CancellationToken cancellationToken = default)
        => _inner.GetRangeAsync(options, skip, take, cancellationToken);

    public Task<List<TEntity>> GetRangeAsync(ISpecification<TEntity> specification, int? skip = null, int? take = null, CancellationToken cancellationToken = default)
        => _inner.GetRangeAsync(specification, skip, take, cancellationToken);

    public Task<List<TOut>> GetRangeAsync<TOut>(QueryOptions<TEntity>? options = null, int? skip = null, int? take = null, Expression<Func<TEntity, TOut>>? selector = default, CancellationToken cancellationToken = default)
        => _inner.GetRangeAsync(options, skip, take, selector, cancellationToken);

    public Task<List<TOut>> GetRangeAsync<TOut>(ISpecification<TEntity> specification, int? skip = null, int? take = null, Expression<Func<TEntity, TOut>>? selector = default, CancellationToken cancellationToken = default)
        => _inner.GetRangeAsync(specification, skip, take, selector, cancellationToken);

    public Task<TEntity?> FirstOrDefaultAsync(QueryOptions<TEntity>? options = null, CancellationToken cancellationToken = default)
        => _inner.FirstOrDefaultAsync(options, cancellationToken);

    public Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
        => _inner.FirstOrDefaultAsync(specification, cancellationToken);

    public Task<TOut?> FirstOrDefaultAsync<TOut>(QueryOptions<TEntity>? options = null, Expression<Func<TEntity, TOut>>? selector = default, CancellationToken cancellationToken = default)
        => _inner.FirstOrDefaultAsync(options, selector, cancellationToken);

    public Task<TOut?> FirstOrDefaultAsync<TOut>(ISpecification<TEntity> specification, Expression<Func<TEntity, TOut>>? selector = default, CancellationToken cancellationToken = default)
        => _inner.FirstOrDefaultAsync(specification, selector, cancellationToken);

    public Task<TEntity?> SingleOrDefaultAsync(QueryOptions<TEntity>? options = null, CancellationToken cancellationToken = default)
        => _inner.SingleOrDefaultAsync(options, cancellationToken);

    public Task<TEntity?> SingleOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
        => _inner.SingleOrDefaultAsync(specification, cancellationToken);

    public Task<TOut?> SingleOrDefaultAsync<TOut>(QueryOptions<TEntity>? options = null, Expression<Func<TEntity, TOut>>? selector = default, CancellationToken cancellationToken = default)
        => _inner.SingleOrDefaultAsync(options, selector, cancellationToken);

    public Task<TOut?> SingleOrDefaultAsync<TOut>(ISpecification<TEntity> specification, Expression<Func<TEntity, TOut>>? selector = default, CancellationToken cancellationToken = default)
        => _inner.SingleOrDefaultAsync(specification, selector, cancellationToken);

    public Task<TEntity?> LastOrDefaultAsync(QueryOptions<TEntity>? options = null, CancellationToken cancellationToken = default)
        => _inner.LastOrDefaultAsync(options, cancellationToken);

    public Task<TEntity?> LastOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
        => _inner.LastOrDefaultAsync(specification, cancellationToken);

    public Task<TOut?> LastOrDefaultAsync<TOut>(QueryOptions<TEntity>? options = null, Expression<Func<TEntity, TOut>>? selector = default, CancellationToken cancellationToken = default)
        => _inner.LastOrDefaultAsync(options, selector, cancellationToken);

    public Task<TOut?> LastOrDefaultAsync<TOut>(ISpecification<TEntity> specification, Expression<Func<TEntity, TOut>>? selector = default, CancellationToken cancellationToken = default)
        => _inner.LastOrDefaultAsync(specification, selector, cancellationToken);

    public Task<int> CountAsync(QueryOptions<TEntity>? options = null, CancellationToken cancellationToken = default)
        => _inner.CountAsync(options, cancellationToken);

    public Task<int> CountAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
        => _inner.CountAsync(specification, cancellationToken);

    public Task<bool> AnyAsync(QueryOptions<TEntity>? options = null, CancellationToken cancellationToken = default)
        => _inner.AnyAsync(options, cancellationToken);

    public Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
        => _inner.AnyAsync(specification, cancellationToken);

    public Task<decimal> SumAsync(Expression<Func<TEntity, decimal>> selector, QueryOptions<TEntity>? options = null, CancellationToken cancellationToken = default)
        => _inner.SumAsync(selector, options, cancellationToken);

    public Task<decimal> SumAsync(Expression<Func<TEntity, decimal>> selector, ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
        => _inner.SumAsync(selector, specification, cancellationToken);

    public Task<TEntity> AddAsync(TEntity entity, Guid? userId = default, string? userName = default, CancellationToken cancellationToken = default)
        => _inner.AddAsync(entity, userId, userName, cancellationToken);

    public Task AddRangeAsync(IEnumerable<TEntity> entities, Guid? userId = default, string? userName = default, CancellationToken cancellationToken = default)
        => _inner.AddRangeAsync(entities, userId, userName, cancellationToken);

    public Task UpdateRangeAsync(Expression<Func<TEntity, bool>>? condition = default, params (LambdaExpression propertyExpression, LambdaExpression valueExpression)[] updateData)
        => _inner.UpdateRangeAsync(condition, updateData);

    public Task UpdateRangeAsync(QueryOptions<TEntity> options, params (LambdaExpression propertyExpression, LambdaExpression valueExpression)[] updateData)
        => _inner.UpdateRangeAsync(options, updateData);

    public Task UpdateRangeAsync(ISpecification<TEntity> specification, params (LambdaExpression propertyExpression, LambdaExpression valueExpression)[] updateData)
        => _inner.UpdateRangeAsync(specification, updateData);

    public Task RemoveAsync(TEntity entity, Guid? userId, bool hard = false, CancellationToken cancellationToken = default)
        => _inner.RemoveAsync(entity, userId, hard, cancellationToken);

    public Task RemoveAsync(TEntity entity, bool hard = false, CancellationToken cancellationToken = default)
        => _inner.RemoveAsync(entity, hard, cancellationToken);

    public Task RemoveRangeAsync(IEnumerable<TEntity> entities, bool hard = false, CancellationToken cancellationToken = default)
        => _inner.RemoveRangeAsync(entities, hard, cancellationToken);

    public Task RemovePermanentRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        => _inner.RemovePermanentRangeAsync(entities, cancellationToken);

    public Task RemoveRangeAsync(QueryOptions<TEntity> options, bool hard = false, CancellationToken cancellationToken = default)
        => _inner.RemoveRangeAsync(options, hard, cancellationToken);

    public Task RemoveRangeAsync(Expression<Func<TEntity, bool>> conditions, bool hard = false, CancellationToken cancellationToken = default)
        => _inner.RemoveRangeAsync(conditions, hard, cancellationToken);

    public Task RemoveRangeAsync(ISpecification<TEntity> specification, bool hard = false, CancellationToken cancellationToken = default)
        => _inner.RemoveRangeAsync(specification, hard, cancellationToken);

    public void Execute(Action process, bool useTransaction = false) => _inner.Execute(process, useTransaction);

    public TResult Execute<TResult>(Func<TResult> process, bool useTransaction = false) => _inner.Execute(process, useTransaction);

    public Task ExecuteAsync(Func<Task> process, bool useTransaction = false, CancellationToken cancellationToken = default)
        => _inner.ExecuteAsync(process, useTransaction, cancellationToken);

    public Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> process, bool useTransaction = false, CancellationToken cancellationToken = default)
        => _inner.ExecuteAsync(process, useTransaction, cancellationToken);

    public IQueryable<TEntity> Set(QueryOptions<TEntity>? options = null) => _inner.Set(options);

    public void SaveChanges() => _inner.SaveChanges();

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => _inner.SaveChangesAsync(cancellationToken);
}
