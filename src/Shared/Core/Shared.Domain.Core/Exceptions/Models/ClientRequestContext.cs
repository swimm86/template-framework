// ----------------------------------------------------------------------------------------------
// <copyright file="ClientRequestContext.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Exceptions.Models;

/// <summary>
/// Контекст данных запроса, содержащий информацию о клиенте.
/// </summary>
public class ClientRequestContext
{
    /// <summary>
    /// Имя клиента.
    /// </summary>
    public string ClientName { get; }

    /// <summary>
    /// Абсолютный путь URL, к которому осуществлялся запрос.
    /// </summary>
    public string AbsolutePath { get; }

    /// <summary>
    /// Инициализация <see cref="ClientRequestContext"/>.
    /// </summary>
    /// <param name="clientName">Имя клиента, если известно.</param>
    /// <param name="absolutePath">Абсолютный путь.</param>
    public ClientRequestContext(string clientName, string absolutePath)
    {
        ClientName = clientName;
        AbsolutePath = absolutePath;
    }
}
