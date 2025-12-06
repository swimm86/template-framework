// ----------------------------------------------------------------------------------------------
// <copyright file="OutboxDependencyInjection.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.Outbox.Interfaces;

namespace Shared.Application.Core.Outbox.Extensions;

/// <summary>
/// Расширения для регистрации Outbox сервисов.
/// </summary>
public static class OutboxDependencyInjection
{
    /// <summary>
    /// Регистрирует Outbox сервисы.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <returns>Коллекция сервисов для цепочки вызовов.</returns>
    public static IServiceCollection AddOutboxServices(this IServiceCollection services)
    {
        services.AddScoped<IOutboxService, OutboxService>();
        services.AddScoped<OutboxEventProcessor>();

        return services;
    }

    /// <summary>
    /// Регистрирует обработчик Outbox событий.
    /// </summary>
    /// <typeparam name="THandler">Тип обработчика.</typeparam>
    /// <param name="services">Коллекция сервисов.</param>
    /// <returns>Коллекция сервисов для цепочки вызовов.</returns>
    public static IServiceCollection AddOutboxEventHandler<THandler>(this IServiceCollection services)
        where THandler : class, IOutboxEventHandler
    {
        services.AddScoped<IOutboxEventHandler, THandler>();
        return services;
    }
}

