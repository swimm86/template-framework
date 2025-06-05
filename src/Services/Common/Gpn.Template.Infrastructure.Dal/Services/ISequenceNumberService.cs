// ----------------------------------------------------------------------------------------------
// <copyright file="ISequenceNumberService.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Gpn.Template.Infrastructure.Dal.Services;

/// <summary>
/// .
/// </summary>
/// <typeparam name="TEntity">.</typeparam>
public interface ISequenceNumberService<TEntity>
{
    /// <summary>
    /// .
    /// </summary>
    /// <param name="context">.</param>
    /// <param name="cancellationToken">.</param>
    /// <returns>.</returns>
    Task SetSequenceNumberAsync(
        Microsoft.EntityFrameworkCore.DbContext context,
        CancellationToken cancellationToken);
}
