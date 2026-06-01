// ----------------------------------------------------------------------------------------------
// <copyright file="DependencyInjectorTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Application.Core.DependencyInjection;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Testing.Doubles.Infrastructure;
using Shared.Testing.Doubles.Logging;
using Shared.Testing.Doubles.Repository;
using Template.Domain.Entities;
using Template.Getter.Application.Interfaces;
using Template.Getter.Application.Services;
using DependencyInjector = Template.Getter.Application.DependencyInjection.DependencyInjector;

namespace Template.Getter.Application.Tests.DependencyInjection;

/// <summary>
/// Тесты <see cref="DependencyInjector"/>.
/// Проверяют регистрацию <see cref="IPersonsService"/>, время жизни сервиса,
/// корректность реализации и логирование успешного внедрения зависимостей.
/// </summary>
public sealed class DependencyInjectorTests
{
    /// <summary>
    /// <see cref="DependencyInjector.Inject"/> регистрирует <see cref="IPersonsService"/>
    /// в коллекции сервисов.
    /// </summary>
    [Fact]
    public void Process_RegistersIPersonsService()
    {
        // Arrange
        var injector = new DependencyInjector(NullLoggerFactory.Instance);
        var services = new ServiceCollection();
        services.AddScoped<IUnitOfWork>(_ => new FakeUnitOfWork());
        services.AddScoped<IRepository<Person>>(_ => new FakeRepository<Person>());

        // Act
        injector.Inject(services);
        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IPersonsService>().Should().NotBeNull();
    }

    /// <summary>
    /// <see cref="DependencyInjector.Inject"/> регистрирует <see cref="IPersonsService"/>
    /// со временем жизни <c>Scoped</c>: два разных scope дают разные экземпляры.
    /// </summary>
    [Fact]
    public void Process_RegistersIPersonsService_AsScoped()
    {
        // Arrange
        var injector = new DependencyInjector(NullLoggerFactory.Instance);
        var services = new ServiceCollection();
        services.AddScoped<IUnitOfWork>(_ => new FakeUnitOfWork());
        services.AddScoped<IRepository<Person>>(_ => new FakeRepository<Person>());

        // Act
        injector.Inject(services);
        using var provider = services.BuildServiceProvider();
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();
        var service1 = scope1.ServiceProvider.GetService<IPersonsService>();
        var service2 = scope2.ServiceProvider.GetService<IPersonsService>();

        // Assert
        service1.Should().NotBeNull();
        service2.Should().NotBeNull();
        service1.Should().NotBeSameAs(service2);
    }

    /// <summary>
    /// <see cref="DependencyInjector.Inject"/> регистрирует <see cref="IPersonsService"/>
    /// с реализацией <see cref="PersonsService"/>.
    /// </summary>
    [Fact]
    public void Process_ImplementationType_IsPersonsService()
    {
        // Arrange
        var injector = new DependencyInjector(NullLoggerFactory.Instance);
        var services = new ServiceCollection();

        // Act
        injector.Inject(services);
        var descriptor = services.Single(s => s.ServiceType == typeof(IPersonsService));

        // Assert
        descriptor.ImplementationType.Should().Be(typeof(PersonsService));
    }

    /// <summary>
    /// <see cref="DependencyInjector.Inject"/> возвращает ту же
    /// <see cref="IServiceCollection"/>, что и получил (fluent API).
    /// </summary>
    [Fact]
    public void Process_ReturnsSameServiceCollection()
    {
        // Arrange
        var injector = new DependencyInjector(NullLoggerFactory.Instance);
        var services = new ServiceCollection();

        // Act
        var result = injector.Inject(services);

        // Assert
        result.Should().BeSameAs(services);
    }

    /// <summary>
    /// <see cref="DependencyInjector.Inject"/> при успешном выполнении
    /// логирует информационное сообщение.
    /// </summary>
    [Fact]
    public void Inject_LogsInformation_OnSuccess()
    {
        // Arrange
        var logger = new FakeLogger();
        var factory = new FakeLoggerFactory(logger);
        var injector = new DependencyInjector(factory);
        var services = new ServiceCollection();

        // Act
        injector.Inject(services);

        // Assert
        logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Information)
            .Which.Message.Should().Be(DependencyInjectionLogMessages.DependenciesInjected);
    }
}
