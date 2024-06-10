// ----------------------------------------------------------------------------------------------
// <copyright file="ApplicationDependencyInjector.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Bff.Application.HttpClients;
using Gpn.Template.Bff.Application.HttpClients.Settings;
using Gpn.Template.Bff.Application.Interfaces.HttpClients;
using Gpn.Template.Bff.Application.Interfaces.Services;
using Gpn.Template.Bff.Application.Services;
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

            // HttpClients
            .AddClient<GetterApiClientSettings, IGetterClient, GetterClient>(configuration)
            .AddClient<SetterApiClientSettings, ISetterClient, SetterClient>(configuration)

            // Services
            .AddTransient<IGetterService, GetterService>()
            .AddTransient<ISetterService, SetterService>();
    }
}
