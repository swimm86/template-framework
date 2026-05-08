// ----------------------------------------------------------------------------------------------
// <copyright file="PersonSeed.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dal.DbSeeder.Attributes;
using Shared.Application.Core.Dal.DbSeeder.Interfaces;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Template.Domain.Entities;

namespace Template.DatabaseUpgrade.Seeds;

/// <summary>
/// Seed, который добавляет сущности "Person".
/// </summary>
[Seed("person", 0)]
public class PersonSeed(
    IUnitOfWork unitOfWork)
    : ISeed
{
    /// <inheritdoc />
    public async Task SeedAsync()
    {
        await unitOfWork.GetRepository<Person>()
            .AddRangeAsync(Enumerable
                .Range(0, 100)
                .Select(i => Person.Create(
                    name: i.ToString(),
                    email: $"email{i}")));

        await unitOfWork.SaveChangesAsync();
    }
}
