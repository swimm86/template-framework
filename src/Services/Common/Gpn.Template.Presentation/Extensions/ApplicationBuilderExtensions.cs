// ----------------------------------------------------------------------------------------------
// <copyright file="ApplicationBuilderExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Shared.Presentation.Core.Extensions;

namespace Gpn.Template.Presentation.Extensions;

/// <summary>
/// Класс, который содержит расширения для <see cref="IApplicationBuilder"/>.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Конфигурирует <see cref="IApplicationBuilder"/>.
    /// </summary>
    /// <param name="app"><see cref="WebApplication"/>.</param>
    /// <returns><see cref="IApplicationBuilder"/>.</returns>
    public static IApplicationBuilder UseCommonPresentation(
        this WebApplication app)
    {
        return app
            .UsePresentationCore()
            .UseCors(Constants.CorsDefaultPolicyName);
    }
}
