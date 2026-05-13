// ----------------------------------------------------------------------------------------------
// <copyright file="DependencyInjector.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Configuration.Extensions;
using Shared.Application.Core.Dal.Settings.Models.Base;
using Shared.Application.Core.DependencyInjection.Base;
using Template.Infrastructure.Dal.Settings;

namespace Template.Infrastructure.Dal.DependencyInjection;

/// <summary>
/// Регистрация DI-зависимостей слоя: <c>Common.Infrastructure.Dal</c>.
/// </summary>
/// <inheritdoc cref="DependencyInjectorBase" path="/remarks"/>
/// <param name="configuration">Конфигурация приложения (<see cref="IConfiguration"/>).</param>
/// <param name="loggerFactory"><inheritdoc cref="DependencyInjectorBase(ILoggerFactory)" path="/param[@name='loggerFactory']"/></param>
public class DependencyInjector(
    IConfiguration configuration,
    ILoggerFactory loggerFactory)
    : DependencyInjectorBase(loggerFactory)
{
    /// <inheritdoc />
    protected override IServiceCollection Process(
        IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSingleton<DbSettingsBase, DbSettings>(_ =>
            {
                var result = configuration.GetOptions<DbSettings>()!;
                return result;
            });
    }
}
