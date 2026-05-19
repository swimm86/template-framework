// ----------------------------------------------------------------------------------------------
// <copyright file="RequestLoggingSettings.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Presentation.Core.RequestLogging.Settings;

/// <summary>
/// Настройки фильтра логирования аргументов контроллера.
/// </summary>
public record RequestLoggingSettings
{
    /// <summary>
    /// Максимальная глубина сериализации вложенных объектов.
    /// по умолчанию: 50.
    /// </summary>
    public int MaxDepth { get; init; } = 50;

    /// <summary>
    /// Максимальный размер JSON payload в байтах.
    /// по умолчанию: 10MB.
    /// </summary>
    public int MaxJsonPayloadLength { get; init; } = 10 * 1024 * 1024;

    /// <summary>
    /// Признак активности фильтра.
    /// по умолчанию: true.
    /// </summary>
    public bool IsEnabled { get; init; } = true;
}
