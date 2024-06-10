// ----------------------------------------------------------------------------------------------
// <copyright file="ApiClientSettingsExtensions.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Shared.Application.Core.ApiClient.Settings.Base;

namespace Shared.Application.Core.ApiClient;

/// <summary>
/// Расширение для настроек <see cref="ApiClientSettingsBase"/>.
/// </summary>
public static class ApiClientSettingsExtensions
{
    /// <summary>
    /// Валидация настроек.
    /// </summary>
    /// <param name="options"> Нстройки. </param>
    /// <exception cref="ArgumentNullException"> Вызывается при значении настроек == null. </exception>
    /// <exception cref="OptionsValidationException"> Вызывается при пустых значениях полей настроек. </exception>
    public static void Validate<TOptions>(this ApiClientSettingsBase options)
    {
        var optionsType = typeof(TOptions);
        var optionsName = optionsType.Name;

        if (options == default)
        {
            throw new ArgumentNullException(optionsName,
                $"Настройки {typeof(TOptions).Name} должны быть заполнены. ");
        }

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            throw new OptionsValidationException(optionsName,
                optionsType,
                new[] { $"Поле {nameof(options.BaseUrl)} должно быть заполнено. " });
        }
    }
}
