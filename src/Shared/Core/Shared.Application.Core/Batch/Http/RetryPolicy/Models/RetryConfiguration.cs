// ----------------------------------------------------------------------------------------------
// <copyright file="RetryConfiguration.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Batch.Http.RetryPolicy.Interfaces;

namespace Shared.Application.Core.Batch.Http.RetryPolicy.Models;

/// <summary>
/// Параметры повторных попыток страничного запроса.
/// </summary>
/// <remarks>
/// Совместно с <see cref="IHttpBatchRetryPolicy"/> и типовой реализацией <see cref="DefaultHttpBatchRetryPolicy"/> задаёт поведение повторов.
/// </remarks>
public sealed record RetryConfiguration
{
    /// <summary>
    /// Параметры числа попыток и экспоненциальной задержки.
    /// </summary>
    public BackoffConfiguration Backoff { get; init; } = new();

    /// <summary>
    /// Дополнительная классификация транзиентности и подсказки по паузе.
    /// </summary>
    public TransientConfiguration Transient { get; init; } = new();

    /// <summary>
    /// Проверяет корректность значений (делегирует <see cref="BackoffConfiguration.Validate"/>).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Некорректная конфигурация backoff.</exception>
    public void Validate() => Backoff.Validate();
}
