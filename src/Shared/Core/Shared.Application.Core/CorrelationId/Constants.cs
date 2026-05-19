// ----------------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.CorrelationId;

/// <summary>
/// Константы для работы с корреляционными идентификаторами.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Заголовок для идентификатора корреляции запроса.
    /// </summary>
    public const string CorrelationIdHeader = "X-Correlation-Id";
}
