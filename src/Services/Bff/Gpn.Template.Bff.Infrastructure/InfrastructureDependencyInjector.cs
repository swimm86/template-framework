// ----------------------------------------------------------------------------------------------
// <copyright file="InfrastructureDependencyInjector.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.DependencyInjection;

namespace Gpn.Template.Bff.Infrastructure;

/// <summary>
/// Класс для внедрения зависимостей Infrastructure-слоя в Bff
/// </summary>
/// <param name="logger">Логгер.</param>
public class InfrastructureDependencyInjector(
    ILogger<InfrastructureDependencyInjector> logger
    ) : DependencyInjectorBase(logger)
{
    /// <inheritdoc />
    protected override IServiceCollection Process(IServiceCollection serviceCollection)
    {
        return serviceCollection;
    }
}
