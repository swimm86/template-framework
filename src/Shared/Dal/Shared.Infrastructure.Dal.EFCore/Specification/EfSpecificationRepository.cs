//-----------------------------------------------------------------------------------------------
// <copyright file="EfSpecificationRepository.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dal.Repository.Interfaces;
using Shared.Application.Core.Dal.Specification.Interfaces;
using Shared.Domain.Core.Interfaces;
using Shared.Infrastructure.Dal.EFCore.Repository;

namespace Shared.Infrastructure.Dal.EFCore.Specification;

/// <summary>
/// Реализация интерфейса <see cref="ISpecificationRepository{TEntity}"/> на основе ORM "Entity Framework Core".
/// </summary>
/// <typeparam name="TEntity">Тип сущности.</typeparam>
/// <param name="dbContext"><see cref="DbContext"/>.</param>
/// <param name="evaluator"><see cref="IQueryEvaluator"/>.</param>
public class EfSpecificationRepository<TEntity>(
    DbContext dbContext,
    IQueryEvaluator evaluator)
    : EfRepository<TEntity>(dbContext, evaluator), ISpecificationRepository<TEntity>
    where TEntity : class, IEntity
{
    /// <inheritdoc />
    public Task<TEntity?> GetAsync(object id, ISpecification<TEntity>? options = null) =>
        GetAsync(id, options?.BuildOptions());

    /// <inheritdoc />
    public Task<List<TEntity>> GetRangeAsync(ISpecification<TEntity> options, int? skip = null, int? take = null) =>
        GetRangeAsync(options.BuildOptions(), skip, take);

    /// <inheritdoc />
    public Task<List<TOut>> GetRangeAsync<TOut>(ISpecification<TEntity> options, int? skip = null, int? take = null) =>
        GetRangeAsync<TOut>(options.BuildOptions(), skip, take);

    /// <inheritdoc />
    public Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> options) =>
        FirstOrDefaultAsync(options.BuildOptions());

    /// <inheritdoc />
    public Task<TEntity?> SingleOrDefaultAsync(ISpecification<TEntity> options) =>
        SingleOrDefaultAsync(options.BuildOptions());

    /// <inheritdoc />
    public Task<TEntity?> LastOrDefaultAsync(ISpecification<TEntity> options) =>
        LastOrDefaultAsync(options.BuildOptions());

    /// <inheritdoc />
    public Task<int> CountAsync(ISpecification<TEntity> options) =>
        CountAsync(options.BuildOptions());

    /// <inheritdoc />
    public Task RemoveRangeAsync(ISpecification<TEntity> options) =>
        RemoveRangeAsync(options.BuildOptions());
}
