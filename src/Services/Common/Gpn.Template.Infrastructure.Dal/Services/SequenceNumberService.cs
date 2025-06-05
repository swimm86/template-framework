// ----------------------------------------------------------------------------------------------
// <copyright file="SequenceNumberService.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Interfaces;

namespace Gpn.Template.Infrastructure.Dal.Services;

/// <inheritdoc/>
public class SequenceNumberService<TEntity> : ISequenceNumberService<TEntity>
    where TEntity : class
{
    /// <inheritdoc/>
    public async Task SetSequenceNumberAsync(
        Microsoft.EntityFrameworkCore.DbContext context,
        CancellationToken cancellationToken)
    {
        var entries = context.ChangeTracker.Entries()
            .Where(x => x.Entity is IWithSequenceNumber<TEntity>)
            .Where(x => x.State == Microsoft.EntityFrameworkCore.EntityState.Added)
            .ToList();

        var localEntities = entries.Select(x => x.Entity as TEntity);

        foreach (var entry in entries)
        {
            var set = context.Set<TEntity>();
            var entity = (entry.Entity as IWithSequenceNumber<TEntity>)!;

            var maxInDb = set.Where(entity.FilterExpression)
                .Max(x => (x as IWithSequenceNumber<TEntity>)!.SequenceNumber) ?? 0;

            var maxLocal = localEntities.Where(entity.FilterExpression.Compile()!)
                .Max(x => (x as IWithSequenceNumber<TEntity>)!.SequenceNumber) ?? 0;

            entity.SequenceNumber = Math.Max(maxInDb, maxLocal) + 1;

            await set.AddAsync((TEntity)entity, cancellationToken);
        }
    }
}
