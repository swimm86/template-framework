// ----------------------------------------------------------------------------------------------
// <copyright file="DefaultPrimaryHttpMessageHandler.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.ApiClient.Handlers.Attributes;

namespace Template.Infrastructure.ApiClient.Handlers;

/// <summary>
/// Primary HTTP message handler для BFF-сервиса.
/// </summary>
/// <remarks>
/// Проверка серверного TLS-сертификата отключена по требованию стенда заказчика.
/// Стенд работает в закрытом контуре, где TLS-терминация и контроль сетевого трафика
/// обеспечиваются на внешнем периметре (nginx, silium).
/// </remarks>
[ApiClientPrimaryHttpHandlerMetadata]
public sealed class DefaultPrimaryHttpMessageHandler
    : HttpClientHandler
{
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DefaultPrimaryHttpMessageHandler"/>.
    /// </summary>
    public DefaultPrimaryHttpMessageHandler()
    {
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
    }
}
