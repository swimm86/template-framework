// ----------------------------------------------------------------------------------------------
// <copyright file="DbSettingsBase.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
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

    /// <summary>
    /// Признак того, что по-умолчанию включена транзакционность.
    /// </summary>
    required public bool TransactionsEnabled { get; set; } = true;
}
