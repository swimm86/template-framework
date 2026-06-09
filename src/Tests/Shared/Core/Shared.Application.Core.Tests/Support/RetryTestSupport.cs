// ----------------------------------------------------------------------------------------------
// <copyright file="RetryTestSupport.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Job.Pipeline;

namespace Shared.Application.Core.Tests.Support;

/// <summary>
/// Хелперы для построения <see cref="RetryOptions"/> в unit-тестах:
/// короткие задержки и фиксированные значения, чтобы тесты не зависели
/// от реальных временных интервалов.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="RetryOptions.MaxAttempts"/> по умолчанию равен <c>3</c> —
/// это типовой сценарий «успех с первой попытки, провал до retry, успех со второй».
/// </para>
/// <para>
/// <see cref="RetryOptions.Delay"/> сознательно выбран <c>1 мс</c>: для теста
/// важно лишь дождаться первой возможности <c>Task.Delay</c> вернуть управление,
/// а не измерять саму задержку. Тест <c>InvokeAsync_RespectsConfiguredDelayBetweenAttempts</c>
/// подставляет большее значение явно.
/// </para>
/// </remarks>
public static class RetryTestSupport
{
    /// <summary>
    /// <see cref="RetryOptions"/> с типовыми параметрами для тестов:
    /// <c>MaxAttempts = 3</c>, <c>Delay = 1 мс</c>.
    /// </summary>
    /// <returns>Новый экземпляр <see cref="RetryOptions"/>.</returns>
    public static RetryOptions DefaultOptions() =>
        new()
        {
            MaxAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(1),
        };

    /// <summary>
    /// <see cref="RetryOptions"/> на основе <see cref="DefaultOptions"/> с подменой
    /// <see cref="RetryOptions.MaxAttempts"/>. Используется, когда в тесте важен
    /// именно счётчик попыток, а задержка может оставаться дефолтной.
    /// </summary>
    /// <param name="maxAttempts">Новое значение <see cref="RetryOptions.MaxAttempts"/>.</param>
    /// <returns>Новый экземпляр <see cref="RetryOptions"/>.</returns>
    public static RetryOptions WithMaxAttempts(int maxAttempts) =>
        new()
        {
            MaxAttempts = maxAttempts,
            Delay = DefaultOptions().Delay,
        };

    /// <summary>
    /// <see cref="RetryOptions"/> на основе <see cref="DefaultOptions"/> с подменой
    /// <see cref="RetryOptions.Delay"/>. Используется, когда в тесте важна именно
    /// длительность задержки (например, для проверки уважения <see cref="CancellationToken"/>
    /// в середине <c>Task.Delay</c>).
    /// </summary>
    /// <param name="delay">Новое значение <see cref="RetryOptions.Delay"/>.</param>
    /// <returns>Новый экземпляр <see cref="RetryOptions"/>.</returns>
    public static RetryOptions WithDelay(TimeSpan delay) =>
        new()
        {
            MaxAttempts = DefaultOptions().MaxAttempts,
            Delay = delay,
        };
}
