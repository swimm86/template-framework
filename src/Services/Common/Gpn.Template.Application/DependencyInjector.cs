// ----------------------------------------------------------------------------------------------
// <copyright file="DependencyInjector.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.DependencyInjection;

namespace Gpn.Template.Application;

/// <summary>
/// Внедрение зависимостей для Application-слоя.
/// </summary>
public class DependencyInjector(
    ILogger<DependencyInjector> logger)
    : DependencyInjectorBase(logger)
{
    /// <inheritdoc />
    protected override IServiceCollection Process(
        IServiceCollection serviceCollection)
    {
        return serviceCollection;
    }
}
