// ----------------------------------------------------------------------------------------------
// <copyright file="RetryOptions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Job.Pipeline.Middlewares;

namespace Shared.Application.Core.Job.Pipeline;

/// <summary>
/// Настройки in-process retry для <see cref="RetryMiddleware"/>.
/// </summary>
public sealed class RetryOptions
{
    private int _maxAttempts = 3;

    /// <summary>
    /// Максимальное число попыток выполнения (включая первую).
    /// Должно быть не менее <c>1</c>.
    /// По умолчанию <c>3</c>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Выбрасывается при установке значения меньше <c>1</c>.
    /// </exception>
    public int MaxAttempts
    {
        get => _maxAttempts;
        set => _maxAttempts = value >= 1
            ? value
            : throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "MaxAttempts must be at least 1.");
    }

    /// <summary>
    /// Задержка между попытками.
    /// По умолчанию <c>30</c> секунд.
    /// </summary>
    public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(30);
}
