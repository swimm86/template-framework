// ----------------------------------------------------------------------------------------------
// <copyright file="BackoffConfiguration.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Batch.Http.RetryPolicy.Models;

/// <summary>
/// Экспоненциальная пауза между попытками запроса одной страницы.
/// </summary>
public sealed record BackoffConfiguration
{
    /// <summary>
    /// Максимум попыток выполнения запроса одной страницы, включая первую. Значение <c>1</c> отключает повторы.
    /// </summary>
    public int MaxAttempts { get; init; } = 1;

    /// <summary>
    /// Задержка перед первой повторной попыткой; для последующих используется экспоненциальный backoff (множитель 2).
    /// </summary>
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Верхняя граница паузы между попытками. <see langword="null"/> — без ограничения.
    /// </summary>
    public TimeSpan? MaxDelay { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Если <see langword="true"/> (по умолчанию), к базовой паузе применяется случайный множитель примерно в диапазоне <c>[0.75; 1.25]</c>.
    /// </summary>
    public bool UseBackoffJitter { get; init; } = true;

    /// <summary>
    /// Проверяет корректность значений.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Некорректные свойства записи.</exception>
    public void Validate()
    {
        if (MaxAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaxAttempts),
                MaxAttempts,
                $"{nameof(MaxAttempts)} must be a positive number.");
        }

        if (MaxAttempts > 1 && InitialDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(InitialDelay),
                InitialDelay,
                $"{nameof(InitialDelay)} cannot be negative when retries are enabled.");
        }

        if (MaxDelay is { } cap && cap < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaxDelay),
                cap,
                $"{nameof(MaxDelay)} cannot be negative.");
        }
    }
}
