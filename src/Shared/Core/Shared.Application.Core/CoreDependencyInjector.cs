// ----------------------------------------------------------------------------------------------
// <copyright file="CoreDependencyInjector.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.ApiClient.Interfaces;
using Shared.Application.Core.ApiClient.Validators;
using Shared.Application.Core.Cache;
using Shared.Application.Core.Dal.DbSeeder.Extensions;
using Shared.Application.Core.Dal.Extensions;
using Shared.Application.Core.DependencyInjection;
using Shared.Application.Core.Json;
using Shared.Domain.Core.Cache.Interfaces;
using Shared.Domain.Core.Utils.Extensions;

namespace Shared.Application.Core;

/// <summary>
/// Класс для внедрения зависимостей Application.Core-слоя.
/// </summary>
/// <param name="logger">Логгер.</param>
public class CoreDependencyInjector(
    ILogger<CoreDependencyInjector> logger)
    : DependencyInjectorBase(logger)
{
    /// <inheritdoc />
    protected override IServiceCollection Process(IServiceCollection serviceCollection)
    {
        return serviceCollection
            .ConfigureJsonSerializer()
            .AddRepositories()
            .AddDbSeeder()
            .AddPropertyUtil()
            .AddSingleton<IUriValidator, RelativeUriValidator>()
            .AddSingleton<IResponseValidator, ProxiedResponseValidator>()
            .AddScoped<IScopedMemoryCache, ScopedMemoryCache>();
    }
}
