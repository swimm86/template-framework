// ----------------------------------------------------------------------------------------------
// <copyright file="RelativeUriValidator.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Security;
using Shared.Application.Core.ApiClient.Interfaces;

namespace Shared.Application.Core.ApiClient.Validators;

/// <summary>
/// Валидатор относительных URI.
/// </summary>
/// <remarks>
/// Запрещает:
/// <list type="bullet">
/// <item>Абсолютные URI (защита от SSRF)</item>
/// <item>Path traversal (<c>..</c>) с циклическим декодированием</item>
/// <item>URI, начинающиеся с <c>/</c> или <c>\</c></item>
/// </list>
/// </remarks>
public sealed class RelativeUriValidator
    : IUriValidator
{
    /// <inheritdoc />
    public void Validate(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            throw new ArgumentException("URI не может быть пустым", nameof(uri));
        }

        // Запрет абсолютных URI (защита от SSRF)
        if (Uri.IsWellFormedUriString(uri, UriKind.Absolute))
        {
            throw new SecurityException(
                $"Абсолютные URI запрещены. Используйте относительный путь. URI: {uri}");
        }

        // Запрет path traversal с циклическим декодированием
        var decoded = uri;
        string prev;
        do
        {
            prev = decoded;
            decoded = Uri.UnescapeDataString(decoded);
        }
        while (decoded != prev);

        if (decoded.Contains(".."))
        {
            throw new SecurityException(
                $"Path traversal запрещён. URI: {uri}");
        }

        // Запрет абсолютных путей (начинающихся с / или \)
        if (uri.StartsWith('/') || uri.StartsWith('\\'))
        {
            throw new SecurityException(
                $"URI должен быть относительным путём без начального слэша. URI: {uri}");
        }

        // Проверка формата относительного URI
        if (!Uri.IsWellFormedUriString(uri, UriKind.Relative))
        {
            throw new FormatException($"Невалидный относительный URI: {uri}");
        }
    }
}
