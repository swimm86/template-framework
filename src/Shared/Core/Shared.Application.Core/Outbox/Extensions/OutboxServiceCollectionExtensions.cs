// ----------------------------------------------------------------------------------------------
// <copyright file="OutboxServiceCollectionExtensions.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.Outbox.Handlers;
using Shared.Application.Core.Outbox.Interfaces;
using Shared.Application.Core.Outbox.Settings;

namespace Shared.Application.Core.Outbox.Extensions;

/// <summary>
/// Расширения для IServiceCollection для регистрации Outbox сервисов.
/// </summary>
public static class OutboxServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует полный стек Outbox сервисов.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="configuration">Конфигурация.</param>
    /// <returns>Коллекция сервисов для цепочки вызовов.</returns>
    public static IServiceCollection AddOutbox(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Регистрируем настройки
        services.Configure<OutboxSettings>(configuration.GetSection(OutboxSettings.SectionName));

        // Регистрируем основные сервисы
        services.AddOutboxServices();

        // Регистрируем стандартный HTTP обработчик
        services.AddOutboxEventHandler<HttpOutboxEventHandler>();

        // Регистрируем HttpClient для HTTP обработчика
        services.AddHttpClient();

        return services;
    }

    /// <summary>
    /// Регистрирует полный стек Outbox сервисов с кастомными настройками.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="configureSettings">Действие для настройки параметров.</param>
    /// <returns>Коллекция сервисов для цепочки вызовов.</returns>
    public static IServiceCollection AddOutbox(
        this IServiceCollection services,
        Action<OutboxSettings> configureSettings)
    {
        // Регистрируем настройки
        services.Configure(configureSettings);

        // Регистрируем основные сервисы
        services.AddOutboxServices();

        // Регистрируем стандартный HTTP обработчик
        services.AddOutboxEventHandler<HttpOutboxEventHandler>();

        // Регистрируем HttpClient для HTTP обработчика
        services.AddHttpClient();

        return services;
    }
}

