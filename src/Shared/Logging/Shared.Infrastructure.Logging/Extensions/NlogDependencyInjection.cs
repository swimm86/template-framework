// ----------------------------------------------------------------------------------------------
// <copyright file="NlogDependencyInjection.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using Shared.Application.Core.Configuration.Extensions;
using Shared.Common.Extensions;
using Shared.Infrastructure.Logging.Settings;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

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
        var settings = configuration.GetOptions<NlogSettings>();
        var configFileName = string.IsNullOrEmpty(settings?.Path) ? "nlog.base.config" : settings.Path;
        var configPath = Path.IsPathRooted(configFileName)
            ? configFileName
            : Path.Combine(AppContext.BaseDirectory, configFileName);

        // Загружаем конфигурацию NLog вручную, чтобы иметь возможность изменить правила
        var nlogConfig = new XmlLoggingConfiguration(configPath);

        // Определяем минимальный уровень логирования
        var nlogLogLevel = settings?.LogLevel switch
        {
            LogLevel.Trace => NLog.LogLevel.Trace,
            LogLevel.Debug => NLog.LogLevel.Debug,
            LogLevel.Information => NLog.LogLevel.Info,
            LogLevel.Warning => NLog.LogLevel.Warn,
            LogLevel.Error => NLog.LogLevel.Error,
            LogLevel.Critical => NLog.LogLevel.Fatal,
            _ => NLog.LogLevel.Info
        };

        // Переопределяем минимальный уровень для всех правил логирования NLog
        // Это необходимо, так как конфиг из NuGet может содержать неправильный уровень (Trace)
        nlogConfig.LoggingRules.ForEach(rule =>
            rule.SetLoggingLevels(nlogLogLevel, NLog.LogLevel.Fatal));

        // Устанавливаем измененную конфигурацию в LogManager
        LogManager.Configuration = nlogConfig;

        return serviceCollection
            .AddLogging(loggerBuilder =>
            {
                loggerBuilder.ClearProviders();
                loggerBuilder.AddNLog(nlogConfig);

                if (settings is not null)
                    loggerBuilder.SetMinimumLevel(settings.LogLevel);
            });
    }
}
