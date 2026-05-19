// ----------------------------------------------------------------------------------------------
// <copyright file="DependencyInjector.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.ApiClient.Configurators.BuilderConfigurator;
using Shared.Application.Core.DependencyInjection.Base;
using Shared.Infrastructure.Core.ApiClient.Extensions;

namespace Shared.Infrastructure.Core.DependencyInjection;

/// <summary>
/// Регистрация DI-зависимостей слоя <c>Shared.Infrastructure.Core</c>.
/// </summary>
/// <remarks>
/// Инициализирует конфигураторы ApiClient и регистрирует инфраструктурные сервисы
/// для выполнения HTTP-запросов: делегирующие обработчики и типизированные HTTP-клиенты.
/// <para><inheritdoc cref="DependencyInjectorBase" path="/remarks"/></para>
/// </remarks>
/// <param name="configuration">Конфигурация приложения (<see cref="IConfiguration"/>).</param>
/// <param name="loggerFactory"><inheritdoc cref="DependencyInjectorBase(ILoggerFactory)" path="/param[@name='loggerFactory']"/></param>
public class DependencyInjector(
    IConfiguration configuration,
    ILoggerFactory loggerFactory)
    : DependencyInjectorBase(loggerFactory)
{
    /// <inheritdoc />
    protected override IServiceCollection Process(
        IServiceCollection serviceCollection)
    {
        ApiClientBuilderConfiguratorContext.InitializeApiClientBuilderConfiguratorsMap();
        return serviceCollection
            .AddDelegatingHandlers()
            .AddPrimaryHttpMessageHandlers()
            .AddHttpClients(configuration);
    }
}
