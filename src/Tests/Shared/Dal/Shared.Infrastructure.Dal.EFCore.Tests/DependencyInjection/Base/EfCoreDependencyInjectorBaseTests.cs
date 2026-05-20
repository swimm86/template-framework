// ----------------------------------------------------------------------------------------------
// <copyright file="EfCoreDependencyInjectorBaseTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Mapping.Interfaces;
using Shared.Infrastructure.Dal.EFCore.DependencyInjection.Base;
using Shared.Infrastructure.Dal.EFCore.Interfaces;
using Shared.Infrastructure.Dal.EFCore.Repository;
using Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;
using Shared.Testing.Doubles.Mapping;

namespace Shared.Infrastructure.Dal.EFCore.Tests.DependencyInjection.Base;

/// <summary>
/// Тесты <see cref="EfCoreDependencyInjectorBase"/>.
/// Проверяют регистрацию <see cref="IQueryEvaluator"/> и вызов
/// <c>AddDbContexts</c> через шаблонный метод <c>Process</c>.
/// Тестовые DbContext-классы (TestDbContext, TestDbContextForBase и др.) помечены
/// [ManualConfiguration], поэтому <c>AddDbContexts()</c> находит только
/// <see cref="InjectorTestDbContext"/>, для которого существует <see cref="InjectorTestDbSettings"/>.
/// </summary>
public sealed class EfCoreDependencyInjectorBaseTests
{
    /// <summary>
    /// Тестовый injector, наследующий <see cref="EfCoreDependencyInjectorBase"/>
    /// без добавления дополнительной логики.
    /// </summary>
    private sealed class TestEfCoreInjector(ILoggerFactory loggerFactory)
        : EfCoreDependencyInjectorBase(loggerFactory);

    private static ServiceCollection BuildBaseServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment, FakeHostEnvironment>();
        services.AddSingleton<IDbContextOptionsBuilderInitializer, InMemoryDbContextOptionsBuilderInitializer>();
        services.AddSingleton<IMapper, FakeMapper>();
        return services;
    }

    #region Process Tests

    /// <summary>
    /// <c>Process</c> регистрирует <see cref="IQueryEvaluator"/>
    /// с реализацией <see cref="EfQueryEvaluator"/> как singleton.
    /// </summary>
    [Fact]
    public void Process_RegistersQueryEvaluatorAsSingleton()
    {
        // Arrange
        var injector = new TestEfCoreInjector(NullLoggerFactory.Instance);
        var services = BuildBaseServices();

        // Act
        injector.Inject(services);

        // Assert
        services.Should().ContainSingle(d => d.ServiceType == typeof(IQueryEvaluator))
            .Which.Should().Match<ServiceDescriptor>(d =>
                d.Lifetime == ServiceLifetime.Singleton &&
                d.ImplementationType == typeof(EfQueryEvaluator));
    }

    #endregion

    #region Inject Tests

    /// <summary>
    /// <c>Inject</c> возвращает ту же коллекцию сервисов (fluent API).
    /// </summary>
    [Fact]
    public void Inject_ReturnsSameServiceCollection()
    {
        // Arrange
        var injector = new TestEfCoreInjector(NullLoggerFactory.Instance);
        var services = BuildBaseServices();

        // Act
        var result = injector.Inject(services);

        // Assert
        result.Should().BeSameAs(services);
    }

    /// <summary>
    /// <c>Inject</c> регистрирует <see cref="InjectorTestDbContext"/> через <c>AddDbContexts</c>,
    /// т.к. это единственный DbContextBase-наследник без [ManualConfiguration].
    /// </summary>
    [Fact]
    public void Inject_RegistersInjectorTestDbContext()
    {
        // Arrange
        var injector = new TestEfCoreInjector(NullLoggerFactory.Instance);
        var services = BuildBaseServices();

        // Act
        injector.Inject(services);

        // Assert
        services.Should().Contain(d => d.ServiceType == typeof(InjectorTestDbContext));
    }

    /// <summary>
    /// После регистрации через <c>Inject</c> контейнер успешно разрешает
    /// <see cref="IQueryEvaluator"/> как <see cref="EfQueryEvaluator"/>.
    /// </summary>
    [Fact]
    public async Task Inject_ServiceProvider_ResolvesQueryEvaluator()
    {
        // Arrange
        var injector = new TestEfCoreInjector(NullLoggerFactory.Instance);
        var services = BuildBaseServices();
        injector.Inject(services);

        // Act
        await using var provider = services.BuildServiceProvider();
        var evaluator = provider.GetRequiredService<IQueryEvaluator>();

        // Assert
        evaluator.Should().BeOfType<EfQueryEvaluator>();
    }

    #endregion
}
