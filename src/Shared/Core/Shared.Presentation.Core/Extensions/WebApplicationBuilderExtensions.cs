// ----------------------------------------------------------------------------------------------
// <copyright file="WebApplicationBuilderExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.Configuration.Extensions;
using Shared.Application.Core.DependencyInjection.Base;
using Shared.Infrastructure.Core.DependencyInjection.Extensions;
using Shared.Presentation.Core.Conventions;
using Shared.Presentation.Core.RequestLogging.Filters;

namespace Shared.Presentation.Core.Extensions;

/// <summary>
/// Методы расширения для настройки <see cref="WebApplicationBuilder"/> с использованием компонентов Shared.Presentation.Core.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Осуществляет инциализацию .env-конфигурации и регистрацию зависимостей из ссылочных сборок
    /// (<see cref="Infrastructure.Core.DependencyInjection.Extensions.ServiceCollectionExtensions"/>).
    /// </summary>
    /// <remarks>
    /// Данный метод загружает настройки из .env файлов, регистрирует контроллеры с кастомными конвенциями,
    /// добавляет фильтр логирования запросов и автоматически находит и выполняет все реализации <see cref="DependencyInjectorBase"/>.
    /// </remarks>
    /// <param name="builder">Построитель веб-приложения ASP.NET Core.</param>
    /// <returns>Текущий <paramref name="builder"/> для цепочечных вызовов.</returns>
    public static WebApplicationBuilder ImplementDependencies(
        this WebApplicationBuilder builder)
    {
        builder.Configuration.InitializeConfiguration(builder.Environment);
        builder.Services
            .AddControllers(options =>
            {
                options.Conventions.Add(new ControllerTypeConvention());
                options.Conventions.Add(new ControllerNameConvention());
                options.Filters.Add<RequestLoggingFilter>();
            }).Services
            .AddReferencedDependencyInjectors();
        return builder;
    }
}
