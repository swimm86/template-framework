using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Infrastructure.Mapper.AutoMapper.DependencyInjection;

using IMapper = Shared.Domain.Core.Mapping.Interfaces.IMapper;

namespace Shared.Infrastructure.Mapper.AutoMapper.Tests.DependencyInjection;

/// <summary>
/// Тесты для <see cref="DependencyInjector"/> — регистрации AutoMapper в DI-контейнере.
/// </summary>
public sealed class DependencyInjectorTests
{
    /// <summary>
    /// Проверяет, что метод <see cref="DependencyInjector.Inject"/> регистрирует <see cref="IMapper"/>
    /// и <see cref="IConfigurationProvider"/> в коллекции сервисов.
    /// </summary>
    [Fact]
    public void Process_RegistersAutoMapper()
    {
        // Arrange
        var services = new ServiceCollection();
        var injector = new DependencyInjector(NullLoggerFactory.Instance);

        // Act
        injector.Inject(services);

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(IMapper));
        services.Should().Contain(sd => sd.ServiceType == typeof(IConfigurationProvider));
    }

    /// <summary>
    /// Проверяет, что <see cref="IMapper"/> регистрируется с временем жизни Singleton.
    /// </summary>
    [Fact]
    public void Process_RegistersMapperAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        var injector = new DependencyInjector(NullLoggerFactory.Instance);

        // Act
        injector.Inject(services);

        // Assert
        var registration = services.SingleOrDefault(sd => sd.ServiceType == typeof(IMapper));
        registration.Should().NotBeNull();
        registration!.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }
}
