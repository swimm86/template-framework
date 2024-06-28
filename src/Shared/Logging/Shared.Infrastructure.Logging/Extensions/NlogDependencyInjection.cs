// ----------------------------------------------------------------------------------------------
// <copyright file="NlogDependencyInjection.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Shared.Application.Core.Configuration.Extensions;
using Shared.Infrastructure.Logging.Settings;

namespace Shared.Infrastructure.Logging.Extensions;

/// <summary>
///  Содержит методы расширения <see cref="IServiceCollection"/>.
/// </summary>
public static class NlogDependencyInjection
{
    /// <summary>
    /// Добавление логгера Nlog.
    /// </summary>
    /// <param name="serviceCollection">Коллекция сервисов <see cref="IServiceCollection"/>.</param>
    /// <param name="configuration"><see cref="IServiceCollection"/>.</param>
    /// <returns>Коллекция сервисов <see cref="IConfiguration"/>.</returns>
    public static IServiceCollection AddNlog(
        this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        return serviceCollection
            .AddLogging(loggerBuilder =>
            {
                var settings = configuration.GetOptions<NlogSettings>();
                var configPath = settings?.Path;
                loggerBuilder.ClearProviders();
                loggerBuilder.AddNLog(string.IsNullOrEmpty(configPath) ? "nlog.base.config" : configPath);
            });
    }
}
