// ----------------------------------------------------------------------------------------------
// <copyright file="DependencyInjector.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.DependencyInjection.Base;
using Shared.Application.Cqrs.Core.Extensions;

namespace Shared.Application.Cqrs.Core.DependencyInjection;

/// <summary>
/// Регистрация DI-зависимостей слоя: <c>Shared.Application.Cqrs.Core</c>.
/// </summary>
/// <inheritdoc cref="DependencyInjectorBase" path="/remarks"/>
/// <param name="loggerFactory"><inheritdoc cref="DependencyInjectorBase(ILoggerFactory)" path="/param[@name='loggerFactory']"/></param>
public class DependencyInjector(
    ILoggerFactory loggerFactory)
    : DependencyInjectorBase(loggerFactory)
{
    /// <inheritdoc />
    protected override IServiceCollection Process(
        IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddMediatR();
    }
}
