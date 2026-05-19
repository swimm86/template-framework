// ----------------------------------------------------------------------------------------------
// <copyright file="DependencyInjectionLogMessages.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.DependencyInjection;

/// <summary>
/// Сообщения логирования при регистрации зависимостей.
/// </summary>
public static class DependencyInjectionLogMessages
{
    /// <summary>Успешная регистрация зависимостей.</summary>
    public const string DependenciesInjected = "Dependencies injected.";

    /// <summary>Ошибка при регистрации зависимостей.</summary>
    public const string DependenciesNotInjected = "Dependencies not injected.";
}
