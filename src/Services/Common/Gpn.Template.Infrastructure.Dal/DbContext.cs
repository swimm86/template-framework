// ----------------------------------------------------------------------------------------------
// <copyright file="DbContext.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Infrastructure.Dal.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Hosting;
using Shared.Application.Core.Auth;
using Shared.Common.Extensions;
using Shared.Domain.Core.Interfaces;
using Shared.Infrastructure.Dal.EFCore;

namespace Gpn.Template.Infrastructure.Dal;

/// <summary>
/// Реализация <see cref="DbContext"/> для нашего приложения (общий для getter и setter).
/// </summary>
public class DbContext(
    DbContextOptions<DbContext> options,
    IHostEnvironment environment)
    : DbContextBase(options, environment)
{
    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Conventions.Add(_ => new ColumnsNamesConvention());
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }

    /// <inheritdoc/>
    protected override Task BeforeSaveActionAsync(CancellationToken cancellationToken = default)
    {
        var userProvider = this.GetService<IUserProvider>();
        var userId = userProvider.UserId;
        ChangeTracker
            .Entries()
            .ForEach(e =>
            {
                if (e is { Entity: IWithCreated withCreated, State: EntityState.Added })
                {
                    withCreated.SetCreatedByUserId(userId);
                }

                if (e is { Entity: IWithUpdated withUpdated, State: EntityState.Modified })
                {
                    withUpdated.SetUpdatedByUserId(userId);
                }
            });

        return Task.CompletedTask;
    }
}
