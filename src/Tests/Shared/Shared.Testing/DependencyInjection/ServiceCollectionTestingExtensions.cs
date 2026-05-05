using Microsoft.Extensions.DependencyInjection;

namespace Shared.Testing.DependencyInjection;

/// <summary>
/// Расширения для подмены регистраций в тестовом <see cref="IServiceCollection"/> (копировать «продовую» конфигурацию и точечно заменять зависимости).
/// </summary>
public static class ServiceCollectionTestingExtensions
{
    /// <summary>
    /// Удаляет существующие дескрипторы <typeparamref name="TService"/> и регистрирует singleton-экземпляр.
    /// </summary>
    public static IServiceCollection ReplaceSingleton<TService>(
        this IServiceCollection services,
        TService instance)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(instance);

        RemoveAllDescriptorsFor<TService>(services);
        return services.AddSingleton(instance);
    }

    /// <summary>
    /// Удаляет существующие дескрипторы <typeparamref name="TService"/> и регистрирует singleton через фабрику.
    /// </summary>
    public static IServiceCollection ReplaceSingleton<TService>(
        this IServiceCollection services,
        Func<IServiceProvider, TService> implementationFactory)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(implementationFactory);

        RemoveAllDescriptorsFor<TService>(services);
        return services.AddSingleton(implementationFactory);
    }

    private static void RemoveAllDescriptorsFor<TService>(
        IServiceCollection services)
    {
        for (var i = services.Count - 1; i >= 0; i--)
        {
            if (services[i].ServiceType == typeof(TService))
            {
                services.RemoveAt(i);
            }
        }
    }
}
