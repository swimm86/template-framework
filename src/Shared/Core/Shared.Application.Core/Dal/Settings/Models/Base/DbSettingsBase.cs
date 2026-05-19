// ----------------------------------------------------------------------------------------------
// <copyright file="DbSettingsBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Dal.Settings.Models.Base;

/// <summary>
/// Базовая конфигурация подключения к базе данных.
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
