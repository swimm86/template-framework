// ----------------------------------------------------------------------------------------------
// <copyright file="WebApplicationBuilderExtensions.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.Configuration;
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
    /// <summary>
    /// Метод для конфигурования <see cref="WebApplicationBuilder"/>.
    /// </summary>
    /// <param name="builder"><see cref="WebApplicationBuilder"/>.</param>
    /// <returns><see cref="WebApplicationBuilder"/>.</returns>
    public static WebApplicationBuilder AddSharedPresentationCore(this WebApplicationBuilder builder)
    {
        builder.Configuration.InitializeConfiguration(builder.Environment);
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
}
