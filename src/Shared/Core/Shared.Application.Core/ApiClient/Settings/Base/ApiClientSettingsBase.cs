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
    /// Таймаут в секундах.
    /// </summary>
    // TODO: перенести в конфиг.
    private const int TimeoutSec = 10000;

    /// <summary>
    /// Базовый адрес сервиса.
    /// </summary>
    public virtual string BaseUrl { get; set; } = null!;

    /// <summary>
    /// Http timeout.
    /// </summary>
    public virtual TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(TimeoutSec);
}
