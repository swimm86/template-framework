// ----------------------------------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
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
    /// Метод расширения для регистрации всех производных DbContext в IServiceCollection.
    /// </summary>
    /// <param name="serviceCollection">Коллекция сервисов для регистрации.</param>
    /// <returns>Измененная коллекция сервисов с добавленными контекстами данных.</returns>
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
                    throw new InvalidOperationException($"Не удалось найти настройки для типа {type.FullName}");
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
