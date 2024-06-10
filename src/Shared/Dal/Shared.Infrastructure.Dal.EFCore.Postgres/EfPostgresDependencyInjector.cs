// ----------------------------------------------------------------------------------------------
// <copyright file="HotDocDbDependencyInjector.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.DAL.EFCore;

namespace Shared.Infrastructure.Dal.EFCore.Postgres;

public abstract class EfPostgresDependencyInjector(
    IConfiguration configuration,
    ILogger logger
) : EfCoreDependencyInjectorBase(logger)
{
    protected override IServiceCollection Process(IServiceCollection serviceCollection)
    {
        base.Process(serviceCollection);
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        return serviceCollection;
    }

    protected override IServiceCollection AddPostgresDbContext(IServiceCollection serviceCollection)
    {
        return serviceCollection.AddPostgresDbContext<DbContextBase>(
            configuration,
            AssemblyName,
            DbConfigurationOptions);
    }
}
