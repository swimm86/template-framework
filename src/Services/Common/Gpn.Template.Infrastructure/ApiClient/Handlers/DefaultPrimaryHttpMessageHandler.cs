// ----------------------------------------------------------------------------------------------
// <copyright file="DefaultPrimaryHttpMessageHandler.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.ApiClient.Handlers.Attributes;
using Shared.Application.Core.ApiClient.Handlers.Base;

namespace Gpn.Template.Infrastructure.ApiClient.Handlers;

/// <summary>
/// Primary HTTP message handler для BFF-сервиса.
/// </summary>
/// <remarks>
/// Проверка серверного TLS-сертификата отключена по требованию стенда заказчика.
/// Стенд работает в закрытом контуре, где TLS-терминация и контроль сетевого трафика
/// обеспечиваются на внешнем периметре (nginx, silium).
/// </remarks>
[ApiClientHandlerMetadata(order: 0)]
public sealed class DefaultPrimaryHttpMessageHandler
    : PrimaryHttpMessageHandlerBase
{
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DefaultPrimaryHttpMessageHandler"/>.
    /// </summary>
    public DefaultPrimaryHttpMessageHandler()
    {
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
    }
}
