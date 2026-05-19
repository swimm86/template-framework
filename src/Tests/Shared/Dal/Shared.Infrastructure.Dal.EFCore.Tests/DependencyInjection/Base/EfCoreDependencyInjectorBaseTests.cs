// ----------------------------------------------------------------------------------------------
// <copyright file="EfCoreDependencyInjectorBaseTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Infrastructure.Dal.EFCore.DependencyInjection.Base;
using Shared.Infrastructure.Dal.EFCore.Repository;

namespace Shared.Infrastructure.Dal.EFCore.Tests.DependencyInjection.Base;

/// <summary>
/// Тесты <see cref="EfCoreDependencyInjectorBase"/>.
/// Проверяют регистрацию <see cref="IQueryEvaluator"/> и вызов
/// <c>AddDbContexts</c> через шаблонный метод <c>Process</c>.
/// </summary>
public sealed class EfCoreDependencyInjectorBaseTests
{
    /// <summary>
    /// Тестовый injector, наследующий <see cref="EfCoreDependencyInjectorBase"/>
    /// без регистрации конкретных <see cref="Microsoft.EntityFrameworkCore.DbContext"/>.
    /// </summary>
    private sealed class TestEfCoreInjector(
        ILoggerFactory loggerFactory)
        : EfCoreDependencyInjectorBase(loggerFactory);

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
        var services = new ServiceCollection();

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
    /// <c>Inject</c> возвращает ту же коллекцию
    /// сервисов, что и получил (fluent API).
    /// </summary>
    [Fact]
    public void Inject_ReturnsSameServiceCollection()
    {
        // Arrange
        var injector = new TestEfCoreInjector(NullLoggerFactory.Instance);
        var services = new ServiceCollection();

        // Act
        var result = injector.Inject(services);

        // Assert
        result.Should().BeSameAs(services);
    }

    /// <summary>
    /// <c>Inject</c> не выбрасывает исключение
    /// при отсутствии производных <see cref="Microsoft.EntityFrameworkCore.DbContext"/>
    /// в загруженных сборках.
    /// </summary>
    [Fact]
    public void Inject_WhenNoDbContextsLoaded_DoesNotThrow()
    {
        // Arrange
        var injector = new TestEfCoreInjector(NullLoggerFactory.Instance);
        var services = new ServiceCollection();

        // Act
        var act = () => injector.Inject(services);

        // Assert
        act.Should().NotThrow();
    }

    #endregion
}
