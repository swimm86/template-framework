// ----------------------------------------------------------------------------------------------
// <copyright file="NlogSettings.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

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
}
