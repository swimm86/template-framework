// ----------------------------------------------------------------------------------------------
// <copyright file="ApplicationBuilderExtensions.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Shared.Common;
using Shared.Presentation.Core.Swagger;

namespace Shared.Presentation.Core.Extensions;

/// <summary>
/// Класс, который содержит расширения для <see cref="IApplicationBuilder"/>
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Конфигурирует <see cref="IApplicationBuilder"/>.
    /// </summary>
    /// <param name="app"><see cref="WebApplication"/>.</param>
    /// <returns><see cref="IApplicationBuilder"/>.</returns>
    public static IApplicationBuilder ConfigurePresentationCore(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwaggerConfigured();
        }

        app.UseExceptionHandler();

        // Configure the HTTP request pipeline.
        app.UseAuthorization();
        app.MapControllers();

        app.UseCors(Const.CorsDefaultPolicyName);
        return app;
    }
}
