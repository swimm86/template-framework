// ----------------------------------------------------------------------------------------------
// <copyright file="LoggingServiceAccessorTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Common.Logging;

namespace Shared.Common.Tests.Logging;

/// <summary>
/// Тесты для <see cref="LoggingServiceAccessor"/>.
/// </summary>
public sealed class LoggingServiceAccessorTests
{
    [Fact]
    public void Configure_ThenGetLogger_ReturnsNonNullLogger()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var provider = services.BuildServiceProvider();

        LoggingServiceAccessor.Configure(provider);

        var logger = LoggingServiceAccessor.GetLogger(typeof(LoggingServiceAccessorTests));
        logger.Should().NotBeNull();
    }

    [Fact]
    public void Configure_MultipleTimes_DoesNotThrow()
    {
        var services1 = new ServiceCollection();
        services1.AddLogging();
        var provider1 = services1.BuildServiceProvider();

        var services2 = new ServiceCollection();
        services2.AddLogging();
        var provider2 = services2.BuildServiceProvider();

        LoggingServiceAccessor.Configure(provider1);

        var act = () => LoggingServiceAccessor.Configure(provider2);

        act.Should().NotThrow();
    }

    [Fact]
    public void Configure_MultipleTimes_LastConfigurationUsed()
    {
        var services1 = new ServiceCollection();
        services1.AddLogging();
        var provider1 = services1.BuildServiceProvider();

        var services2 = new ServiceCollection();
        services2.AddLogging();
        var provider2 = services2.BuildServiceProvider();

        LoggingServiceAccessor.Configure(provider1);
        var logger1 = LoggingServiceAccessor.GetLogger(typeof(LoggingServiceAccessorTests));

        LoggingServiceAccessor.Configure(provider2);
        var logger2 = LoggingServiceAccessor.GetLogger(typeof(LoggingServiceAccessorTests));

        logger1.Should().NotBeNull();
        logger2.Should().NotBeNull();
        logger2.Should().NotBeSameAs(logger1);
    }
}
