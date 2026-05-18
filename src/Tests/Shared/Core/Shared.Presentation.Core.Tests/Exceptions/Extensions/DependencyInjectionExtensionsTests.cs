using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shared.Presentation.Core.Exceptions;
using Shared.Presentation.Core.Exceptions.Extensions;
using Shared.Presentation.Core.Exceptions.Interfaces;
using Shared.Presentation.Core.Exceptions.Mappers;
using Shared.Presentation.Core.Tests.Infrastructure;

namespace Shared.Presentation.Core.Tests.Exceptions.Extensions;

/// <summary>
/// Интеграционные тесты регистрации обработки исключений через <c>AddExceptionHandling()</c>.
/// </summary>
public sealed class DependencyInjectionExtensionsTests
{
    /// <summary>
    /// Проверяет регистрацию <see cref="IExceptionMapperResolver"/> в DI.
    /// </summary>
    [Fact]
    public void AddExceptionHandling_RegistersIExceptionMapperResolver()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(TestConfigurationBuilder.Empty());

        // Act
        services.AddExceptionHandling();
        var provider = services.BuildServiceProvider();

        // Assert
        var resolver = provider.GetService<IExceptionMapperResolver>();
        resolver.Should().NotBeNull();
        resolver.Should().BeOfType<ExceptionMapperResolver>();
    }

    /// <summary>
    /// Проверяет регистрацию <see cref="IExceptionHandler"/> в DI.
    /// </summary>
    [Fact]
    public void AddExceptionHandling_RegistersIExceptionHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(TestConfigurationBuilder.Empty());

        // Act
        services.AddExceptionHandling();
        var provider = services.BuildServiceProvider();

        // Assert
        var handlers = provider.GetServices<IExceptionHandler>();
        handlers.Should().ContainSingle(h => h is ExceptionHandler);
    }

    /// <summary>
    /// Проверяет авторегистрацию всех мапперов из сборки Shared.Presentation.Core.
    /// </summary>
    [Fact]
    public void AddExceptionHandling_RegistersAllMappers()
    {
        // Arrange — touch type to ensure assembly is loaded
        _ = typeof(DefaultExceptionMapper);
        var services = new ServiceCollection();
        services.AddSingleton(TestConfigurationBuilder.Empty());

        // Act
        services.AddExceptionHandling();
        var provider = services.BuildServiceProvider();

        // Assert
        var mappers = provider.GetServices<IExceptionMapper>().ToList();
        var mapperTypes = mappers.Select(m => m.GetType()).ToHashSet();
        var expectedTypes = new HashSet<Type>
        {
            typeof(DefaultExceptionMapper),
            typeof(AppExceptionMapper),
            typeof(NotFoundExceptionMapper),
            typeof(BusinessLogicExceptionMapper),
            typeof(UnauthorizedExceptionMapper),
            typeof(ValidationExceptionMapper),
            typeof(ProxiedExceptionMapper),
            typeof(AggregateExceptionMapper),
        };
        mapperTypes.Should().BeEquivalentTo(expectedTypes);
    }

    /// <summary>
    /// Проверяет, что все мапперы регистрируются как Singleton.
    /// </summary>
    [Fact]
    public void AddExceptionHandling_RegistersMappersAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(TestConfigurationBuilder.Empty());

        // Act
        services.AddExceptionHandling();

        // Assert
        var descriptors = services.Where(s => s.ServiceType == typeof(IExceptionMapper)).ToList();
        descriptors.Should().NotBeEmpty();
        descriptors.Should().AllSatisfy(d => d.Lifetime.Should().Be(ServiceLifetime.Singleton));
    }

    /// <summary>
    /// Проверяет успешную валидацию DI-контейнера при построении.
    /// </summary>
    [Fact]
    public void AddExceptionHandling_BuildsValidServiceProvider_WithValidateOnBuild()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(TestConfigurationBuilder.Empty());
        services.AddSingleton(Options.Create(new ProblemDetailsOptions()));
        services.AddSingleton(Options.Create(new Microsoft.AspNetCore.Http.Json.JsonOptions()));

        // Act
        services.AddExceptionHandling();

        // Assert — не выбрасывает исключений
        var act = () => services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
        act.Should().NotThrow();
    }

    /// <summary>
    /// Проверяет fluent-API: метод возвращает ту же коллекцию сервисов.
    /// </summary>
    [Fact]
    public void AddExceptionHandling_ReturnsSameServiceCollectionInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(TestConfigurationBuilder.Empty());

        // Act
        var result = services.AddExceptionHandling();

        // Assert
        result.Should().BeSameAs(services);
    }
}
