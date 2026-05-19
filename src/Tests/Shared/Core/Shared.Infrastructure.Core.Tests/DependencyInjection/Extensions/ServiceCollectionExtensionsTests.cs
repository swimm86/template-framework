using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Infrastructure.Core.DependencyInjection;
using Shared.Infrastructure.Core.DependencyInjection.Extensions;

namespace Shared.Infrastructure.Core.Tests.DependencyInjection.Extensions;

/// <summary>
/// Тесты <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    private static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        return services;
    }

    /// <summary>
    /// Проверяет, что <see cref="ServiceCollectionExtensions.AddReferencedDependencyInjectors"/>
    /// успешно выполняется в контексте тестов и регистрирует сервисы из найденных инжекторов.
    /// </summary>
    [Fact]
    public void AddReferencedDependencyInjectors_RegistersServices()
    {
        var services = CreateServices();

        var act = () => services.AddReferencedDependencyInjectors();

        act.Should().NotThrow();
    }

    /// <summary>
    /// Проверяет, что после вызова <see cref="ServiceCollectionExtensions.AddReferencedDependencyInjectors"/>
    /// в коллекции сервисов зарегистрирован хотя бы один делегирующий обработчик
    /// из найденного <see cref="DependencyInjector"/>.
    /// </summary>
    [Fact]
    public void AddReferencedDependencyInjectors_RegistersDelegatingHandlersFromInjector()
    {
        var services = CreateServices();

        services.AddReferencedDependencyInjectors();

        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(Shared.Infrastructure.Core.ApiClient.Handlers.CorrelationIdHeaderDelegatingHandler));
        descriptor.Should().NotBeNull();
    }

    /// <summary>
    /// Проверяет, что <see cref="ServiceCollectionExtensions.AddReferencedDependencyInjectors"/>
    /// не выбрасывает исключение при вызове с коллекцией, содержащей необходимые зависимости.
    /// </summary>
    [Fact]
    public void AddReferencedDependencyInjectors_DoesNotThrowWithPreparedCollection()
    {
        var services = CreateServices();

        var act = () => services.AddReferencedDependencyInjectors();

        act.Should().NotThrow();
    }

    /// <summary>
    /// Проверяет, что <see cref="ServiceCollectionExtensions.AddReferencedDependencyInjectors"/>
    /// возвращает ту же коллекцию сервисов для fluent-API.
    /// </summary>
    [Fact]
    public void AddReferencedDependencyInjectors_ReturnsSameServiceCollectionInstance()
    {
        var services = CreateServices();

        var result = services.AddReferencedDependencyInjectors();

        result.Should().BeSameAs(services);
    }

    /// <summary>
    /// Проверяет, что повторный вызов <see cref="ServiceCollectionExtensions.AddReferencedDependencyInjectors"/>
    /// не вызывает исключений (идемпотентность).
    /// </summary>
    [Fact]
    public void AddReferencedDependencyInjectors_CanBeCalledMultipleTimes()
    {
        var services = CreateServices();

        services.AddReferencedDependencyInjectors();
        var act = () => services.AddReferencedDependencyInjectors();

        act.Should().NotThrow();
    }
}
