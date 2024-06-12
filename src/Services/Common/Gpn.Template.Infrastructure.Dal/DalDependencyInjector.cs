// ----------------------------------------------------------------------------------------------
// <copyright file="DalDependencyInjector.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;
using Gpn.Template.Domain.Entities;
using Gpn.Template.Infrastructure.Dal.Repositories;
using Gpn.Template.Infrastructure.Dal.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Dal.Repository.Interfaces;
using Shared.Application.Core.DependencyInjection;
using Shared.Infrastructure.Dal.EFCore.Extensions;

namespace Gpn.Template.Infrastructure.Dal;

/// <summary>
/// Внедрение зависимостей для DAL-слоя.
/// </summary>
public class DalDependencyInjector(
    ILogger<DalDependencyInjector> logger)
    : DependencyInjectorBase(logger)
{
    /// <inheritdoc />
    protected override IServiceCollection Process(IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddDbContext<DbSettings, DbContext>(Assembly.GetExecutingAssembly().FullName!)
            .AddTransient<IRepository<Person>, PersonRepository>();
    }
}
