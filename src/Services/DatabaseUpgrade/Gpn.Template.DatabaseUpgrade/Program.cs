// ----------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Application.Core.Configuration.Extensions;
using Shared.Application.Core.Dal.DbUpdater.Interfaces;
using Shared.Infrastructure.Core;
using Shared.Infrastructure.Dal.EFCore.Attributes;

[assembly: MigrationAssembly]
var builder = CreateHostBuilder(args);
using var host = builder.Build();
var dbUpdater = host.Services.GetRequiredService<IDbUpdater>();
dbUpdater.CreateDbIfNotExists();
dbUpdater.Migrate();
dbUpdater.Initialize();
return;

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((hostContext, config) =>
        {
            config
                .InitializeConfiguration(hostContext.HostingEnvironment);
        })
        .ConfigureServices((_, services) =>
        {
            services
                .ImplementReferencedInfrastructures();
        });
