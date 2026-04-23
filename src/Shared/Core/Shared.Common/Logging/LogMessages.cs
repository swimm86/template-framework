// ----------------------------------------------------------------------------------------------
// <copyright file="LogMessages.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Common.Logging;

/// <summary>
/// Единое хранилище шаблонов structured-logging сообщений для механизмов логирования.
/// Используется как <see cref="Attributes.LogMethodAttribute"/>, так и
/// <see cref="global::Shared.Common.Logging.Extensions.LoggerExtensions"/>,
/// гарантируя идентичность форматов сообщений в обоих подходах.
/// </summary>
internal static class LogMessages
{
    /// <summary>Шаблон сообщения о начале выполнения процесса.</summary>
    internal const string Started = "{process} started.";

    /// <summary>Шаблон сообщения об успешном завершении процесса.</summary>
    internal const string Completed = "{process} completed.";

    /// <summary>Шаблон сообщения об ошибке выполнения процесса.</summary>
    internal const string Failed = "{process} failed.";

    /// <summary>Шаблон сообщения о времени выполнения процесса.</summary>
    internal const string Elapsed = "{process} processed time: {time}ms.";
}
