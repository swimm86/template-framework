// ----------------------------------------------------------------------------------------------
// <copyright file="InfrastructureDependencyInjector.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.DependencyInjection;

namespace Gpn.Template.Getter.Infrastructure;

/// <summary>
/// Класс для внедрения зависимостей Infrastructure-слоя в Getter
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
