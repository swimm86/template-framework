// ----------------------------------------------------------------------------------------------
// <copyright file="WorkSeed.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Domain.Entities;
using Shared.Application.Core.Dal.DbSeeder.Attributes;
using Shared.Application.Core.Dal.DbSeeder.Interfaces;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;

namespace Gpn.Template.DatabaseUpgrade.Seeds;

/// <summary>
/// Seed, который добавляет сущности "Work".
/// </summary>
[Seed("work", 0)]
public class WorkSeed : ISeed
{
    /// <inheritdoc />
    public async Task SeedAsync(IUnitOfWork unitOfWork)
    {
        await unitOfWork.GetRepository<Work>()
            .AddRangeAsync(
                Enumerable.Range(0, 100).Select(i => Work.Create(i.ToString())),
                null);
        await unitOfWork.SaveChangesAsync();
    }
}
