// ----------------------------------------------------------------------------------------------
// <copyright file="DependencyInjector.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Contour.Admin.Auth.Sdk.Extensions;
using Gpn.Template.Application.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Auth;
using Shared.Application.Core.DependencyInjection;

namespace Gpn.Template.Application;

/// <summary>
/// Внедрение зависимостей для Application-слоя.
/// </summary>
public class DependencyInjector(
    IConfiguration configuration,
    ILogger<DependencyInjector> logger)
    : DependencyInjectorBase(logger)
{
    /// <inheritdoc />
    protected override IServiceCollection Process(IServiceCollection serviceCollection)
    {
        return serviceCollection
            .ConfigureSwaggerAuth()
            .AddAuthServices(configuration)
            .AddScoped<IUserProvider, UserProvider>();
    }
}
