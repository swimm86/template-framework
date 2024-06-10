// ----------------------------------------------------------------------------------------------
// <copyright file="EfCoreDependencyInjectorBase.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Dal.Repository.Interfaces;
using Shared.Application.Core.DependencyInjection;
using Shared.Infrastructure.Dal.EFCore.Repository;

namespace Shared.Infrastructure.DAL.EFCore;

public abstract class EfCoreDependencyInjectorBase(ILogger? logger) : DependencyInjectorBase(logger)
{
    protected static string AssemblyName => Assembly.GetCallingAssembly().FullName!;
    protected virtual Action<DbContextOptionsBuilder>? DbConfigurationOptions => default;

    protected override IServiceCollection Process(IServiceCollection serviceCollection)
    {
        return AddPostgresDbContext(serviceCollection)
            .AddSingleton<IQueryEvaluator, EfQueryEvaluator>();
    }

    protected abstract IServiceCollection AddPostgresDbContext(IServiceCollection serviceCollection);
}
