// ----------------------------------------------------------------------------------------------
// <copyright file="TransientConfiguration.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Batch.Http.RetryPolicy.Models;

/// <summary>
/// Дополнительные правила транзиентности и паузы перед повтором (поверх встроенной эвристики).
/// </summary>
public sealed record TransientConfiguration
{
    /// <summary>
    /// Ключ в <see cref="Exception.Data"/> для паузы в стиле заголовка Retry-After (в секундах).
    /// </summary>
    public const string RetryAfterExceptionDataKey = "Retry-After";

    /// <summary>
    /// При возврате <see langword="true"/> исключение считается транзиентным наряду со встроенными правилами.
    /// </summary>
    public Func<Exception, bool>? IsAdditionalTransientException { get; init; }

    /// <summary>
    /// Дополнительная нижняя граница паузы перед повтором.
    /// </summary>
    public Func<Exception, TimeSpan?>? ResolveRetryDelayAfterTransientFailure { get; init; }
}