// ----------------------------------------------------------------------------------------------
// <copyright file="ProxiedResponseValidatorSettings.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.ApiClient.Validators.Settings;

/// <summary>
/// Настройки <see cref="ProxiedResponseValidator"/>.
/// </summary>
/// <param name="MaxLoggedBodyLength">
/// Максимальная длина тела ответа, попадающего в лог (в символах).
/// При превышении тело обрезается с суффиксом <c>…[truncated, total {N} chars]</c>.
/// Значение по умолчанию <c>4096</c>.
/// </param>
public record ProxiedResponseValidatorSettings(
    int MaxLoggedBodyLength = 4096);