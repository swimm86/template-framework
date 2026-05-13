// ----------------------------------------------------------------------------------------------
// <copyright file="DependencyInjectionExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Cqrs.Core.Behaviours;
using Shared.Common.Helpers;

namespace Shared.Application.Cqrs.Core.Extensions;

/// <summary>
/// Методы расширения для регистрации CQRS-зависимостей в <see cref="IServiceCollection"/>.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Регистрирует MediatR и pipeline-поведения.
    /// </summary>
    /// <param name="services">Коллекция сервисов <see cref="IServiceCollection"/>.</param>
    /// <returns>Текущая коллекция сервисов <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddMediatR(
        this IServiceCollection services)
    {
        return services
            .AddMediatR(opt => opt.RegisterServicesFromAssemblies(AssemblyHelper.GetAssembliesByPrefix().ToArray()))
            .AddPipelineBehaviours();
    }

    /// <summary>
    /// Регистрирует pipeline-поведения MediatR.
    /// </summary>
    /// <param name="services">Коллекция сервисов <see cref="IServiceCollection"/>.</param>
    /// <returns>Текущая коллекция сервисов <see cref="IServiceCollection"/>.</returns>
    private static IServiceCollection AddPipelineBehaviours(this IServiceCollection services)
    {
        return services
            .AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingPipelineBehaviour<,>))
            .AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehaviour<,>));
    }
}
