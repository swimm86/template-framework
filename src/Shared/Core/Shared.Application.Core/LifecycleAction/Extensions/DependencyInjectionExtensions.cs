// ----------------------------------------------------------------------------------------------
// <copyright file="DependencyInjectionExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.DependencyInjection.Extensions;
using Shared.Application.Core.LifecycleAction.Interfaces;

namespace Shared.Application.Core.LifecycleAction.Extensions;

/// <summary>
/// Методы расширения для регистрации обработчиков событий жизненного цикла сущностей в контейнере зависимостей.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Регистрирует все компоненты системы действий перехвата жизненного цикла сущностей.
    /// Включает регистрацию обработчиков и оркестратора.
    /// </summary>
    /// <param name="serviceCollection">Коллекция сервисов для регистрации зависимостей.</param>
    /// <returns>Текущая коллекция сервисов <see cref="IServiceCollection"/>.</returns>
    /// <remarks>
    /// Для валидации создаётся временный <see cref="ServiceProvider"/>
    /// с отключённым <see cref="ServiceProviderOptions.ValidateOnBuild"/>
    /// (чтобы не валидировать весь DI-граф приложения) — резолвятся только
    /// зарегистрированные обработчики. После проверки провайдер закрывается.
    /// </remarks>
    public static IServiceCollection AddLifecycleActions(
        this IServiceCollection serviceCollection)
    {
        serviceCollection
            .AddLifecycleHandlers()
            .AddLifecycleOrchestrator();

        ValidateHandlerKeys(serviceCollection);

        return serviceCollection;
    }

    /// <summary>
    /// Регистрирует все обработчики действий перехвата жизненного цикла сущностей, реализующие интерфейс <see cref="ILifecycleActionHandler"/>.
    /// Автоматически обнаруживает и регистрирует все производные типы как scoped-сервисы.
    /// </summary>
    /// <param name="serviceCollection">Коллекция сервисов для регистрации зависимостей.</param>
    /// <returns>Текущая коллекция сервисов <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddLifecycleHandlers(
        this IServiceCollection serviceCollection)
    {
        return serviceCollection.RegisterDerivedTypeDependencies<ILifecycleActionHandler>(
            serviceTypeAsInterface: true,
            lifetime: ServiceLifetime.Scoped);
    }

    /// <summary>
    /// Регистрирует оркестратор действий перехвата жизненного цикла сущностей.
    /// </summary>
    /// <param name="serviceCollection">Коллекция сервисов для регистрации зависимостей.</param>
    /// <returns>Текущая коллекция сервисов <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddLifecycleOrchestrator(
        this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddScoped<ILifecycleEntityRegistry, LifecycleEntityRegistry>()
            .AddScoped<ILifecycleActionGate, LifecycleActionGate>()
            .AddScoped<ILifecycleActionOrchestrator, LifecycleActionOrchestrator>();
    }

    /// <summary>
    /// Резолвит все зарегистрированные <see cref="ILifecycleActionHandler"/>
    /// через временный <see cref="ServiceProvider"/> и выполняет полную
    /// валидацию.
    /// </summary>
    /// <param name="serviceCollection">Коллекция сервисов для валидации.</param>
    /// <exception cref="InvalidOperationException">
    /// Пробрасывается из <see cref="LifecycleActionValidator.Validate"/>
    /// при обнаружении коллизий.
    /// </exception>
    private static void ValidateHandlerKeys(IServiceCollection serviceCollection)
    {
        using var scope = serviceCollection
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = false,
                ValidateScopes = false,
            })
            .CreateScope();

        var handlers = scope.ServiceProvider.GetServices<ILifecycleActionHandler>();
        LifecycleActionValidator.Validate(handlers);
    }
}
