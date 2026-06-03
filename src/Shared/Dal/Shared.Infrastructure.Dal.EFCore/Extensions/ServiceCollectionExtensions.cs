// ----------------------------------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.DependencyInjection.Attributes;
using Shared.Common.Extensions;
using Shared.Common.Helpers;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Infrastructure.Dal.EFCore.Attributes;
using Shared.Infrastructure.Dal.EFCore.Interfaces;
using Shared.Infrastructure.Dal.EFCore.Repository;
using Shared.Infrastructure.Dal.EFCore.Settings;

namespace Shared.Infrastructure.Dal.EFCore.Extensions;

/// <summary>
/// Содержит методы расширения для регистрации EF Core зависимостей.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует DbContext, фабрику контекста, репозитории и Unit of Work.
    /// </summary>
    /// <typeparam name="TSettings">Тип настроек для базы данных.</typeparam>
    /// <typeparam name="TContext">Тип контекста данных.</typeparam>
    /// <param name="serviceCollection">Коллекция сервисов <see cref="IServiceCollection"/>.</param>
    /// <param name="migrationAssemblyName">Имя сборки, в которой расположены миграции.</param>
    /// <returns>Текущая коллекция сервисов <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddDbContext<TSettings, TContext>(
        this IServiceCollection serviceCollection,
        string migrationAssemblyName)
        where TSettings : EfDbSettingsBase<TContext>
        where TContext : DbContextBase
    {
        using var serviceProvider = serviceCollection.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var dbContextOptionsBuilderInitializer =
            scope.ServiceProvider.GetService<IDbContextOptionsBuilderInitializer>()!;

        return serviceCollection
            // Регистрируем фабрику контекстов (для безопасного извлечения TContext-ов)
            .AddDbContextFactory<TContext>(opt =>
                dbContextOptionsBuilderInitializer.Initialize<TSettings>(opt, migrationAssemblyName))
            // Регистрируем TContext как scoped сервис через factory для использования в репозиториях
            .AddScoped<TContext>(sp =>
            {
                var factory = sp.GetRequiredService<IDbContextFactory<TContext>>();
                return factory.CreateDbContext();
            })
            // Регистрируем базовый DbContext для использования в EfRepository<>
            .AddScoped<DbContext>(sp => sp.GetRequiredService<TContext>())
            .AddScoped(typeof(IRepository<>), typeof(EfRepository<>))
            .AddScoped<IUnitOfWork, EfUnitOfWork<TContext>>();
    }

    /// <summary>
    /// Регистрирует все производные <see cref="DbContextBase"/> и соответствующие настройки.
    /// </summary>
    /// <param name="serviceCollection">Коллекция сервисов <see cref="IServiceCollection"/>.</param>
    /// <returns>Текущая коллекция сервисов <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddDbContexts(
        this IServiceCollection serviceCollection)
    {
        var migrationAssembly = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(assembly =>
                assembly.GetCustomAttributes(typeof(MigrationAssemblyAttribute), false).Any());
        AssemblyHelper.GetDerivedTypesFromAssemblies<DbContextBase>(
                excludedAttributesTypes: [typeof(ManualConfigurationAttribute)])
            .ForEach(type =>
            {
                var settings = AssemblyHelper
                    .GetDerivedTypesFromAssemblies(
                        typeof(EfDbSettingsBase<>).MakeGenericType(type),
                        excludedAttributesTypes: [typeof(ManualConfigurationAttribute)])
                    .FirstOrDefault();
                if (settings is null)
                {
                    throw new InvalidOperationException($"Failed to find settings for type {type.FullName}");
                }

                var migrationAssemblyName = (migrationAssembly ?? type.Assembly).FullName;
                typeof(ServiceCollectionExtensions)
                    .GetMethods()
                    .First(m => m is { Name: nameof(AddDbContext), IsGenericMethod: true })
                    .MakeGenericMethod(settings, type)
                    .Invoke(null, [serviceCollection, migrationAssemblyName]);
            });

        return serviceCollection;
    }
}
