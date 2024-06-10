// ----------------------------------------------------------------------------------------------
// <copyright file="ApplicationDependencyInjector.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.DependencyInjection;

namespace Gpn.Template.Setter.Application;

/// <summary>
/// Класс для внедрения зависимостей Application-слоя в Setter
/// </summary>
/// <param name="logger">Логгер.</param>
public class ApplicationDependencyInjector(
    ILogger<ApplicationDependencyInjector> logger
    ) : DependencyInjectorBase(logger)
{
    /// <inheritdoc />
    protected override IServiceCollection Process(IServiceCollection serviceCollection)
    {
        return serviceCollection;
    }
}
