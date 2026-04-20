// ----------------------------------------------------------------------------------------------
// <copyright file="LoggingServiceAccessor.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shared.Common.Logging;

/// <summary>
/// Статический аксессор для получения <see cref="ILoggerFactory"/> из AOP-атрибутов,
/// где нет прямого доступа к DI-контейнеру.
/// </summary>
public static class LoggingServiceAccessor
{
    private static volatile ILoggerFactory? _loggerFactory;

    /// <summary>
    /// Конфигурирует аксессор, извлекая <see cref="ILoggerFactory"/> из <see cref="IServiceProvider"/>.
    /// Должен вызываться при старте приложения до первого использования атрибута <c>[LogMethod]</c>.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> приложения.</param>
    public static void Configure(IServiceProvider serviceProvider)
    {
        _loggerFactory = serviceProvider.GetService<ILoggerFactory>();
    }

    /// <summary>
    /// Создаёт экземпляр <see cref="ILogger"/> для указанного типа.
    /// </summary>
    /// <param name="type">Тип, для которого создаётся логгер.</param>
    /// <returns>Экземпляр <see cref="ILogger"/> или <c>null</c>, если фабрика не сконфигурирована.</returns>
    public static ILogger? GetLogger(Type type)
    {
        Debug.Assert(
            _loggerFactory != null,
            $"{nameof(LoggingServiceAccessor)}.{nameof(Configure)} was not called.");
        return _loggerFactory?.CreateLogger(type);
    }
}
