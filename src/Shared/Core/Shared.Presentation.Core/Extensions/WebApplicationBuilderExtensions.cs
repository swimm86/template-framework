// ----------------------------------------------------------------------------------------------
// <copyright file="WebApplicationBuilderExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.Configuration.Extensions;
using Shared.Infrastructure.Core.DependencyInjection.Extensions;
using Shared.Presentation.Core.Conventions;
using Shared.Presentation.Core.Exceptions.Extensions;
using Shared.Presentation.Core.RequestLogging.Filters;
using Shared.Presentation.Core.Swagger.Extensions;

namespace Shared.Presentation.Core.Extensions;

/// <summary>
/// Методы расширения для настройки <see cref="WebApplicationBuilder"/> с использованием компонентов Shared.Presentation.Core.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Подключает контроллеры и соглашения маршрутов, Swagger, CORS, журналирование запросов,
    /// обработку исключений, FluentValidation и регистрацию зависимостей из ссылочных сборок
    /// (<see cref="Infrastructure.Core.DependencyInjection.Extensions.ServiceCollectionExtensions"/>).
    /// </summary>
    /// <param name="builder">Построитель веб-приложения ASP.NET Core.</param>
    /// <returns>Текущий <paramref name="builder"/> для цепочечных вызовов.</returns>
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
            .AddReferencedDependencyInjectors()
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
