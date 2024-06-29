// ----------------------------------------------------------------------------------------------
// <copyright file="ApiClientSettingsExtensions.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Shared.Application.Core.ApiClient.Settings.Base;

namespace Shared.Application.Core.ApiClient;

/// <summary>
/// Расширение для настроек <see cref="ApiClientSettingsBase{TClient}"/>.
/// </summary>
public static class ApiClientSettingsExtensions
{
    /// <summary>
    /// Проверяет корректность настроек API-клиента.
    /// </summary>
    /// <typeparam name="TOptions">Тип настроек, который должен быть проверен.</typeparam>
    /// <typeparam name="TApiClient">Тип API-клиента.</typeparam>
    /// <param name="options">Экземпляр настроек для валидации.</param>
    /// <exception cref="ArgumentNullException">Исключение, выбрасываемое, если переданный экземпляр настроек равен null.</exception>
    /// <exception cref="OptionsValidationException">Исключение, выбрасываемое, если обязательные поля в настройках не заполнены или содержат пустые значения.</exception>
    public static void Validate<TOptions, TApiClient>(
        this ApiClientSettingsBase<TApiClient> options)
        where TApiClient : ApiClient
    {
        var optionsType = typeof(TOptions);
        var optionsName = optionsType.Name;

        if (options == default)
        {
            throw new ArgumentNullException(
                optionsName,
                $"Настройки {typeof(TOptions).Name} должны быть заполнены. ");
        }

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            throw new OptionsValidationException(
                optionsName,
                optionsType,
                new[] { $"Поле {nameof(options.BaseUrl)} должно быть заполнено. " });
        }
    }
}
