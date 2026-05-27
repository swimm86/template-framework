// ----------------------------------------------------------------------------------------------
// <copyright file="LoggingServiceAccessorTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Shared.Common.Logging;
using Xunit;

namespace Shared.Common.Tests.Logging;

[CollectionDefinition(nameof(LoggingServiceAccessorCollection), DisableParallelization = true)]
public class LoggingServiceAccessorCollection;

[Collection(nameof(LoggingServiceAccessorCollection))]
public sealed class LoggingServiceAccessorTests
{
    /// <summary>
    /// После <see cref="LoggingServiceAccessor.Configure"/> вызов GetLogger возвращает не-null логгер.
    /// </summary>
    [Fact]
    public void Configure_ThenGetLogger_ReturnsNonNullLogger()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var provider = services.BuildServiceProvider();

        // Act
        LoggingServiceAccessor.Configure(provider);

        // Assert
        var logger = LoggingServiceAccessor.GetLogger(typeof(LoggingServiceAccessorTests));
        logger.Should().NotBeNull();
    }

    /// <summary>
    /// Повторный вызов <see cref="LoggingServiceAccessor.Configure"/> не вызывает исключения.
    /// </summary>
    [Fact]
    public void Configure_MultipleTimes_DoesNotThrow()
    {
        // Arrange
        var services1 = new ServiceCollection();
        services1.AddLogging();
        var provider1 = services1.BuildServiceProvider();

        var services2 = new ServiceCollection();
        services2.AddLogging();
        var provider2 = services2.BuildServiceProvider();

        // Act
        LoggingServiceAccessor.Configure(provider1);

        // Assert
        var act = () => LoggingServiceAccessor.Configure(provider2);
        act.Should().NotThrow();
    }

    /// <summary>
    /// При повторной конфигурации используется последний провайдер (логгеры разные).
    /// </summary>
    [Fact]
    public void Configure_MultipleTimes_LastConfigurationUsed()
    {
        // Arrange
        var services1 = new ServiceCollection();
        services1.AddLogging();
        var provider1 = services1.BuildServiceProvider();

        var services2 = new ServiceCollection();
        services2.AddLogging();
        var provider2 = services2.BuildServiceProvider();

        // Act
        LoggingServiceAccessor.Configure(provider1);
        var logger1 = LoggingServiceAccessor.GetLogger(typeof(LoggingServiceAccessorTests));

        LoggingServiceAccessor.Configure(provider2);
        var logger2 = LoggingServiceAccessor.GetLogger(typeof(LoggingServiceAccessorTests));

        // Assert
        logger1.Should().NotBeNull();
        logger2.Should().NotBeNull();
        logger2.Should().NotBeSameAs(logger1);
    }
}
