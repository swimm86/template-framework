// ----------------------------------------------------------------------------------------------
// <copyright file="DependencyInjection.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Cqrs.Core.Behaviours;
using Shared.Common.Helpers;

namespace Shared.Application.Cqrs.Core.Extensions;

/// <summary>
/// Добавление зависимостей в DI
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Регистрация MediatR
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/>.</param>
    /// <returns><see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddMediatr(this IServiceCollection services)
    {
        return services
            .AddMediatR(opt => opt.RegisterServicesFromAssemblies(AssemblyHelper.GetAssembliesByPrefix().ToArray()))
            .AddPipelineBehaviours();
    }

    /// <summary>
    /// Регистрация пайплайнов MediatR
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/>.</param>
    /// <returns><see cref="IServiceCollection"/>.</returns>
    private static IServiceCollection AddPipelineBehaviours(this IServiceCollection services)
    {
        return services
            .AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingPipelineBehaviour<,>))
            .AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehaviour<,>));
    }
}
