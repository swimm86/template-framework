// ----------------------------------------------------------------------------------------------
// <copyright file="CoreDependencyInjector.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.ApiClient;
using Shared.Application.Core.Dal.DbSeeder.Extensions;
using Shared.Application.Core.Dal.Repository.Extensions;
using Shared.Application.Core.DependencyInjection;
using Shared.Application.Core.Exceptions.Extensions;
using Shared.Application.Core.Json;

namespace Shared.Application.Core;

/// <summary>
/// Класс для внедрения зависимостей Application.Core-слоя.
/// </summary>
/// <param name="configuration"><see cref="IConfiguration"/>.</param>
/// <param name="logger">Логгер.</param>
public class CoreDependencyInjector(
    IConfiguration configuration,
    ILogger<CoreDependencyInjector> logger)
    : DependencyInjectorBase(logger)
{
    /// <inheritdoc />
    protected override IServiceCollection Process(IServiceCollection serviceCollection)
    {
        return serviceCollection
            .ConfigureJsonSerializer()
            .AddExceptionsHandlers()
            .AddHttpClients(configuration)
            .AddRepositories()
            .AddDbSeeder();
    }
}
