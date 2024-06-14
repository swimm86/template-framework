// ----------------------------------------------------------------------------------------------
// <copyright file="WebApplicationBuilderExtensions.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using DotNetEnv.Configuration;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Application.Core.Exceptions;
using Shared.Application.Core.Json;
using Shared.Common;
using Shared.Infrastructure.Core;
using Shared.Presentation.Core.Conventions;
using Shared.Presentation.Core.Swagger;

namespace Shared.Presentation.Core.Extensions;

/// <summary>
/// Класс для внедрения зависимостей. Внедряет динамически все подключенные к проекту зависимости.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    private const string Env = ".env";

    /// <summary>
    /// Метод для конфигурования <see cref="WebApplicationBuilder"/>.
    /// </summary>
    /// <param name="builder"><see cref="WebApplicationBuilder"/>.</param>
    /// <returns><see cref="WebApplicationBuilder"/>.</returns>
    public static WebApplicationBuilder AddSharedPresentationCore(this WebApplicationBuilder builder)
    {
        builder.Configuration.AddEnvironmentVariables();
        builder.Configuration.LoadEnv(builder.Environment);

        var allowedOrigins = builder.Configuration.GetValue<string>("AllowedOrigins");
        builder.Services
            .AddControllers(options =>
            {
                options.Conventions.Add(new ControllerTypeConvention());
                options.Conventions.Add(new ControllerNameConvention());
            }).Services
            .AddEndpointsApiExplorer()
            .AddSwagger()
            .ImplementReferencedInfrastructures()
            .AddFluentValidation()
            .AddCors(options =>
            {
                options.AddPolicy(
                    name: Const.CorsDefaultPolicyName,
                    policy =>
                    {
                        policy.WithOrigins(allowedOrigins ?? "*");
                        policy.AllowAnyHeader();
                        policy.AllowAnyMethod();
                    });
            });

        return builder;
    }

    private static void LoadEnv(
        this ConfigurationManager configurationBuilder,
        IHostEnvironment hostEnvironment)
    {
        configurationBuilder.AddEnvironmentVariables();

        var appPath = AppDomain.CurrentDomain.BaseDirectory;
        var envPath = Path.Combine(appPath, Env);
        if (File.Exists(envPath))
        {
            configurationBuilder.AddDotNetEnv(envPath);
        }

        var currentEnv =
            Path.Combine(appPath, $"{Env}.{hostEnvironment.EnvironmentName.ToLower()}");
        if (File.Exists(currentEnv))
        {
            configurationBuilder.AddDotNetEnv(currentEnv);
        }
    }
}
