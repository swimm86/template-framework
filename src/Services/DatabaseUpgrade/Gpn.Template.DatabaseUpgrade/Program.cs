// ----------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Application.Core.Configuration.Attributes;
using Shared.Application.Core.Configuration.Extensions;
using Shared.Application.Core.Dal.DbSeeder.Interfaces;
using Shared.Infrastructure.Core;

[assembly: EnvPath("./.env")]
var builder = CreateHostBuilder(args);
using var host = builder.Build();
var dbSeeder = host.Services.GetRequiredService<IDbSeeder>();
dbSeeder.CreateDbIfNotExists();
dbSeeder.Migrate();
dbSeeder.Initialize();
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
