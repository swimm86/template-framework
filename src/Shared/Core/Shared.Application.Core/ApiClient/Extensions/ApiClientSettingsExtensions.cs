// ----------------------------------------------------------------------------------------------
// <copyright file="ApiClientSettingsExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Shared.Application.Core.ApiClient.Settings.Base;

namespace Shared.Application.Core.ApiClient.Extensions;

/// <summary>
/// Расширение для настроек <see cref="ApiClientSettingsBase"/>.
/// </summary>
public static class ApiClientSettingsExtensions
{
    /// <summary>
    /// Проверяет корректность базовых настроек API-клиента.
    /// </summary>
    /// <param name="options">Экземпляр настроек для валидации.</param>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="options"/> равен <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// Проверяется обязательность поля <see cref="ApiClientSettingsBase.BaseUrl"/>.
    /// </remarks>
    /// <exception cref="OptionsValidationException">
    /// Выбрасывается, если <see cref="ApiClientSettingsBase.BaseUrl"/> не заполнен.
    /// </exception>
    public static void Validate(
        this ApiClientSettingsBase options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(
                nameof(options),
                "API client settings instance must not be null.");
        }

        var optionsType = options.GetType();
        var optionsName = optionsType.Name;

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            throw new OptionsValidationException(
                optionsName,
                optionsType,
                [$"The '{nameof(options.BaseUrl)}' field must be not null or empty."]);
        }
    }
}
