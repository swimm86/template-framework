// ----------------------------------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.Dal.UnitOfWork.Interfaces;
using Shared.Application.Core.DependencyInjection.Attributes;
using Shared.Common.Extensions;
using Shared.Common.Helpers;
using Shared.Infrastructure.Dal.EFCore.Interfaces;
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
            .AddDbContextFactory<TContext>(opt =>
                dbContextOptionsBuilderInitializer.Initialize<TSettings>(opt, migrationAssemblyName))
            .AddTransient<IUnitOfWork, EfUnitOfWork<TContext>>();
    }

    /// <summary>
    /// Метод расширения для регистрации всех производных DbContext в IServiceCollection.
    /// </summary>
    /// <param name="serviceCollection">Коллекция сервисов для регистрации.</param>
    /// <returns>Измененная коллекция сервисов с добавленными контекстами данных.</returns>
    public static IServiceCollection AddDbContexts(
        this IServiceCollection serviceCollection)
    {
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

                typeof(ServiceCollectionExtensions)
                    .GetMethods()
                    .First(m => m is { Name: nameof(AddDbContext), IsGenericMethod: true })
                    .MakeGenericMethod(settings, type)
                    .Invoke(null, [serviceCollection, type.Assembly.FullName]);
            });

        return serviceCollection;
    }
}
