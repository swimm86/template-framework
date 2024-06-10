// ----------------------------------------------------------------------------------------------
// <copyright file="DbSettingsBase.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Infrastructure.Dal.EFCore.Settings;

/// <summary>
/// Базовые настройки подключения к БД.
/// </summary>
public class DbSettings
{
    /// <summary>
    /// Строка подключения к БД.
    /// </summary>
    public required string ConnectionString { get; init; } = null!;
}
