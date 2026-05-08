// ----------------------------------------------------------------------------------------------
// <copyright file="DependencyInjector.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.DependencyInjection.Base;
using Template.Presentation.Swagger.Extensions;

namespace Template.Presentation.DependencyInjection;

/// <summary>
/// Регистрация DI-зависимостей слоя: <c>Common.Presentation</c>.
/// </summary>
/// <inheritdoc cref="DependencyInjectorBase" path="/remarks"/>
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
        var allowedOrigins = configuration.GetValue<string>("AllowedOrigins");
        return serviceCollection
            .ConfigureSwaggerAuth()
            .AddCors(options =>
            {
                options.AddPolicy(
                    name: Constants.CorsDefaultPolicyName,
                    policy =>
                    {
                        policy.WithOrigins(allowedOrigins ?? "*");
                        policy.AllowAnyHeader();
                        policy.AllowAnyMethod();
                    });
            });
    }
}
