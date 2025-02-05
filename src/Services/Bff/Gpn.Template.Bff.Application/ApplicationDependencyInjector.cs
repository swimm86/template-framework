// ----------------------------------------------------------------------------------------------
// <copyright file="ApplicationDependencyInjector.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Contour.Admin.Auth.Sdk.Helpers;
using Gpn.Template.Bff.Application.HttpClients;
using Gpn.Template.Bff.Application.HttpClients.Settings;
using Gpn.Template.Bff.Application.Interfaces.HttpClients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.ApiClient;
using Shared.Application.Core.DependencyInjection;

namespace Gpn.Template.Bff.Application;

/// <summary>
/// Класс для внедрения зависимостей Application-слоя в Bff
/// </summary>
/// <param name="configuration"><see cref="IConfiguration"/>.</param>
/// <param name="logger">Логгер.</param>
public class ApplicationDependencyInjector(
    IConfiguration configuration,
    ILogger<ApplicationDependencyInjector> logger
    ) : DependencyInjectorBase(logger)
{
    /// <inheritdoc />
    protected override IServiceCollection Process(IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddClient<GetterApiClientSettings, IGetterClient, GetterClient>(
                configuration,
                builder => builder.AddHttpMessageHandler<AuthHttpClientHandler>())
            .AddClient<SetterApiClientSettings, ISetterClient, SetterClient>(
                configuration,
                builder => builder.AddHttpMessageHandler<AuthHttpClientHandler>());
    }
}
