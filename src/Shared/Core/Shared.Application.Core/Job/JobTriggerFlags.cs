// ----------------------------------------------------------------------------------------------
// <copyright file="JobTriggerFlags.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Job;

/// <summary>
/// Флаги триггеров задач.
/// </summary>
[Flags]
public enum JobTriggerFlags
{
    /// <summary>
    /// Ежедневно.
    /// </summary>
    Daily = 1 << 0,

    /// <summary>
    /// Еженедельно.
    /// </summary>
    Weekly = 1 << 1,

    /// <summary>
    /// Ежемесячно.
    /// </summary>
    Monthly = 1 << 2,

    /// <summary>
    /// При запуске приложения.
    /// </summary>
    OnStartup = 1 << 3,

    /// <summary>
    /// Каждую минуту.
    /// </summary>
    EveryMinute = 1 << 4,

    /// <summary>
    /// Каждый час.
    /// </summary>
    EveryHour = 1 << 5,
}