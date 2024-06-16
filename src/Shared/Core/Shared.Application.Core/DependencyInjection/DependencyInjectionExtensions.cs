// ----------------------------------------------------------------------------------------------
// <copyright file="DependencyInjectionExtensions.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.Attributes;
using Shared.Common.Extensions;
using Shared.Common.Helpers;

namespace Shared.Application.Core.DependencyInjection;

/// <summary>
/// Класс методов расширения для внедрения зависимостей в коллекцию служб <see cref="IServiceCollection"/>.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Регистрирует зависимости в коллекции служб в качестве Transient-сервисов для всех типов,
    /// производных от заданного сервисного типа.
    /// </summary>
    /// <param name="serviceCollection">Коллекция служб для конфигурации зависимостей.</param>
    /// <param name="baseType">Тип сервиса, для которого необходимо найти и зарегистрировать производные типы.</param>
    /// <returns>Коллекция служб с зарегистрированными зависимостями.</returns>
    /// <remarks>
    /// Метод проходит по всем сборкам текущего домена приложения, находит все классы, которые наследуются
    /// от указанного baseType, и регистрирует каждый найденный класс в коллекции служб как Transient-зависимость.
    /// Предполагается, что каждый производный класс реализует интерфейс, который является обобщённым типом baseType.
    /// Если класс, который реализует интерфейс, содержит атрибут ManualConfiguration, то этот тип не регистрируется.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Выбрасывается, если у производного типа не найден интерфейс,
    /// соответствующий заданному baseType.</exception>
    public static IServiceCollection RegisterDerivedTypeDependenciesTransient(
        this IServiceCollection serviceCollection,
        Type baseType) =>
        serviceCollection.RegisterDerivedTypeDependencies(
            baseType,
            (serviceType, implementationType) => serviceCollection.AddTransient(serviceType, implementationType));

    /// <summary>
    /// Регистрирует зависимости в коллекции служб в качестве Scoped-сервисов для всех типов,
    /// производных от заданного сервисного типа.
    /// </summary>
    /// <param name="serviceCollection">Коллекция служб для конфигурации зависимостей.</param>
    /// <param name="baseType">Тип сервиса, для которого необходимо найти и зарегистрировать производные типы.</param>
    /// <returns>Коллекция служб с зарегистрированными зависимостями.</returns>
    /// <remarks>
    /// Метод проходит по всем сборкам текущего домена приложения, находит все классы, которые наследуются
    /// от указанного baseType, и регистрирует каждый найденный класс в коллекции служб как Transient-зависимость.
    /// Предполагается, что каждый производный класс реализует интерфейс, который является обобщённым типом baseType.
    /// Если класс, который реализует интерфейс, содержит атрибут ManualConfiguration, то этот тип не регистрируется.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Выбрасывается, если у производного типа не найден интерфейс,
    /// соответствующий заданному baseType.</exception>
    public static IServiceCollection RegisterDerivedTypeDependenciesScoped(
        this IServiceCollection serviceCollection,
        Type baseType) =>
        serviceCollection.RegisterDerivedTypeDependencies(
            baseType,
            (serviceType, implementationType) => serviceCollection.AddScoped(serviceType, implementationType));

    /// <summary>
    /// Регистрирует зависимости в коллекции служб в качестве Singleton-сервисов для всех типов,
    /// производных от заданного сервисного типа.
    /// </summary>
    /// <param name="serviceCollection">Коллекция служб для конфигурации зависимостей.</param>
    /// <param name="baseType">Тип сервиса, для которого необходимо найти и зарегистрировать производные типы.</param>
    /// <returns>Коллекция служб с зарегистрированными зависимостями.</returns>
    /// <remarks>
    /// Метод проходит по всем сборкам текущего домена приложения, находит все классы, которые наследуются
    /// от указанного baseType, и регистрирует каждый найденный класс в коллекции служб как Transient-зависимость.
    /// Предполагается, что каждый производный класс реализует интерфейс, который является обобщённым типом baseType.
    /// Если класс, который реализует интерфейс, содержит атрибут ManualConfiguration, то этот тип не регистрируется.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Выбрасывается, если у производного типа не найден интерфейс,
    /// соответствующий заданному baseType.</exception>
    public static IServiceCollection RegisterDerivedTypeDependenciesSingleton(
        this IServiceCollection serviceCollection,
        Type baseType) =>
        serviceCollection.RegisterDerivedTypeDependencies(
            baseType,
            (serviceType, implementationType) => serviceCollection.AddSingleton(serviceType, implementationType));

    private static IServiceCollection RegisterDerivedTypeDependencies(
        this IServiceCollection serviceCollection,
        Type baseType,
        Action<Type, Type> registerAction)
    {
        AssemblyHelper
            .GetDerivedTypesFromAssemblies(baseType, excludedAttributesTypes: [typeof(ManualConfigurationAttribute)])
            .ForEach(type =>
            {
                if (type.IsGenericTypeDefinition)
                {
                    return;
                }

                var serviceType = type.GetInterfaces().First(t => t.GetGenericTypeDefinition() == baseType);
                registerAction(serviceType, type);
            });
        return serviceCollection;
    }
}
