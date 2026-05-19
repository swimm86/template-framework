// ----------------------------------------------------------------------------------------------
// <copyright file="ApiClientSettingsBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Options;

namespace Shared.Application.Core.ApiClient.Settings.Base;

/// <summary>
/// Базовые настройки API-клиента.
/// </summary>
public abstract class ApiClientSettingsBase
{
    /// <summary>
    /// Значение таймаута HTTP-запроса по умолчанию (в секундах), используемое для инициализации
    /// <see cref="Timeout"/>, если в конфигурации не задано иное.
    /// </summary>
    private const int DefaultTimeoutSec = 100;

    /// <summary>
    /// Базовый адрес сервиса.
    /// </summary>
    public virtual string BaseUrl { get; set; } = null!;

    /// <summary>
    /// Таймаут HTTP-запроса.
    /// Значение по умолчанию — <see cref="DefaultTimeoutSec"/> секунд;
    /// переопределяется через конфигурацию сервиса.
    /// </summary>
    public virtual TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(DefaultTimeoutSec);

    /// <summary>
    /// Проверяет корректность настроек API-клиента.
    /// </summary>
    /// <remarks>
    /// Проверяется обязательность поля <see cref="BaseUrl"/>.
    /// </remarks>
    /// <exception cref="OptionsValidationException">
    /// Выбрасывается, если <see cref="BaseUrl"/> не заполнен.
    /// </exception>
    public void Validate()
    {
        var optionsType = GetType();
        var optionsName = optionsType.Name;

        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            throw new OptionsValidationException(
                optionsName,
                optionsType,
                [$"The '{nameof(BaseUrl)}' field must be not null or empty."]);
        }
    }
}
