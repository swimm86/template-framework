// ----------------------------------------------------------------------------------------------
// <copyright file="DependencyInjectorTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Mapster;
using Shared.Domain.Core.Mapping.Interfaces;
using Shared.Infrastructure.Mapper.Mapster.DependencyInjection;

namespace Shared.Infrastructure.Mapper.Mapster.Tests.DependencyInjection;

/// <summary>
/// Тесты для <see cref="DependencyInjector"/> — регистрации Mapster в DI-контейнере.
/// </summary>
public sealed class DependencyInjectorTests
{
    /// <summary>
    /// <see cref="DependencyInjector"/> регистрирует <see cref="IMapper"/> и <see cref="TypeAdapterConfig"/> в DI.
    /// </summary>
    [Fact]
    public void Process_RegistersMapster()
    {
        // Arrange
        var services = new ServiceCollection();
        var injector = new DependencyInjector(NullLoggerFactory.Instance);

        // Act
        injector.Inject(services);

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(IMapper));
        services.Should().Contain(sd => sd.ServiceType == typeof(TypeAdapterConfig));
    }

    /// <summary>
    /// <see cref="IMapper"/> регистрируется как Singleton.
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
