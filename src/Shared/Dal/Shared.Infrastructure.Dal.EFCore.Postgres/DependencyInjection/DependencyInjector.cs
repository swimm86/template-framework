// ----------------------------------------------------------------------------------------------
// <copyright file="DependencyInjector.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Dal.EFCore.DependencyInjection.Base;
using Shared.Infrastructure.Dal.EFCore.Interfaces;

namespace Shared.Infrastructure.Dal.EFCore.Postgres.DependencyInjection;

/// <summary>
/// Регистрация DI-зависимостей слоя <c>Shared.Infrastructure.Dal.EFCore.Postgres</c>.
/// </summary>
/// <inheritdoc cref="EfCoreDependencyInjectorBase"/>
/// <param name="loggerFactory"><inheritdoc cref="EfCoreDependencyInjectorBase(ILoggerFactory)" path="/param[@name='loggerFactory']"/></param>
public class DependencyInjector(
    ILoggerFactory loggerFactory)
    : EfCoreDependencyInjectorBase(loggerFactory)
{
    /// <inheritdoc />
    protected override IServiceCollection Process(
        IServiceCollection serviceCollection)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        serviceCollection
            .AddTransient<IDbContextOptionsBuilderInitializer, DbContextOptionsBuilderInitializer>();
        base.Process(serviceCollection);
        return serviceCollection;
    }
}
