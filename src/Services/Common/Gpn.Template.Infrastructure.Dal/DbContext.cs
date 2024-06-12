// ----------------------------------------------------------------------------------------------
// <copyright file="DbContext.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Shared.Application.Core.Dal.Repository.Interfaces;
using Shared.Infrastructure.Dal.EFCore;

namespace Gpn.Template.Infrastructure.Dal;

/// <summary>
/// Реализация <see cref="DbContext"/> для нашего приложения (общий для getter и setter).
/// </summary>
public class DbContext(
    DbContextOptions<DbContext> options,
    IQueryEvaluator evaluator,
    IHostEnvironment environment)
    : DbContextBase(options, evaluator, environment)
{
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
    }

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }
}
