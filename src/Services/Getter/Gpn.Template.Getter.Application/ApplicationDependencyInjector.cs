// ----------------------------------------------------------------------------------------------
// <copyright file="ApplicationDependencyInjector.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Getter.Application.Interfaces;
using Gpn.Template.Getter.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.DependencyInjection;

namespace Gpn.Template.Getter.Application;

/// <summary>
/// Класс для внедрения зависимостей Application-слоя в Getter
/// </summary>
/// <param name="logger">Логгер.</param>
public class ApplicationDependencyInjector(
    ILogger<ApplicationDependencyInjector> logger
    ) : DependencyInjectorBase(logger)
{
    /// <inheritdoc />
    protected override IServiceCollection Process(IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddTransient<IPersonsService, PersonsService>();
    }
}
