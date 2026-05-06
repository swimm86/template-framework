// ----------------------------------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.DependencyInjection.Base;
using Shared.Common.Extensions;
using Shared.Common.Helpers;

namespace Shared.Infrastructure.Core.DependencyInjection.Extensions;

/// <summary>
/// Методы расширения для <see cref="IServiceCollection"/>, обеспечивающие обнаружение и вызов
/// классов <see cref="DependencyInjectorBase"/> из загруженных сборок (с учётом порядка слоёв чистой архитектуры).
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Загружает ссылочные сборки инфраструктуры, находит наследников <see cref="DependencyInjectorBase"/>
    /// и последовательно регистрирует их сервисы в контейнере.
    /// </summary>
    /// <param name="services">Коллекция сервисов приложения.</param>
    /// <returns>Та же коллекция сервисов для цепочечных вызовов.</returns>
    public static IServiceCollection AddReferencedDependencyInjectors(
        this IServiceCollection services)
    {
        LoadReferencedInfrastructureAssemblies();

        using var provider = services.BuildServiceProvider();

        // Сначала регистрируются зависимости Shared, затем конкретного модуля,
        // от нижнего слоя к верхнему.
        var dependencyInjectorTypes =
            AssemblyHelper.GetDerivedTypesFromAssemblies<DependencyInjectorBase>()
                .OrderBy(x => !x.FullName!.StartsWith(nameof(Shared), StringComparison.OrdinalIgnoreCase))
                .ThenBy(x => !x.FullName!.Contains(nameof(Application), StringComparison.OrdinalIgnoreCase))
                .ThenBy(x => !x.FullName!.Contains(nameof(Infrastructure), StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(x => x.FullName!.Split('.').Length)
                .ToArray();
        dependencyInjectorTypes.ForEach(x => services.ApplyDependencyInjector(provider, x));

        return services;
    }

    /// <summary>
    /// Дополнительно загружает из каталога приложения сборки инфраструктуры, ещё не загруженные в домен.
    /// </summary>
    private static void LoadReferencedInfrastructureAssemblies()
    {
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        var loadedPaths = loadedAssemblies.Select(a => a.Location).ToArray();

        var referencedPaths = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll");
        var toLoad = referencedPaths
            .Where(r =>
                !loadedPaths.Contains(r, StringComparer.InvariantCultureIgnoreCase) &&
                r.Contains(nameof(Infrastructure)))
            .ToArray();

        Parallel.ForEach(
            toLoad,
            path => AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(path)));
    }

    /// <summary>
    /// Создаёт экземпляр регистратора указанного типа и вызывает <see cref="DependencyInjectorBase.Inject"/>.
    /// </summary>
    /// <param name="serviceCollection">Коллекция сервисов приложения.</param>
    /// <param name="provider">Провайдер для разрешения конструктора регистратора.</param>
    /// <param name="dependencyInjectorType">Тип неабстрактного класса, наследующего <see cref="DependencyInjectorBase"/>.</param>
    /// <exception cref="ArgumentException">Тип не является подходящим наследником <see cref="DependencyInjectorBase"/>.</exception>
    private static void ApplyDependencyInjector(
        this IServiceCollection serviceCollection,
        IServiceProvider provider,
        Type dependencyInjectorType)
    {
        if (!typeof(DependencyInjectorBase).IsAssignableFrom(dependencyInjectorType) || !dependencyInjectorType.IsClass ||
            dependencyInjectorType.IsAbstract)
        {
            throw new ArgumentException(
                $"Dependency injector type must be non-abstract and assignable from {nameof(DependencyInjectorBase)}.",
                nameof(dependencyInjectorType));
        }

        var dependencyInjector =
            (ActivatorUtilities.CreateInstance(provider, dependencyInjectorType) as DependencyInjectorBase)!;
        dependencyInjector.Inject(serviceCollection);
    }
}
