// ----------------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Quartz;

namespace Shared.Infrastructure.Job.Quartz;

/// <summary>
/// Константы для интеграции с Quartz и DI-контейнером.
/// </summary>
/// <remarks>
/// Используются как ключи в <c><see cref="JobDataMap"/></c> для передачи данных
/// между планировщиком и задачами.
/// </remarks>
public static class Constants
{
    /// <summary>
    /// Ключ в <c><see cref="JobDataMap"/></c> для лямбда-действия.
    /// Используется <c><see cref="QuartzScheduledJobAdapter"/></c> для восстановления делегата при выполнении.
    /// </summary>
    public const string ActionDataKey = "JobAction";

    /// <summary>
    /// Ключ в <c><see cref="JobDataMap"/></c>, под которым хранится полное имя типа фоновой задачи
    /// (для получения из DI при выполнении).
    /// </summary>
    public const string JobTypeKey = "JobType";

    /// <summary>
    /// Ключ в <c><see cref="JobDataMap"/></c>, под которым хранится ключ keyed-сервиса в DI
    /// (опционально).
    /// </summary>
    public const string ServiceKeyKey = "ServiceKey";

    /// <summary>
    /// Ключ в <c><see cref="JobDataMap"/></c>, под которым хранятся настройки in-process retry.
    /// </summary>
    public const string RetryOptionsKey = "RetryOptions";
}
