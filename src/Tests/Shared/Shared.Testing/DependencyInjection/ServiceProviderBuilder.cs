using Microsoft.Extensions.DependencyInjection;

namespace Shared.Testing.DependencyInjection;

/// <summary>
/// Сборка <see cref="ServiceProvider"/> для тестов с проверкой графа зависимостей (аналог паттерна из интеграционных фикстур).
/// </summary>
public static class ServiceProviderBuilder
{
    /// <summary>
    /// Строит провайдер с <see cref="ServiceProviderOptions.ValidateOnBuild"/> и <see cref="ServiceProviderOptions.ValidateScopes"/>.
    /// </summary>
    /// <param name="configureServices">Настройка коллекции сервисов.</param>
    /// <returns>Собранный провайдер; вызывайте <c>Dispose</c> / <c>DisposeAsync</c> по окончании теста.</returns>
    public static ServiceProvider Build(
        Action<IServiceCollection> configureServices)
    {
        ArgumentNullException.ThrowIfNull(configureServices);

        var services = new ServiceCollection();
        configureServices(services);

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }
}
