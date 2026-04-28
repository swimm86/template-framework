// ----------------------------------------------------------------------------------------------
// <copyright file="ApiClientSettingsBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

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
}
