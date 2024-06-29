// ----------------------------------------------------------------------------------------------
// <copyright file="ApiClientSettingsBase.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
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
    /// Базовый адрес сервиса.
    /// </summary>
    public virtual string BaseUrl { get; set; } = null!;

    /// <summary>
    /// Http timeout.
    /// </summary>
    public virtual TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
}
