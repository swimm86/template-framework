// ----------------------------------------------------------------------------------------------
// <copyright file="CorrelationIdScopePropertyKeys.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Infrastructure.Logging.Constants;

/// <summary>
/// Константы для работы с корреляционными идентификаторами.
/// </summary>
public static class CorrelationIdScopePropertyKeys
{
    /// <summary>
    /// Ключ для идентификатора корреляции HTTP запросов в NLog ScopeContext.
    /// </summary>
    public const string Http = "http-correlation-id";

    /// <summary>
    /// Ключ для идентификатора корреляции фоновых задач (джоб) в NLog ScopeContext.
    /// </summary>
    public const string Job = "job-correlation-id";
}
