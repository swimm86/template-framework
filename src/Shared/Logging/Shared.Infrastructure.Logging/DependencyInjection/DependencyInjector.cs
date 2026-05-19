// ----------------------------------------------------------------------------------------------
// <copyright file="DependencyInjector.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.DependencyInjection.Base;
using Shared.Infrastructure.Logging.Extensions;

namespace Shared.Infrastructure.Logging.DependencyInjection;

/// <summary>
/// Регистрация DI-зависимостей слоя <c>Shared.Infrastructure.Logging</c>.
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
    protected override IServiceCollection Process(IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddNlog(configuration);
    }
}
