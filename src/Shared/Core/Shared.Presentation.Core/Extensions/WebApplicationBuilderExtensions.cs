// ----------------------------------------------------------------------------------------------
// <copyright file="WebApplicationBuilderExtensions.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.Configuration.Extensions;
using Shared.Infrastructure.Core;
using Shared.Presentation.Core.Conventions;
using Shared.Presentation.Core.Exceptions.Extensions;
using Shared.Presentation.Core.RequestLogging.Filters;
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
                options.Filters.Add<RequestLoggingFilter>();
            }).Services
            .AddEndpointsApiExplorer()
            .AddSwagger()
            .ImplementReferencedInfrastructures()
            .AddFluentValidation()
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
            })
            .AddExceptionHandling();

        return builder;
    }
}
