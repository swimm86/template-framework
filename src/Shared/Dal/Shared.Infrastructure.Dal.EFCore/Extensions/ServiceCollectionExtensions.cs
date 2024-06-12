// ----------------------------------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.Dal.Settings;
using Shared.Application.Core.Dal.UnitOfWork.Interfaces;
using Shared.Infrastructure.Dal.EFCore.Interfaces;

namespace Shared.Infrastructure.Dal.EFCore.Extensions;

/// <summary>
/// Методы расширения для <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Метод расширения для IServiceCollection, который добавляет Postgres DbContext.
    /// </summary>
    /// <param name="serviceCollection">Коллекция сервисов для регистрации.</param>
    /// <param name="migrationAssemblyName">Название сборки, в которой хранятся миграции.</param>
    /// <typeparam name="TSettings">Тип настроек для базы данных.</typeparam>
    /// <typeparam name="TContext">Тип контекста данных.</typeparam>
    /// <returns>Измененная коллекция сервисов с добавленным Postgres DbContext.</returns>
    public static IServiceCollection AddDbContext<TSettings, TContext>(
        this IServiceCollection serviceCollection,
        string migrationAssemblyName)
        where TSettings : DbSettingsBase
        where TContext : DbContextBase
    {
        using var serviceProvider = serviceCollection.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var dbContextOptionsBuilderInitializer =
            scope.ServiceProvider.GetService<IDbContextOptionsBuilderInitializer>()!;

        return serviceCollection
            .AddDbContextFactory<TContext>(opt =>
                dbContextOptionsBuilderInitializer.Initialize<TSettings>(opt, migrationAssemblyName))
            .AddTransient<IUnitOfWork, EfUnitOfWork<TContext>>();
    }
}
