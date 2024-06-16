// ----------------------------------------------------------------------------------------------
// <copyright file="CqrsDependencyInjector.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.DependencyInjection;
using Shared.Application.Cqrs.Core.Extensions;

namespace Shared.Application.Cqrs.Core;

/// <summary>
/// Класс для внедрения зависимостей Application.Core-слоя.
/// </summary>
/// <param name="logger">Логгер.</param>
public class CqrsDependencyInjector(
    ILogger<CqrsDependencyInjector> logger)
    : DependencyInjectorBase(logger)
{
    /// <inheritdoc />
    protected override IServiceCollection Process(IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddMediatr();
    }
}