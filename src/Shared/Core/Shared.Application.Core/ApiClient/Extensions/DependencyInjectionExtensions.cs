// ----------------------------------------------------------------------------------------------
// <copyright file="DependencyInjectionExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.ApiClient.Configurators.BuilderConfigurator;
using Shared.Application.Core.ApiClient.Handlers.Attributes;
using Shared.Application.Core.ApiClient.Handlers.Base;
using Shared.Application.Core.ApiClient.Settings.Base;
using Shared.Application.Core.Configuration.Extensions;
using Shared.Common.Extensions;
using Shared.Common.Helpers;

namespace Shared.Application.Core.ApiClient.Extensions;

/// <summary>
/// Методы расширения для регистрации API-клиентов в <see cref="IServiceCollection"/>.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Регистрирует API-клиент вручную через <see cref="IConfiguration"/>.
    /// </summary>
    /// <typeparam name="TOptions">Тип настроек API-клиента.</typeparam>
    /// <typeparam name="TIClient">Тип интерфейса API-клиента.</typeparam>
    /// <typeparam name="TClient">Тип реализации API-клиента.</typeparam>
    /// <param name="serviceCollection">Коллекция сервисов <see cref="IServiceCollection"/>.</param>
    /// <param name="configuration">Конфигурация приложения <see cref="IConfiguration"/>.</param>
    /// <returns>Текущая коллекция сервисов <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddClient<TOptions, TIClient, TClient>(
        this IServiceCollection serviceCollection,
        IConfiguration configuration)
        where TOptions : ApiClientSettingsBase
        where TIClient : class
        where TClient : ApiClient, TIClient
    {
        var options = configuration.GetOptions<TOptions>();
        return serviceCollection.AddClient<TIClient, TClient>(options!);
    }

    /// <summary>
    /// Регистрирует API-клиент вручную через объект настроек.
    /// </summary>
    /// <typeparam name="TIClient">Тип интерфейса API-клиента.</typeparam>
    /// <typeparam name="TClient">Тип реализации API-клиента.</typeparam>
    /// <param name="serviceCollection">Коллекция сервисов <see cref="IServiceCollection"/>.</param>
    /// <param name="options">Настройки API-клиента.</param>
    /// <returns>Текущая коллекция сервисов <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddClient<TIClient, TClient>(
        this IServiceCollection serviceCollection,
        ApiClientSettingsBase options)
        where TIClient : class
        where TClient : ApiClient, TIClient
    {
        options.Validate();
        var clientType = typeof(TClient);
        var clientInterfaceType = typeof(TIClient);
        var builder = serviceCollection
            .AddHttpClient(clientType.FullName!, client =>
            {
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = options.Timeout;
            })
            .RegisterHandlers(clientType)
            .SetHandlerLifetime(TimeSpan.FromMinutes(2));

        var builderConfigurator = ApiClientBuilderConfiguratorContext.GetBuilderConfigurator(clientType);
        builderConfigurator?.Configure(builder);

        return serviceCollection
            .AddTransient(clientInterfaceType, clientType);
    }

    private static IHttpClientBuilder RegisterHandlers(
        this IHttpClientBuilder builder,
        Type clientType)
    {
        return builder
            .ConfigurePrimaryHttpMessageHandler(sp => ResolvePrimaryHttpMessageHandler(sp, clientType))
            .RegisterDelegatingHandlers(clientType);
    }

    private static HttpMessageHandler ResolvePrimaryHttpMessageHandler(
        IServiceProvider serviceProvider,
        Type clientType)
    {
        var primaryHandlerType = GetOrderedHandlerTypes<PrimaryHttpMessageHandlerBase>(clientType)
            .SingleOrDefault();
        return primaryHandlerType is null
            ? new HttpClientHandler()
            : (HttpMessageHandler)serviceProvider.GetRequiredService(primaryHandlerType);
    }

    private static IHttpClientBuilder RegisterDelegatingHandlers(
        this IHttpClientBuilder builder,
        Type clientType)
    {
        GetOrderedHandlerTypes<DelegatingHandlerBase>(clientType)
            .ForEach(type => builder.AddHttpMessageHandler(
                serviceProvider => (DelegatingHandler)serviceProvider.GetRequiredService(type)));
        return builder;
    }

    private static IEnumerable<Type> GetOrderedHandlerTypes<THandler>(
        Type clientType)
    {
        var handlerTypes = AssemblyHelper.GetDerivedTypesFromAssemblies<THandler>(
            includedAttributesTypes: [typeof(ApiClientHandlerMetadataAttribute)]);
        var result = handlerTypes
            .Select(type => new
            {
                Type = type,
                Metadata = type.GetCustomAttribute<ApiClientHandlerMetadataAttribute>()!,
            })
            .Where(x => x.Metadata.AppliesTo(clientType))
            .OrderBy(x => x.Metadata.Order)
            .Select(x => x.Type)
            .ToArray();
        return result;
    }
}
