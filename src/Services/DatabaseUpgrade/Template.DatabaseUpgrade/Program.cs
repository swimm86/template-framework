// ----------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Application.Core.Configuration.Extensions;
using Shared.Application.Core.Dal.DbUpdater.Interfaces;
using Shared.Common.Attributes;
using Shared.Infrastructure.Core.DependencyInjection.Extensions;
using Shared.Infrastructure.Dal.EFCore.Attributes;

[assembly: MigrationAssembly]
[assembly: StartupAssembly]
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
                .AddReferencedDependencyInjectors();
        });
