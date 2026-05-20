using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Infrastructure.Core.DependencyInjection;

namespace Shared.Infrastructure.Core.Tests.DependencyInjection;

/// <summary>
/// Тесты <see cref="DependencyInjector"/>.
/// </summary>
public sealed class DependencyInjectorTests
{
    /// <summary>
    /// Проверяет, что <c>Inject</c> инициализирует контекст
    /// конфигураторов API-клиентов.
    /// </summary>
    [Fact]
    public void Inject_InitializesApiClientBuilderConfiguratorContext()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        var loggerFactory = NullLoggerFactory.Instance;

        var injector = new DependencyInjector(configuration, loggerFactory);

        // Act
        var act = () => injector.Inject(services);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Проверяет, что <c>Inject</c> регистрирует
    /// делегирующие обработчики в коллекции сервисов.
    /// </summary>
    [Fact]
    public void Inject_RegistersDelegatingHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        var loggerFactory = NullLoggerFactory.Instance;

        var injector = new DependencyInjector(configuration, loggerFactory);

        // Act
        injector.Inject(services);

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(Shared.Infrastructure.Core.ApiClient.Handlers.CorrelationIdHeaderDelegatingHandler));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    /// <summary>
    /// Проверяет, что <c>Inject</c> возвращает
    /// ту же коллекцию сервисов для fluent-API.
    /// </summary>
    [Fact]
    public void Inject_ReturnsSameServiceCollectionInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        var loggerFactory = NullLoggerFactory.Instance;

        var injector = new DependencyInjector(configuration, loggerFactory);

        // Act
        var result = injector.Inject(services);

        // Assert
        result.Should().BeSameAs(services);
    }
}
