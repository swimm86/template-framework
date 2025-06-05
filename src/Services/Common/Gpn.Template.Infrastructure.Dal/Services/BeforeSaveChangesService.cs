// ----------------------------------------------------------------------------------------------
// <copyright file="BeforeSaveChangesService.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Domain.Entities;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Dal.EFCore.Interfaces;

namespace Gpn.Template.Infrastructure.Dal.Services;

/// <summary>
/// .
/// </summary>
/// <param name="logger">.</param>
public class BeforeSaveChangesService(
    ILogger<BeforeSaveChangesService> logger,
    ISequenceNumberService<Person> sequenceNumberService) : IBeforeSaveChangesService
{
    /// <inheritdoc/>
    public void Process(Microsoft.EntityFrameworkCore.DbContext dbContext)
    {
        ProcessAsync(dbContext).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public Task ProcessAsync(Microsoft.EntityFrameworkCore.DbContext dbContext, CancellationToken cancellationToken = default)
    {
        return SetSequenceNumber(dbContext, cancellationToken);
    }

    /// <summary>
    /// .
    /// </summary>
    /// <param name="dbContext">.</param>
    /// <param name="cancellationToken">.</param>
    /// <returns>.</returns>
    private async Task SetSequenceNumber(
        Microsoft.EntityFrameworkCore.DbContext dbContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var task = sequenceNumberService?.SetSequenceNumberAsync(dbContext, cancellationToken);
            if (task is not null)
            {
                await task;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning($"Ошибка при работе SequenceNumberService. {ex.Message}");
        }
    }
}
