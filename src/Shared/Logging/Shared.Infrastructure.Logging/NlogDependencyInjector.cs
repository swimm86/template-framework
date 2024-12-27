// ----------------------------------------------------------------------------------------------
// <copyright file="NlogDependencyInjector.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.DependencyInjection;
using Shared.Infrastructure.Logging.Extensions;

namespace Shared.Infrastructure.Logging;

/// <summary>
/// Класс, предназначенный для интеграции nLog в DI через <see cref="IServiceCollection"/>.
/// </summary>
public class NlogDependencyInjector(
    IConfiguration configuration,
    ILogger<NlogDependencyInjector> logger
) : DependencyInjectorBase(logger)
{
    /// <summary>
    /// Инициализирует зависимости nLog (вызывается неявно).
    /// </summary>
    /// <param name="serviceCollection"><see cref="IServiceCollection"/>.</param>
    /// <returns><see cref="IServiceCollection"/>.</returns>
    protected override IServiceCollection Process(IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddNlog(configuration);
    }
}
