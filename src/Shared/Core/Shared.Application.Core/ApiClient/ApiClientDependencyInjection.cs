// ----------------------------------------------------------------------------------------------
// <copyright file="ApiClientDependencyInjection.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.ApiClient.Settings.Base;
using Shared.Application.Core.DependencyInjection.Attributes;
using Shared.Common.Extensions;
using Shared.Common.Helpers;

namespace Shared.Application.Core.ApiClient;

/// <summary>
/// Методы расширения для <see cref="IServiceCollection"/>.
/// </summary>
public static class ApiClientDependencyInjection
{
    /// <summary>
    /// Метод расширения для регистрации всех производных HttpClient в IServiceCollection.
    /// </summary>
    /// <param name="serviceCollection">Коллекция сервисов для регистрации.</param>
    /// <param name="configuration"><see cref="IConfiguration"/>.</param>
    /// <param name="builderAction"><see cref="Action"/> для конфигурации <see cref="IHttpClientBuilder"/></param>
    /// <returns>Измененная коллекция сервисов с добавленными контекстами данных.</returns>
    public static IServiceCollection AddHttpClients(
        this IServiceCollection serviceCollection,
        IConfiguration configuration,
        Action<IHttpClientBuilder>? builderAction = null)
    {
        return serviceCollection.AddHttpClients(
            configuration,
            builderAction,
            delegateHandlerType: null);
    }

    /// <summary>
    /// Метод расширения для регистрации всех производных HttpClient в IServiceCollection.
    /// </summary>
    /// <typeparam name="TDelegatingHandler">Тип обработчика HTTP-запросов.</typeparam>
    /// <param name="serviceCollection">Коллекция сервисов для регистрации.</param>
    /// <param name="configuration"><see cref="IConfiguration"/>.</param>
    /// <param name="builderAction"><see cref="Action"/> для конфигурации <see cref="IHttpClientBuilder"/></param>
    /// <returns>Измененная коллекция сервисов с добавленными контекстами данных.</returns>
    public static IServiceCollection AddHttpClients<TDelegatingHandler>(
        this IServiceCollection serviceCollection,
        IConfiguration configuration,
        Action<IHttpClientBuilder>? builderAction = null)
        where TDelegatingHandler : DelegatingHandler
    {
        return serviceCollection.AddHttpClients(
            configuration,
            builderAction,
            typeof(TDelegatingHandler));
    }

    private static IServiceCollection AddHttpClients(
        this IServiceCollection serviceCollection,
        IConfiguration configuration,
        Action<IHttpClientBuilder>? builderAction,
        Type? delegateHandlerType)
    {
        AssemblyHelper.GetDerivedTypesFromAssemblies<ApiClient>(
                excludedAttributesTypes: [typeof(ManualConfigurationAttribute)])
            .ForEach(type =>
            {
                var settings = AssemblyHelper
                    .GetDerivedTypesFromAssemblies(
                        typeof(ApiClientSettingsBase<>).MakeGenericType(type),
                        excludedAttributesTypes: [typeof(ManualConfigurationAttribute)])
                    .FirstOrDefault();
                if (settings is null)
                {
                    throw new InvalidOperationException($"Не удалось найти настройки для типа {type.FullName}");
                }

                var genericArgumentsCount = delegateHandlerType is null ? 3 : 4;
                var method =
                    typeof(ApiClientExtensions)
                        .GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .SingleOrDefault(m =>
                            m.Name == nameof(ApiClientExtensions.AddClient) &&
                            m.GetParameters().Length == 3 &&
                            m.GetGenericArguments().Length == genericArgumentsCount)
                    ?? throw new InvalidOperationException(
                        $"Не найден метод AddClient с {genericArgumentsCount} generic-параметрами");
                var @interface = type
                    .GetInterfaces()
                    .FirstOrDefault(@interface => @interface.Name == $"I{type.Name}");
                if (@interface is null)
                {
                    throw new InvalidOperationException($"Не удалось найти интефейс для типа {type.FullName}");
                }

                Type[] genericTypes = delegateHandlerType is null
                    ? [settings, @interface, type]
                    : [settings, delegateHandlerType, @interface, type];
                method
                    .MakeGenericMethod(genericTypes)
                    .Invoke(null, [serviceCollection, configuration, builderAction]);
            });

        return serviceCollection;
    }
}
