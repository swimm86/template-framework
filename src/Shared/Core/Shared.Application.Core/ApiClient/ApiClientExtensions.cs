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
    /// Добавляет API-клиент с обработчиком аутентификации.
    /// </summary>
    /// <typeparam name="TOptions">Тип настроек (наследуется от <see cref="ApiClientSettingsBase{TClient}"/>).</typeparam>
    /// <typeparam name="TDelegatingHandler">Тип обработчика (наследуется от <see cref="DelegatingHandler"/>).</typeparam>
    /// <typeparam name="TIClient">Тип интерфейса клиента.</typeparam>
    /// <typeparam name="TClient">Тип реализациии клиента (наследуется от <see cref="ApiClient"/>).</typeparam>
    /// <param name="serviceCollection"> Сервисы <see cref="IServiceCollection"/>. </param>
    /// <param name="configuration"> Конфигурация. </param>
    /// <param name="builderAction"><see cref="Action"/> для конфигурации <see cref="IHttpClientBuilder"/></param>
    /// <returns> Сервисы <see cref="IServiceCollection"/>. </returns>
    public static IServiceCollection AddClient<TOptions, TDelegatingHandler, TIClient, TClient>(
        this IServiceCollection serviceCollection,
        IConfiguration configuration,
        Action<IHttpClientBuilder>? builderAction = null)
        where TOptions : ApiClientSettingsBase<TClient>
        where TDelegatingHandler : DelegatingHandler
        where TIClient : class
        where TClient : ApiClient, TIClient
    {
        return serviceCollection.AddClient<TOptions, TIClient, TClient>(
            configuration,
            x =>
            {
                builderAction?.Invoke(x);
                x.AddHttpMessageHandler<TDelegatingHandler>();
            });
    }

    /// <summary>
    /// Добавляет API-клиент без обработчика аутентификации.
    /// </summary>
    /// <typeparam name="TOptions">Тип настроек (наследуется от <see cref="ApiClientSettingsBase{TClient}"/>).</typeparam>
    /// <typeparam name="TIClient">Тип интерфейса клиента.</typeparam>
    /// <typeparam name="TClient">Тип реализациии клиента (наследуется от <see cref="ApiClient"/>).</typeparam>
    /// <param name="serviceCollection"> Сервисы <see cref="IServiceCollection"/>. </param>
    /// <param name="configuration"> Конфигурация. </param>
    /// <param name="builderAction"><see cref="Action"/> для конфигурации <see cref="IHttpClientBuilder"/></param>
    /// <returns> Сервисы <see cref="IServiceCollection"/>. </returns>
    public static IServiceCollection AddClient<TOptions, TIClient, TClient>(
        this IServiceCollection serviceCollection,
        IConfiguration configuration,
        Action<IHttpClientBuilder>? builderAction = null)
        where TOptions : ApiClientSettingsBase<TClient>
        where TIClient : class
        where TClient : ApiClient, TIClient
    {
        var options = configuration.GetOptions<TOptions>()!;
        options.Validate<TOptions, TClient>();

        var builder = serviceCollection
            .AddHttpClient(typeof(TClient).Name, client =>
            {
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = options.Timeout;
            });

        builderAction?.Invoke(builder);

        return serviceCollection
            .AddTransient<TIClient, TClient>();
    }
}
