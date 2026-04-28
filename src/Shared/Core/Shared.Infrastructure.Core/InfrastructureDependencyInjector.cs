// ----------------------------------------------------------------------------------------------
// <copyright file="InfrastructureDependencyInjector.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.ApiClient.Configurators.BuilderConfigurator;
using Shared.Application.Core.DependencyInjection;
using Shared.Infrastructure.Core.ApiClient.Extensions;

namespace Shared.Infrastructure.Core;

/// <summary>
/// Класс для внедрения зависимостей Application.Core-слоя.
/// </summary>
public class InfrastructureDependencyInjector(
    IConfiguration configuration,
    ILogger<InfrastructureDependencyInjector> logger)
    : DependencyInjectorBase(logger)
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
