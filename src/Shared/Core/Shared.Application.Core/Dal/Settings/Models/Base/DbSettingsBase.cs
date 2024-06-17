// ----------------------------------------------------------------------------------------------
// <copyright file="DbSettingsBase.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Dal.Settings.Models.Base;

/// <summary>
/// Базовая конфигурация для бд.
/// </summary>
public abstract class DbSettingsBase
{
    /// <summary>
    /// Строка подключения к БД.
    /// </summary>
    required public string ConnectionString { get; init; } = null!;
}
