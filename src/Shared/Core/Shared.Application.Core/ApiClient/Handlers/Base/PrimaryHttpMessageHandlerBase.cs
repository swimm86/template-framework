// ----------------------------------------------------------------------------------------------
// <copyright file="PrimaryHttpMessageHandlerBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.ApiClient.Handlers.Base;

/// <summary>
/// Базовый класс для primary HTTP message handler-ов.
/// </summary>
/// <remarks>
/// Наследники автоматически регистрируются в DI.
/// </remarks>
public abstract class PrimaryHttpMessageHandlerBase
    : HttpClientHandler;
