// ----------------------------------------------------------------------------------------------
// <copyright file="IUriValidator.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Security;

namespace Shared.Application.Core.ApiClient.Interfaces;

/// <summary>
/// Валидатор URI для HTTP-запросов.
/// </summary>
/// <remarks>
/// Обеспечивает защиту от SSRF-атак (запрет абсолютных URI),
/// path traversal (запрет <c>..</c> в пути) и гарантирует,
/// что используются только относительные URI.
/// </remarks>
public interface IUriValidator
{
    /// <summary>
    /// Валидирует URI, обеспечивая что это относительный путь без path traversal.
    /// </summary>
    /// <param name="uri">URI для валидации.</param>
    /// <exception cref="ArgumentException">
    /// Выбрасывается, если <paramref name="uri"/> равен <see langword="null"/>, пуст или состоит из пробелов.
    /// </exception>
    /// <exception cref="SecurityException">
    /// Выбрасывается, если <paramref name="uri"/> является абсолютным URI или содержит path traversal.
    /// </exception>
    void Validate(string uri);
}
