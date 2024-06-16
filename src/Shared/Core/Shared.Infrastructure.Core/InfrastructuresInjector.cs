// ----------------------------------------------------------------------------------------------
// <copyright file="InfrastructuresInjector.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.DependencyInjection;
using Shared.Common.Helpers;

namespace Shared.Infrastructure.Core;

/// <summary>
/// Класс для добвления инфраструктурных зависимостей.
/// </summary>
public static class InfrastructuresInjector
{
    /// <summary>
    /// Имплементация инфраструктурных зависимостей.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/>.</param>
    /// <returns><see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection ImplementReferencedInfrastructures(this IServiceCollection services)
    {
        DynamicLoadInfrastructureAssemblies();

        using var provider = services.BuildServiceProvider();

        // Сначала имплементируем 'Shared' зависимости, затем конкретного модуля,
        // начиная с более низкого уровня и вплоть до первого.
        // Shared.Application.Core => Shared.Infrastructure.Core => App.Application => App.Infrastructure
        var infrastructureTypes =
            AssemblyHelper.GetDerivedTypesFromAssemblies<DependencyInjectorBase>()
                .OrderBy(x => !x.FullName!.StartsWith(nameof(Shared), StringComparison.OrdinalIgnoreCase))
                .ThenBy(x => !x.FullName!.Contains(nameof(Application), StringComparison.OrdinalIgnoreCase))
                .ThenBy(x => !x.FullName!.Contains(nameof(Infrastructure), StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(x => x.FullName!.Split('.').Length)
                .ToList();
        infrastructureTypes.ForEach(x => services.AddInfrastructure(provider, x));

        return services;
    }

    /// <summary>
    /// Динамическая загрузка (не явная) инфраструктурных зависимостей.
    /// </summary>
    private static void DynamicLoadInfrastructureAssemblies()
    {
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        var loadedPaths = loadedAssemblies.Select(a => a.Location).ToArray();

        var referencedPaths = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll");
        var toLoad = referencedPaths
            .Where(r =>
                !loadedPaths.Contains(r, StringComparer.InvariantCultureIgnoreCase) &&
                r.Contains(nameof(Infrastructure)))
            .ToList();

        Parallel.ForEach(toLoad, path => AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(path)));
    }

    /// <summary>
    /// Добвление инфраструктуры по типу <see cref="infrastructureType"/>.
    /// </summary>
    /// <param name="serviceCollection"><see cref="IServiceCollection"/>.</param>
    /// <param name="provider"><see cref="IServiceProvider"/>.</param>
    /// <param name="infrastructureType">Тип инфраструктуры.</param>
    /// <exception cref="ArgumentException">Ошибка при добавлении некорректной инфраструктуры.</exception>
    private static void AddInfrastructure(
        this IServiceCollection serviceCollection,
        IServiceProvider provider,
        Type infrastructureType)
    {
        if (!typeof(DependencyInjectorBase).IsAssignableFrom(infrastructureType) || !infrastructureType.IsClass ||
            infrastructureType.IsAbstract)
        {
            throw new ArgumentException(
                $"Тип для имплементируемой зависимости должен быть не абстрактным и унаследованным от {nameof(DependencyInjectorBase)}.",
                nameof(infrastructureType));
        }

        var infrastructure =
            (ActivatorUtilities.CreateInstance(provider, infrastructureType) as DependencyInjectorBase)!;
        infrastructure.Inject(serviceCollection);
    }
}
