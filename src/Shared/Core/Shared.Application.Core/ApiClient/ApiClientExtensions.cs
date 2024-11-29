// ----------------------------------------------------------------------------------------------
// <copyright file="ApiClientExtensions.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.ApiClient.Settings.Base;
using Shared.Application.Core.Configuration.Extensions;

namespace Shared.Application.Core.ApiClient;

/// <summary>
/// Расширения для api-клиента.
/// </summary>
public static class ApiClientExtensions
{
    /// <summary>
    /// Добавление Api-клиента.
    /// </summary>
    /// <typeparam name="TOptions"> Тип настроек. </typeparam>
    /// <typeparam name="TDelegatingHandler"> Делегат обработчика. </typeparam>
    /// <typeparam name="TIClient"> Тип интерфейса api-клиента. </typeparam>
    /// <typeparam name="TClient"> Тип api-клиента. </typeparam>
    /// <param name="serviceCollection"> Сервисы <see cref="IServiceCollection"/>. </param>
    /// <param name="configuration"> Конфигурация. </param>
    /// <returns> Сервисы <see cref="IServiceCollection"/>. </returns>
    public static IServiceCollection AddClient<TOptions, TDelegatingHandler, TIClient, TClient>(
        this IServiceCollection serviceCollection, IConfiguration configuration)
        where TOptions : ApiClientSettingsBase<TClient>
        where TDelegatingHandler : DelegatingHandler
        where TIClient : class
        where TClient : ApiClient, TIClient
    {
        var options = configuration.GetOptions<TOptions>();
        serviceCollection.AddClient<TOptions, TDelegatingHandler, TIClient, TClient>(options!);

        return serviceCollection;
    }

    /// <summary>
    /// Добавление Api-клиента.
    /// </summary>
    /// <typeparam name="TOptions"> Тип настроек. </typeparam>
    /// <typeparam name="TIClient"> Тип интерфейса api-клиента. </typeparam>
    /// <typeparam name="TClient"> Тип api-клиента. </typeparam>
    /// <param name="serviceCollection"> Сервисы <see cref="IServiceCollection"/>. </param>
    /// <param name="configuration"> Конфигурация. </param>
    /// <returns> Сервисы <see cref="IServiceCollection"/>. </returns>
    public static IServiceCollection AddClient<TOptions, TIClient, TClient>(
        this IServiceCollection serviceCollection, IConfiguration configuration)
        where TOptions : ApiClientSettingsBase<TClient>
        where TIClient : class
        where TClient : ApiClient, TIClient
    {
        var options = configuration.GetOptions<TOptions>();
        serviceCollection.AddClient<TOptions, TIClient, TClient>(options!);

        return serviceCollection;
    }

    /// <summary>
    /// Добавление Api-клиента.
    /// </summary>
    /// <typeparam name="TOptions"> Тип настроек. </typeparam>
    /// <typeparam name="TIClient"> Тип интерфейса api-клиента. </typeparam>
    /// <typeparam name="TClient"> Тип api-клиента. </typeparam>
    /// <param name="serviceCollection"> Сервисы <see cref="IServiceCollection"/>. </param>
    /// <param name="configuration"> Конфигурация. </param>
    /// <param name="builderAction">Операция настроек <see cref="IHttpClientBuilder"/>.</param>
    /// <returns> Сервисы <see cref="IServiceCollection"/>. </returns>
    public static IServiceCollection AddClient<TOptions, TIClient, TClient>(
        this IServiceCollection serviceCollection,
        IConfiguration configuration,
        Action<IHttpClientBuilder>? builderAction = default)
        where TOptions : ApiClientSettingsBase<TClient>
        where TIClient : class
        where TClient : ApiClient, TIClient
    {
        var options = configuration.GetOptions<TOptions>();

        options.Validate<TOptions, TClient>();

        var builder = serviceCollection
            .AddHttpClient(typeof(TClient).Name, client =>
            {
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = options.Timeout;
            })
            .ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            });

        builderAction?.Invoke(builder);

        return serviceCollection
            .AddTransient<TIClient, TClient>();
    }

    /// <summary>
    /// Добавление Api-клиента.
    /// </summary>
    /// <typeparam name="TOptions"> Тип настроек. </typeparam>
    /// <typeparam name="TDelegatingHandler"> Делегат обработчика. </typeparam>
    /// <typeparam name="TIClient"> Тип интерфейса api-клиента. </typeparam>
    /// <typeparam name="TClient"> Тип api-клиента. </typeparam>
    /// <param name="serviceCollection"> Сервисы <see cref="IServiceCollection"/>. </param>
    /// <param name="options"> Настройки. </param>
    /// <returns> Сервисы <see cref="IServiceCollection"/>. </returns>
    public static IServiceCollection AddClient<TOptions, TDelegatingHandler, TIClient, TClient>(
        this IServiceCollection serviceCollection, TOptions options)
        where TOptions : ApiClientSettingsBase<TClient>
        where TDelegatingHandler : DelegatingHandler
        where TIClient : class
        where TClient : ApiClient, TIClient
    {
        options.Validate<TOptions, TClient>();
        serviceCollection
            .AddClient<TOptions, TIClient, TClient>(options, x => x.AddHttpMessageHandler<TDelegatingHandler>());
        return serviceCollection;
    }

    /// <summary>
    /// Добавление Api-клиента.
    /// </summary>
    /// <typeparam name="TOptions"> Тип настроек. </typeparam>
    /// <typeparam name="TIClient"> Тип интерфейса api-клиента. </typeparam>
    /// <typeparam name="TClient"> Тип api-клиента. </typeparam>
    /// <param name="serviceCollection"> Сервисы <see cref="IServiceCollection"/>. </param>
    /// <param name="options"> Настройки. </param>
    /// <param name="builderAction"><see cref="Action"/> для конфигурации <see cref="IHttpClientBuilder"/></param>
    /// <returns> Сервисы <see cref="IServiceCollection"/>. </returns>
    public static IServiceCollection AddClient<TOptions, TIClient, TClient>(
        this IServiceCollection serviceCollection,
        TOptions options,
        Action<IHttpClientBuilder>? builderAction = default)
        where TOptions : ApiClientSettingsBase<TClient>
        where TIClient : class
        where TClient : ApiClient, TIClient
    {
        options.Validate<TOptions, TClient>();

        var builder = serviceCollection
            .AddHttpClient(typeof(TClient).Name, client =>
            {
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = options.Timeout;
            })
            .ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; }
            });

        builderAction?.Invoke(builder);

        return serviceCollection
            .AddTransient<TIClient, TClient>();
    }
}
