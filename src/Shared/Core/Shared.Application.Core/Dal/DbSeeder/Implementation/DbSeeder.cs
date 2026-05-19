// ----------------------------------------------------------------------------------------------
// <copyright file="DbSeeder.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Dal.DbSeeder.Attributes;
using Shared.Application.Core.Dal.DbSeeder.Entities;
using Shared.Application.Core.Dal.DbSeeder.Interfaces;
using Shared.Common.Extensions;
using Shared.Common.Helpers;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;

namespace Shared.Application.Core.Dal.DbSeeder.Implementation;

/// <summary>
/// Сервис для применения seed-процессов к базе данных.
/// </summary>
public class DbSeeder(
    IUnitOfWork unitOfWork,
    IServiceProvider serviceProvider,
    ILogger<DbSeeder>? logger = null)
    : IDbSeeder
{
    /// <inheritdoc />
    public async Task ApplySeedsAsync(CancellationToken cancellationToken)
    {
        var repo = unitOfWork.GetRepository<Seed>();
        var seedNames = await repo.GetRangeAsync(cancellationToken: cancellationToken);
        var seeds = AssemblyHelper.GetDerivedTypesFromAssemblies<ISeed>([typeof(SeedAttribute)])
            .Select(type => new { type, attr = type.GetCustomAttribute<SeedAttribute>() })
            .Where(x => x.attr != null && !seedNames.Any(seed => seed.Name.Equals(x.attr.Name)))
            .OrderBy(x => x.attr!.Order)
            .ToList();
        await seeds.ForeachAsync(
            async x =>
            {
                var seedName = x.attr!.Name;
                logger?.LogInformation($"Seed {seedName} started");
                ((ISeed)ActivatorUtilities.CreateInstance(serviceProvider, x.type)).SeedAsync().GetAwaiter().GetResult();
                await repo.AddAsync(Seed.Create(x.attr!.Name), cancellationToken: cancellationToken);
                logger?.LogInformation($"Seed {seedName} completed");
            },
            cancellationToken: cancellationToken);

        if (seeds.Any())
        {
            await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
        }
    }
}