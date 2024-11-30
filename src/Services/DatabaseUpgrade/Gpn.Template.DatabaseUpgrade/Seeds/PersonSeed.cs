// ----------------------------------------------------------------------------------------------
// <copyright file="PersonSeed.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Domain.Entities;
using Shared.Application.Core.Dal.DbSeeder.Attributes;
using Shared.Application.Core.Dal.DbSeeder.Interfaces;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;

namespace Gpn.Template.DatabaseUpgrade.Seeds;

/// <summary>
/// Seed, который добавляет сущности "Person".
/// </summary>
[Seed("person", 0)]
public class PersonSeed : ISeed
{
    /// <inheritdoc />
    public async Task SeedAsync(IUnitOfWork unitOfWork)
    {
        await unitOfWork.GetRepository<Person>()
            .AddRangeAsync(
                Enumerable.Range(0, 100).Select(i => Person.Create(i.ToString(), $"email{i}")),
                null);
        await unitOfWork.SaveChangesAsync();
    }
}
