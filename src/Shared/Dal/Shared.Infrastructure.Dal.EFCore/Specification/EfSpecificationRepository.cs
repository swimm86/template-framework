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
}
