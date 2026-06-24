using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Shared.Domain.Core.Dal;
using Shared.Domain.Core.Dal.Repository;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Dal.Specification.Interfaces;
using Shared.Domain.Core.Interfaces;

namespace Shared.Testing.Doubles.Repository;

public sealed class FakeRepository<TEntity>
    : IRepository<TEntity>
    where TEntity : class, IEntity
{
    private readonly ConcurrentDictionary<object, TEntity> _storage = new();

    public Func<TEntity, object>? KeySelector { get; set; }

    /// <summary>
    /// Преобразователь сущности <typeparamref name="TEntity"/> в проекцию,
    /// используемый в <c>GetRangeAsync&lt;TOut&gt;</c> при отсутствии явного <c>selector</c>.
    /// </summary>
    /// <remarks>
    /// Если значение не задано, используется прямое приведение типа.
    /// Задаётся в тестах, где бизнес-код проецирует сущности на DTO без явного <c>selector</c>
    /// (например, при вызове <c>repo.GetRangeAsync&lt;PersonListPayload&gt;()</c>).
    /// </remarks>
    public Func<TEntity, object>? PayloadMapper { get; set; }

    public Exception? ExceptionToThrowOnGet { get; set; }
    public Exception? ExceptionToThrowOnAdd { get; set; }
    public Exception? ExceptionToThrowOnRemove { get; set; }
    public Exception? ExceptionToThrowOnSaveChanges { get; set; }

    public int RemoveCallCount { get; private set; }

    public IReadOnlyCollection<TEntity> Items => _storage.Values.ToList().AsReadOnly();

    public void AddDirect(TEntity entity)
    {
        var key = KeySelector?.Invoke(entity) ?? entity.Id;
        _storage[key] = entity;
    }

    private object GetKey(TEntity entity) => KeySelector?.Invoke(entity) ?? entity.Id;

    private void ThrowIfConfigured(Exception? exception)
    {
        if (exception is not null)
            throw exception;
    }

    #region Read methods

    public Task<TEntity?> GetAsync(
        object id,
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfConfigured(ExceptionToThrowOnGet);
        ArgumentNullException.ThrowIfNull(id);

        if (!_storage.TryGetValue(id, out var entity))
        {
            return Task.FromResult<TEntity?>(null);
        }

        return Task.FromResult(MatchesOptions(entity, options) ? entity : null);
    }

    public Task<TEntity?> GetAsync(
        object id,
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
        => GetAsync(id, specification.BuildOptions(), cancellationToken);

    public Task<TOut?> GetAsync<TOut>(
        object id,
        QueryOptions<TEntity>? options = null,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfConfigured(ExceptionToThrowOnGet);
        ArgumentNullException.ThrowIfNull(id);

        if (!_storage.TryGetValue(id, out var entity) || !MatchesOptions(entity, options))
        {
            return Task.FromResult<TOut?>(default);
        }

        if (selector is not null)
        {
            return Task.FromResult<TOut?>(selector.Compile()(entity));
        }

        return Task.FromResult((TOut?)(object)entity);
    }

    public Task<TOut?> GetAsync<TOut>(
        object? id,
        ISpecification<TEntity> specification,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default)
        => GetAsync(id, specification.BuildOptions(), selector, cancellationToken);

    public Task<List<TEntity>> GetRangeAsync(
        QueryOptions<TEntity>? options = null,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfConfigured(ExceptionToThrowOnGet);
        var query = ApplyOptions(_storage.Values.AsQueryable(), options);
        if (skip.HasValue) query = query.Skip(skip.Value);
        if (take.HasValue) query = query.Take(take.Value);
        return Task.FromResult(query.ToList());
    }

    public Task<List<TEntity>> GetRangeAsync(
        ISpecification<TEntity> specification,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default)
        => GetRangeAsync(specification.BuildOptions(), skip, take, cancellationToken);

    public Task<List<TOut>> GetRangeAsync<TOut>(
        QueryOptions<TEntity>? options = null,
        int? skip = null,
        int? take = null,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfConfigured(ExceptionToThrowOnGet);
        var query = ApplyOptions(_storage.Values.AsQueryable(), options);
        if (skip.HasValue) query = query.Skip(skip.Value);
        if (take.HasValue) query = query.Take(take.Value);
        var compiledSelector = selector?.Compile()
            ?? (PayloadMapper is not null
                ? new Func<TEntity, TOut>(e => (TOut)PayloadMapper(e))
                : (e => (TOut)(object)e));
        return Task.FromResult(query.Select(compiledSelector).ToList());
    }

    public Task<List<TOut>> GetRangeAsync<TOut>(
        ISpecification<TEntity> specification,
        int? skip = null,
        int? take = null,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default)
        => GetRangeAsync(specification.BuildOptions(), skip, take, selector, cancellationToken);

    public Task<TEntity?> FirstOrDefaultAsync(
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfConfigured(ExceptionToThrowOnGet);
        return Task.FromResult(ApplyOptions(_storage.Values.AsQueryable(), options).FirstOrDefault());
    }

    public Task<TEntity?> FirstOrDefaultAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
        => FirstOrDefaultAsync(specification.BuildOptions(), cancellationToken);

    public Task<TOut?> FirstOrDefaultAsync<TOut>(
        QueryOptions<TEntity>? options = null,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfConfigured(ExceptionToThrowOnGet);
        var query = ApplyOptions(_storage.Values.AsQueryable(), options);
        var entity = query.FirstOrDefault();
        if (entity is null)
        {
            return Task.FromResult<TOut?>(default);
        }

        if (selector is not null)
        {
            return Task.FromResult<TOut?>(selector.Compile()(entity));
        }

        return Task.FromResult((TOut?)(object)entity);
    }

    public Task<TOut?> FirstOrDefaultAsync<TOut>(
        ISpecification<TEntity> specification,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default)
        => FirstOrDefaultAsync(specification.BuildOptions(), selector, cancellationToken);

    public Task<TEntity?> SingleOrDefaultAsync(
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfConfigured(ExceptionToThrowOnGet);
        return Task.FromResult(ApplyOptions(_storage.Values.AsQueryable(), options).SingleOrDefault());
    }

    public Task<TEntity?> SingleOrDefaultAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
        => SingleOrDefaultAsync(specification.BuildOptions(), cancellationToken);

    public Task<TOut?> SingleOrDefaultAsync<TOut>(
        QueryOptions<TEntity>? options = null,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfConfigured(ExceptionToThrowOnGet);
        var query = ApplyOptions(_storage.Values.AsQueryable(), options);
        var entity = query.SingleOrDefault();
        if (entity is null)
            return Task.FromResult<TOut?>(default);
        if (selector is not null)
            return Task.FromResult<TOut?>(selector.Compile()(entity));
        return Task.FromResult((TOut?)(object)entity);
    }

    public Task<TOut?> SingleOrDefaultAsync<TOut>(
        ISpecification<TEntity> specification,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default)
        => SingleOrDefaultAsync(specification.BuildOptions(), selector, cancellationToken);

    public Task<TEntity?> LastOrDefaultAsync(
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfConfigured(ExceptionToThrowOnGet);
        return Task.FromResult(ApplyOptions(_storage.Values.AsQueryable(), options).LastOrDefault());
    }

    public Task<TEntity?> LastOrDefaultAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
        => LastOrDefaultAsync(specification.BuildOptions(), cancellationToken);

    public Task<TOut?> LastOrDefaultAsync<TOut>(
        QueryOptions<TEntity>? options = null,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfConfigured(ExceptionToThrowOnGet);
        var query = ApplyOptions(_storage.Values.AsQueryable(), options);
        var entity = query.LastOrDefault();
        if (entity is null)
        {
            return Task.FromResult<TOut?>(default);
        }

        if (selector is not null)
        {
            return Task.FromResult<TOut?>(selector.Compile()(entity));
        }

        return Task.FromResult((TOut?)(object)entity);
    }

    public Task<TOut?> LastOrDefaultAsync<TOut>(
        ISpecification<TEntity> specification,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default)
        => LastOrDefaultAsync(specification.BuildOptions(), selector, cancellationToken);

    #endregion

    #region Aggregation methods

    public Task<int> CountAsync(
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfConfigured(ExceptionToThrowOnGet);
        return Task.FromResult(ApplyOptions(_storage.Values.AsQueryable(), options).Count());
    }

    public Task<int> CountAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
        => CountAsync(specification.BuildOptions(), cancellationToken);

    public Task<bool> AnyAsync(
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfConfigured(ExceptionToThrowOnGet);
        return Task.FromResult(ApplyOptions(_storage.Values.AsQueryable(), options).Any());
    }

    public Task<bool> AnyAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
        => AnyAsync(specification.BuildOptions(), cancellationToken);

    public Task<decimal> SumAsync(
        Expression<Func<TEntity, decimal>> selector,
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfConfigured(ExceptionToThrowOnGet);
        return Task.FromResult(ApplyOptions(_storage.Values.AsQueryable(), options).Sum(selector.Compile()));
    }

    public Task<decimal> SumAsync(
        Expression<Func<TEntity, decimal>> selector,
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
        => SumAsync(selector, specification.BuildOptions(), cancellationToken);

    #endregion

    #region Add methods

    public Task<TEntity> AddAsync(
        TEntity entity,
        Guid? userId = null,
        string? userName = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfConfigured(ExceptionToThrowOnAdd);
        var key = GetKey(entity);
        _storage[key] = entity;
        return Task.FromResult(entity);
    }

    public Task AddRangeAsync(
        IEnumerable<TEntity> entities,
        Guid? userId = null,
        string? userName = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfConfigured(ExceptionToThrowOnAdd);
        foreach (var entity in entities)
        {
            _storage[GetKey(entity)] = entity;
        }
        return Task.CompletedTask;
    }

    #endregion

    #region Update methods

    public Task ExecuteUpdateRangeAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        params (LambdaExpression propertyExpression,
            LambdaExpression valueExpression)[] updateData)
    {
        var query = _storage.Values.AsQueryable();
        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        var entities = query.ToList();
        foreach (var entity in entities)
        {
            foreach (var (propExpr, valExpr) in updateData)
            {
                if (propExpr.Body is MemberExpression { Member: PropertyInfo propInfo })
                {
                    var value = valExpr.Compile().DynamicInvoke(entity);
                    propInfo.SetValue(entity, value);
                }
            }
        }
        return Task.CompletedTask;
    }

    public Task ExecuteUpdateRangeAsync(
        QueryOptions<TEntity> options,
        params (LambdaExpression propertyExpression, LambdaExpression valueExpression)[] updateData)
    {
        var query = ApplyOptions(_storage.Values.AsQueryable(), options);
        var entities = query.ToList();
        foreach (var entity in entities)
        {
            foreach (var (propExpr, valExpr) in updateData)
            {
                if (propExpr.Body is MemberExpression { Member: PropertyInfo propInfo })
                {
                    var value = valExpr.Compile().DynamicInvoke(entity);
                    propInfo.SetValue(entity, value);
                }
            }
        }
        return Task.CompletedTask;
    }

    public Task ExecuteUpdateRangeAsync(
        ISpecification<TEntity> specification,
        params (LambdaExpression propertyExpression, LambdaExpression valueExpression)[] updateData)
        => ExecuteUpdateRangeAsync(specification.BuildOptions(), updateData);

    #endregion

    #region Grouping methods

    public Task<List<IGrouping<TKey, TEntity>>> GetGroupingAsync<TKey>(
        Expression<Func<TEntity, TKey>> keySelector,
        QueryOptions<TEntity>? options = null,
        int? skip = null,
        int? take = null,
        OrderDirectionType? groupKeyOrderDirection = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfConfigured(ExceptionToThrowOnGet);
        GroupingPagingGuard.EnsureGroupOrderingForPaging(skip, take, groupKeyOrderDirection);
        var grouped = ApplyOptions(_storage.Values.AsQueryable(), options).GroupBy(keySelector);
        grouped = ApplyGroupKeyOrdering(grouped, groupKeyOrderDirection);
        var query = grouped;
        if (skip.HasValue) query = query.Skip(skip.Value);
        if (take.HasValue) query = query.Take(take.Value);
        return Task.FromResult(query.ToList());
    }

    public Task<List<TOut>> GetGroupingAsync<TKey, TOut>(
        Expression<Func<TEntity, TKey>> keySelector,
        QueryOptions<TEntity>? options = null,
        int? skip = null,
        int? take = null,
        Expression<Func<IGrouping<TKey, TEntity>, TOut>>? selector = null,
        OrderDirectionType? groupKeyOrderDirection = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfConfigured(ExceptionToThrowOnGet);
        GroupingPagingGuard.EnsureGroupOrderingForPaging(skip, take, groupKeyOrderDirection);
        var grouped = ApplyOptions(_storage.Values.AsQueryable(), options).GroupBy(keySelector);
        grouped = ApplyGroupKeyOrdering(grouped, groupKeyOrderDirection);
        var query = grouped;
        if (skip.HasValue) query = query.Skip(skip.Value);
        if (take.HasValue) query = query.Take(take.Value);
        var selectorFunc = selector?.Compile();
        var projected = selectorFunc is not null
            ? query.Select(selectorFunc)
            : query.Select(g => (TOut)(object)g.ToList());
        return Task.FromResult(projected.ToList());
    }

    public Task<int> CountGroupsAsync<TKey>(
        Expression<Func<TEntity, TKey>> keySelector,
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfConfigured(ExceptionToThrowOnGet);
        return Task.FromResult(ApplyOptions(_storage.Values.AsQueryable(), options).GroupBy(keySelector).Count());
    }

    public Task<int> CountGroupsAsync<TKey>(
        Expression<Func<TEntity, TKey>> keySelector,
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
        => CountGroupsAsync(keySelector, specification.BuildOptions(), cancellationToken);

    public Task<TOut?> MaxAsync<TOut>(
        Expression<Func<TEntity, TOut>> selector,
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfConfigured(ExceptionToThrowOnGet);
        var query = ApplyOptions(_storage.Values.AsQueryable(), options);
        return Task.FromResult(query.Any() ? query.Max(selector) : default);
    }

    public Task<TOut?> MaxAsync<TOut>(
        Expression<Func<TEntity, TOut>> selector,
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
        => MaxAsync(selector, specification.BuildOptions(), cancellationToken);

    public Task<TOut?> MinAsync<TOut>(
        Expression<Func<TEntity, TOut>> selector,
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfConfigured(ExceptionToThrowOnGet);
        var query = ApplyOptions(_storage.Values.AsQueryable(), options);
        return Task.FromResult(query.Any() ? query.Min(selector) : default);
    }

    public Task<TOut?> MinAsync<TOut>(
        Expression<Func<TEntity, TOut>> selector,
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
        => MinAsync(selector, specification.BuildOptions(), cancellationToken);

    private static IQueryable<IGrouping<TKey, TEntity>> ApplyGroupKeyOrdering<TKey>(
        IQueryable<IGrouping<TKey, TEntity>> grouped,
        OrderDirectionType? groupKeyOrderDirection) =>
        groupKeyOrderDirection switch
        {
            OrderDirectionType.Ascending => grouped.OrderBy(g => g.Key),
            OrderDirectionType.Descending => grouped.OrderByDescending(g => g.Key),
            _ => grouped,
        };

    #endregion

    #region Remove methods

    public Task RemoveAsync(
        TEntity entity,
        Guid? userId,
        bool hard = false,
        CancellationToken cancellationToken = default)
    {
        ThrowIfConfigured(ExceptionToThrowOnRemove);
        RemoveCallCount++;
        if (!hard && entity is IWithDeleted deletable)
        {
            deletable.SetIsDeleted();
            deletable.OnDelete(userId);
        }
        else
        {
            _storage.TryRemove(GetKey(entity), out _);
        }
        return Task.CompletedTask;
    }

    public Task RemoveAsync(
        TEntity entity,
        bool hard = false,
        CancellationToken cancellationToken = default)
    {
        return RemoveAsync(entity, userId: null, hard, cancellationToken);
    }

    public Task RemoveRangeAsync(
        IEnumerable<TEntity> entities,
        bool hard = false,
        CancellationToken cancellationToken = default)
    {
        ThrowIfConfigured(ExceptionToThrowOnRemove);
        RemoveCallCount++;
        foreach (var entity in entities)
        {
            if (!hard && entity is IWithDeleted deletable)
            {
                deletable.SetIsDeleted();
                deletable.OnDelete(userId: null);
            }
            else
            {
                _storage.TryRemove(GetKey(entity), out _);
            }
        }
        return Task.CompletedTask;
    }

    public Task RemovePermanentRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        ThrowIfConfigured(ExceptionToThrowOnRemove);
        RemoveCallCount++;
        foreach (var entity in entities)
            _storage.TryRemove(GetKey(entity), out _);
        return Task.CompletedTask;
    }

    public Task RemoveRangeAsync(
        QueryOptions<TEntity> options,
        bool hard = false,
        CancellationToken cancellationToken = default)
    {
        ThrowIfConfigured(ExceptionToThrowOnRemove);
        RemoveCallCount++;
        var toRemove = ApplyOptions(_storage.Values.AsQueryable(), options).ToList();
        foreach (var entity in toRemove)
            _storage.TryRemove(GetKey(entity), out _);
        return Task.CompletedTask;
    }

    public Task RemoveRangeAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool hard = false,
        CancellationToken cancellationToken = default)
    {
        ThrowIfConfigured(ExceptionToThrowOnRemove);
        RemoveCallCount++;
        var toRemove = _storage.Values.Where(predicate.Compile()).ToList();
        foreach (var entity in toRemove)
            _storage.TryRemove(GetKey(entity), out _);
        return Task.CompletedTask;
    }

    public Task RemoveRangeAsync(
        ISpecification<TEntity> specification,
        bool hard = false,
        CancellationToken cancellationToken = default)
        => RemoveRangeAsync(specification.BuildOptions(), hard, cancellationToken);

    public Task ExecuteRemoveRangeAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        ExecuteRemoveRangeAsync(new QueryOptions<TEntity>().AddFilter(predicate), cancellationToken);

    public Task ExecuteRemoveRangeAsync(
        QueryOptions<TEntity> options,
        CancellationToken cancellationToken = default)
    {
        ThrowIfConfigured(ExceptionToThrowOnRemove);
        RemoveCallCount++;
        var toRemove = ApplyOptions(_storage.Values.AsQueryable(), options).ToList();
        foreach (var entity in toRemove)
            _storage.TryRemove(GetKey(entity), out _);
        return Task.CompletedTask;
    }

    public Task ExecuteRemoveRangeAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
        => ExecuteRemoveRangeAsync(specification.BuildOptions(), cancellationToken);

    #endregion

    public void Execute(Action process, bool useTransaction = false)
    {
        Execute<object?>(() => { process(); return null; });
    }

    public TResult Execute<TResult>(Func<TResult> process, bool useTransaction = false)
    {
        return process();
    }

    public Task ExecuteAsync(
        Func<Task> process,
        bool useTransaction = false,
        CancellationToken cancellationToken = default)
        => ExecuteAsync<object?>(
            async () => { await process(); return null; },
            useTransaction,
            cancellationToken);

    public Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> process,
        bool useTransaction = false,
        CancellationToken cancellationToken = default)
    {
        return process();
    }

    public IQueryable<TEntity> Set(QueryOptions<TEntity>? options = null)
    {
        return ApplyOptions(_storage.Values.AsQueryable(), options);
    }

    public void SaveChanges()
    {
        ThrowIfConfigured(ExceptionToThrowOnSaveChanges);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfConfigured(ExceptionToThrowOnSaveChanges);
        return Task.CompletedTask;
    }

    private static bool MatchesOptions(
        TEntity entity,
        QueryOptions<TEntity>? options)
        => ApplyOptions(new[] { entity }.AsQueryable(), options).Any();

    private static IQueryable<TEntity> ApplyOptions(
        IQueryable<TEntity> query,
        QueryOptions<TEntity>? options)
    {
        if (options is null)
        {
            return query;
        }

        query = options.Filters.Aggregate(query, (current, filter) => current.Where(filter));

        query = options.OrderBy.Aggregate(query, (current, order) => order.Direction == OrderDirectionType.Ascending
            ? current.OrderBy(order.Expression)
            : current.OrderByDescending(order.Expression));

        if (options.Distinct)
        {
            query = query.Distinct();
        }

        return query;
    }
}
