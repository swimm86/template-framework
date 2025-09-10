// ----------------------------------------------------------------------------------------------
// <copyright file="NlogSettings.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Shared.Infrastructure.Logging.Settings;

/// <summary>
/// Найстройки для Nlog.
/// </summary>
public class NlogSettings
{
    /// <summary>
    /// Путь к nlog.config.
    /// </summary>
    required public string Path { get; init; } = null!;

    /// <summary>
    /// Минимальный уровень логирования.
    /// </summary>
    required public LogLevel LogLevel { get; init; } = LogLevel.Information;
}
