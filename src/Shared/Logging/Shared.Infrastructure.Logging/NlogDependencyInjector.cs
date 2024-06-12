// ----------------------------------------------------------------------------------------------
// <copyright file="NlogDependencyInjector.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Shared.Application.Core.Configuration;
using Shared.Application.Core.DependencyInjection;
using Shared.Infrastructure.Logging.Settings;

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
        serviceCollection.AddLogging(loggerBuilder =>
        {
            var settings = configuration.GetOptions<NlogSettings>();
            var configPath = settings?.Path;
            loggerBuilder.ClearProviders();
            loggerBuilder.AddNLog(string.IsNullOrEmpty(configPath) ? "nlog.config" : configPath);
        });

        return serviceCollection;
    }
}
