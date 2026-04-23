// ----------------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Infrastructure.Logging;

/// <summary>
/// Константы для работы с корреляционными идентификаторами.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Ключ для идентификатора корреляции HTTP запросов в NLog ScopeContext.
    /// </summary>
    public const string HttpCorrelationIdScopePropertyKey = "http-correlation-id";

    /// <summary>
    /// Ключ для идентификатора корреляции фоновых задач (джоб) в NLog ScopeContext.
    /// </summary>
    public const string JobCorrelationIdScopePropertyKey = "job-correlation-id";
}
