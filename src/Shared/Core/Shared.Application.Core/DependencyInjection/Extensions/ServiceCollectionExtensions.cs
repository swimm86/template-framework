// ----------------------------------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.DependencyInjection.Attributes;
using Shared.Common.Helpers;

namespace Shared.Application.Core.DependencyInjection.Extensions;

/// <summary>
/// Методы расширения для регистрации зависимостей в <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует один производный тип, найденный по <typeparamref name="TBaseType"/>.
    /// </summary>
    /// <typeparam name="TBaseType">Тип сервиса, для которого необходимо найти и зарегистрировать производные типы.</typeparam>
    /// <param name="serviceCollection">Коллекция сервисов <see cref="IServiceCollection"/>.</param>
    /// <param name="serviceTypeAsInterface">
    /// Если <see langword="true"/>, регистрация выполняется как <c>интерфейс -> реализация</c>;
    /// иначе как <c>реализация -> реализация</c>.
    /// </param>
    /// <param name="lifetime"><see cref="ServiceLifetime"/>.</param>
    /// <returns>Текущая коллекция сервисов <see cref="IServiceCollection"/>.</returns>
    /// <remarks>
    /// Метод проходит по сборкам текущего домена приложения, находит типы, производные от базового типа,
    /// и регистрирует их с указанным временем жизни. Типы с атрибутом
    /// <see cref="ManualConfigurationAttribute"/> исключаются.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если найдено более одного производного типа для не-обобщённого базового типа
    /// либо если не найден соответствующий интерфейс при <paramref name="serviceTypeAsInterface"/> = <see langword="true"/>.
    /// </exception>
    public static IServiceCollection RegisterDerivedTypeDependency<TBaseType>(
        this IServiceCollection serviceCollection,
        bool serviceTypeAsInterface,
        ServiceLifetime lifetime)
    {
        var baseType = typeof(TBaseType);
        return serviceCollection.RegisterDerivedTypeDependenciesInternal(baseType, serviceTypeAsInterface, lifetime, true);
    }

    /// <summary>
    /// Регистрирует все производные типы, найденные по <typeparamref name="TBaseType"/>.
    /// </summary>
    /// <typeparam name="TBaseType">Тип сервиса, для которого необходимо найти и зарегистрировать производные типы.</typeparam>
    /// <param name="serviceCollection">Коллекция сервисов <see cref="IServiceCollection"/>.</param>
    /// <param name="serviceTypeAsInterface">
    /// Если <see langword="true"/>, регистрация выполняется как <c>интерфейс -> реализация</c>;
    /// иначе как <c>реализация -> реализация</c>.
    /// </param>
    /// <param name="lifetime"><see cref="ServiceLifetime"/>.</param>
    /// <returns>Текущая коллекция сервисов <see cref="IServiceCollection"/>.</returns>
    /// <remarks>
    /// Метод проходит по сборкам текущего домена приложения, находит типы, производные от базового типа,
    /// и регистрирует их с указанным временем жизни. Типы с атрибутом
    /// <see cref="ManualConfigurationAttribute"/> исключаются.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если не найден соответствующий интерфейс при
    /// <paramref name="serviceTypeAsInterface"/> = <see langword="true"/>.
    /// </exception>
    public static IServiceCollection RegisterDerivedTypeDependencies<TBaseType>(
        this IServiceCollection serviceCollection,
        bool serviceTypeAsInterface,
        ServiceLifetime lifetime)
    {
        var baseType = typeof(TBaseType);
        return serviceCollection.RegisterDerivedTypeDependenciesInternal(baseType, serviceTypeAsInterface, lifetime, false);
    }

    /// <summary>
    /// Регистрирует один производный тип, найденный по <paramref name="baseType"/>.
    /// </summary>
    /// <param name="serviceCollection">Коллекция сервисов <see cref="IServiceCollection"/>.</param>
    /// <param name="baseType">Тип сервиса, для которого необходимо найти и зарегистрировать производные типы.</param>
    /// <param name="serviceTypeAsInterface">
    /// Если <see langword="true"/>, регистрация выполняется как <c>интерфейс -> реализация</c>;
    /// иначе как <c>реализация -> реализация</c>.
    /// </param>
    /// <param name="lifetime"><see cref="ServiceLifetime"/>.</param>
    /// <returns>Текущая коллекция сервисов <see cref="IServiceCollection"/>.</returns>
    /// <remarks>
    /// Метод проходит по сборкам текущего домена приложения, находит типы, производные от базового типа,
    /// и регистрирует их с указанным временем жизни. Типы с атрибутом
    /// <see cref="ManualConfigurationAttribute"/> исключаются.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если найдено более одного производного типа для не-обобщённого базового типа
    /// либо если не найден соответствующий интерфейс при <paramref name="serviceTypeAsInterface"/> = <see langword="true"/>.
    /// </exception>
    public static IServiceCollection RegisterDerivedTypeDependency(
        this IServiceCollection serviceCollection,
        Type baseType,
        bool serviceTypeAsInterface,
        ServiceLifetime lifetime)
    {
        return serviceCollection.RegisterDerivedTypeDependenciesInternal(baseType, serviceTypeAsInterface, lifetime, true);
    }

    /// <summary>
    /// Регистрирует все производные типы, найденные по <paramref name="baseType"/>.
    /// </summary>
    /// <param name="serviceCollection">Коллекция сервисов <see cref="IServiceCollection"/>.</param>
    /// <param name="baseType">Тип сервиса, для которого необходимо найти и зарегистрировать производные типы.</param>
    /// <param name="serviceTypeAsInterface">
    /// Если <see langword="true"/>, регистрация выполняется как <c>интерфейс -> реализация</c>;
    /// иначе как <c>реализация -> реализация</c>.
    /// </param>
    /// <param name="lifetime"><see cref="ServiceLifetime"/>.</param>
    /// <returns>Текущая коллекция сервисов <see cref="IServiceCollection"/>.</returns>
    /// <remarks>
    /// Метод проходит по сборкам текущего домена приложения, находит типы, производные от базового типа,
    /// и регистрирует их с указанным временем жизни. Типы с атрибутом
    /// <see cref="ManualConfigurationAttribute"/> исключаются.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если не найден соответствующий интерфейс при
    /// <paramref name="serviceTypeAsInterface"/> = <see langword="true"/>.
    /// </exception>
    public static IServiceCollection RegisterDerivedTypeDependencies(
        this IServiceCollection serviceCollection,
        Type baseType,
        bool serviceTypeAsInterface,
        ServiceLifetime lifetime)
    {
        return serviceCollection.RegisterDerivedTypeDependenciesInternal(baseType, serviceTypeAsInterface, lifetime, false);
    }

    /// <summary>
    /// Заменяет зарегистрированную реализацию сервиса.
    /// </summary>
    /// <typeparam name="TService">Тип сервиса.</typeparam>
    /// <param name="services">Коллекция сервисов <see cref="IServiceCollection"/>.</param>
    /// <param name="implementation">Фабрика новой реализации сервиса.</param>
    /// <param name="lifetime">Жизненный цикл сервиса.</param>
    /// <returns>Текущая коллекция сервисов <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection Replace<TService>(
        this IServiceCollection services,
        Func<IServiceProvider, TService> implementation,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TService : class
    {
        if (!services.Remove(services.First(d => d.ServiceType == typeof(TService))))
        {
            throw new InvalidOperationException($"Could not remove service registration: {typeof(TService).Name}.");
        }

        services.Add(new ServiceDescriptor(typeof(TService), implementation, lifetime));
        return services;
    }

    private static IServiceCollection RegisterDerivedTypeDependenciesInternal(
        this IServiceCollection serviceCollection,
        Type baseType,
        bool serviceTypeAsInterface,
        ServiceLifetime lifetime,
        bool requireSingleImplementation)
    {
        var candidateTypes = AssemblyHelper
            .GetDerivedTypesFromAssemblies(baseType, excludedAttributesTypes: [typeof(ManualConfigurationAttribute)])
            .Where(type => !type.IsGenericTypeDefinition)
            .ToArray();

        if (requireSingleImplementation &&
            !baseType.IsGenericTypeDefinition &&
            candidateTypes.Length > 1)
        {
            throw new InvalidOperationException(
                $"Only one derived type is allowed for service type '{baseType.Name}'.");
        }

        foreach (var type in candidateTypes)
        {
            var serviceType = serviceTypeAsInterface
                ? type
                    .GetInterfaces()
                    .First(t => baseType.IsGenericTypeDefinition
                        ? t.GetGenericTypeDefinition() == baseType
                        : t == baseType)
                : type;
            serviceCollection.Add(new ServiceDescriptor(serviceType, type, lifetime));
        }

        return serviceCollection;
    }
}
