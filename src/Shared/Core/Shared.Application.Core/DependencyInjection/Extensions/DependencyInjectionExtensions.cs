// ----------------------------------------------------------------------------------------------
// <copyright file="DependencyInjectionExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.DependencyInjection.Attributes;
using Shared.Common.Extensions;
using Shared.Common.Helpers;

namespace Shared.Application.Core.DependencyInjection.Extensions;

/// <summary>
/// Класс методов расширения для внедрения зависимостей в коллекцию служб <see cref="IServiceCollection"/>.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Регистрирует зависимости в коллекции служб в качестве сервисов для всех типов,
    /// производных от заданного сервисного типа.
    /// </summary>
    /// <typeparam name="TBaseType">Тип сервиса, для которого необходимо найти и зарегистрировать производные типы.</typeparam>
    /// <param name="serviceCollection">Коллекция служб для конфигурации зависимостей.</param>
    /// <param name="lifetime"><see cref="ServiceLifetime"/>.</param>
    /// <returns>Коллекция служб с зарегистрированными зависимостями.</returns>
    /// <remarks>
    /// Метод проходит по всем сборкам текущего домена приложения, находит все классы, которые наследуются
    /// от указанного baseType, и регистрирует каждый найденный класс в коллекции служб как Transient-зависимость.
    /// Предполагается, что каждый производный класс реализует интерфейс, который является обобщённым типом baseType.
    /// Если класс, который реализует интерфейс, содержит атрибут ManualConfiguration, то этот тип не регистрируется.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Выбрасывается, если у производного типа не найден интерфейс,
    /// соответствующий заданному baseType.</exception>
    public static IServiceCollection RegisterDerivedTypeDependencies<TBaseType>(
        this IServiceCollection serviceCollection,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        var baseType = typeof(TBaseType);
        return serviceCollection.RegisterDerivedTypeDependencies(baseType, lifetime);
    }

    /// <summary>
    /// Регистрирует зависимости в коллекции служб в качестве сервисов для всех типов,
    /// производных от заданного сервисного типа.
    /// </summary>
    /// <param name="serviceCollection">Коллекция служб для конфигурации зависимостей.</param>
    /// <param name="baseType">Тип сервиса, для которого необходимо найти и зарегистрировать производные типы.</param>
    /// <param name="lifetime"><see cref="ServiceLifetime"/>.</param>
    /// <returns>Коллекция служб с зарегистрированными зависимостями.</returns>
    /// <remarks>
    /// Метод проходит по всем сборкам текущего домена приложения, находит все классы, которые наследуются
    /// от указанного baseType, и регистрирует каждый найденный класс в коллекции служб как Transient-зависимость.
    /// Предполагается, что каждый производный класс реализует интерфейс, который является обобщённым типом baseType.
    /// Если класс, который реализует интерфейс, содержит атрибут ManualConfiguration, то этот тип не регистрируется.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Выбрасывается, если у производного типа не найден интерфейс,
    /// соответствующий заданному baseType, либо если для не шаблонного базового типа несколько производных типов.</exception>
    public static IServiceCollection RegisterDerivedTypeDependencies(
        this IServiceCollection serviceCollection,
        Type baseType,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        var types = AssemblyHelper
            .GetDerivedTypesFromAssemblies(baseType, excludedAttributesTypes: [typeof(ManualConfigurationAttribute)])
            .ToArray();

        if (!baseType.IsGenericTypeDefinition && types.Length > 1)
        {
            throw new InvalidOperationException(
                $"Для сервиса с типом '{baseType.Name}' должен быть только один производный тип.");
        }

        types.ForEach(type =>
        {
            if (type.IsGenericTypeDefinition)
            {
                return;
            }

            var serviceType = type.GetInterfaces().First(t =>
                baseType.IsGenericTypeDefinition ? t.GetGenericTypeDefinition() == baseType : t == baseType);
            serviceCollection.Add(new ServiceDescriptor(serviceType, type, lifetime));
        });
        return serviceCollection;
    }

    /// <summary>
    /// Замена сервиса.
    /// </summary>
    /// <typeparam name="TService"> Тип сервиса. </typeparam>
    /// <param name="services"> Сервисы. </param>
    /// <param name="implementation"> Реализация нового сервиса. </param>
    /// <param name="lifetime"> Жизненный цикл сервиса. </param>
    /// <returns> Сервисы. </returns>
    public static IServiceCollection Replace<TService>(
        this IServiceCollection services,
        Func<IServiceProvider, TService> implementation,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TService : class
    {
        if (!services.Remove(services.First(d => d.ServiceType == typeof(TService))))
        {
            throw new InvalidOperationException($"Не удалось удалить сервис: {typeof(TService).Name}.");
        }

        services.Add(new ServiceDescriptor(typeof(TService), implementation, lifetime));
        return services;
    }
}
