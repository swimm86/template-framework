// ----------------------------------------------------------------------------------------------
// <copyright file="OutboxSettings.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Outbox.Settings;

/// <summary>
/// Настройки для Outbox pattern.
/// </summary>
public class OutboxSettings
{
    /// <summary>
    /// Секция конфигурации в appsettings.json.
    /// </summary>
    public const string SectionName = "Outbox";

    /// <summary>
    /// Включен ли процессор Outbox событий.
    /// </summary>
    public bool ProcessorEnabled { get; set; } = true;

    /// <summary>
    /// CRON выражение для планировщика обработки событий.
    /// По умолчанию: каждую минуту.
    /// </summary>
    public string ProcessorCronExpression { get; set; } = "0 * * * * ?";

    /// <summary>
    /// Размер батча для обработки событий за один проход.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Длительность блокировки события в минутах при обработке.
    /// </summary>
    public int LockDurationMinutes { get; set; } = 5;

    /// <summary>
    /// Включена ли автоматическая очистка обработанных событий.
    /// </summary>
    public bool CleanupEnabled { get; set; } = true;

    /// <summary>
    /// CRON выражение для планировщика очистки событий.
    /// По умолчанию: каждый час.
    /// </summary>
    public string CleanupCronExpression { get; set; } = "0 0 * * * ?";

    /// <summary>
    /// Удалять обработанные события старше указанного количества дней.
    /// </summary>
    public int CleanupOlderThanDays { get; set; } = 30;

    /// <summary>
    /// Максимальное количество попыток обработки по умолчанию.
    /// </summary>
    public int DefaultMaxRetryCount { get; set; } = 5;

    /// <summary>
    /// Таймаут HTTP запроса по умолчанию в секундах.
    /// </summary>
    public int DefaultTimeoutSeconds { get; set; } = 100;

    /// <summary>
    /// Приоритет по умолчанию для новых событий.
    /// </summary>
    public int DefaultPriority { get; set; } = 0;
}

