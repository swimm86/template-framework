// ----------------------------------------------------------------------------------------------
// <copyright file="ApiClientSettingsBase.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.ApiClient.Settings.Base;

/// <summary>
/// Базовые настройки api-клиента.
/// </summary>
/// <typeparam name="TApiClient">Тип API-клиента.</typeparam>
public abstract class ApiClientSettingsBase<TApiClient>
    where TApiClient : ApiClient
{
    /// <summary>
    /// Таймаут HTTP-запроса по умолчанию (100 секунд).
    /// </summary>
    private const int DefaultTimeoutSec = 100;

    /// <summary>
    /// Базовый адрес сервиса.
    /// </summary>
    public virtual string BaseUrl { get; set; } = null!;

    /// <summary>
    /// Таймаут HTTP-запроса.
    /// Переопределите в конфигурации для изменения значения по умолчанию.
    /// </summary>
    public virtual TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(DefaultTimeoutSec);
}
