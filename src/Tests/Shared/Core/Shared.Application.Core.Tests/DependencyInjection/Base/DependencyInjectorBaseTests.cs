// ----------------------------------------------------------------------------------------------
// <copyright file="DependencyInjectorBaseTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Application.Core.DependencyInjection;
using Shared.Application.Core.DependencyInjection.Base;
using Shared.Testing.Doubles.Infrastructure;
using Shared.Testing.Doubles.Logging;

namespace Shared.Application.Core.Tests.DependencyInjection.Base;

/// <summary>
/// Тесты шаблонного метода <see cref="DependencyInjectorBase"/>.
/// Проверяют общее для всех наследников поведение:
/// fluent API, вызов <c>Process</c>, логирование успеха/ошибки и проброс исключений.
/// </summary>
public sealed class DependencyInjectorBaseTests
{
    /// <summary>
    /// Тестовый injector, позволяющий переопределить <c>Process</c>
    /// и отслеживать факт его вызова.
    /// </summary>
    private sealed class TestInjector(
        ILoggerFactory loggerFactory,
        Func<IServiceCollection, IServiceCollection>? process = null)
        : DependencyInjectorBase(loggerFactory)
    {
        /// <summary>
        /// Признак того, что <c>Process</c> был вызван.
        /// </summary>
        public bool ProcessCalled { get; private set; }

        /// <inheritdoc />
        protected override IServiceCollection Process(
            IServiceCollection serviceCollection)
        {
            ProcessCalled = true;
            return process is not null ? process(serviceCollection) : serviceCollection;
        }
    }

    #region Inject Tests

    /// <summary>
    /// <see cref="DependencyInjectorBase.Inject"/> возвращает ту же
    /// коллекцию сервисов, что и получил (fluent API).
    /// </summary>
    [Fact]
    public void Inject_ReturnsSameServiceCollection()
    {
        // Arrange
        var injector = new TestInjector(NullLoggerFactory.Instance);
        var services = new ServiceCollection();

        // Act
        var result = injector.Inject(services);

        // Assert
        result.Should().BeSameAs(services);
    }

    /// <summary>
    /// <see cref="DependencyInjectorBase.Inject"/> вызывает
    /// абстрактный метод <c>Process</c> ровно один раз.
    /// </summary>
    [Fact]
    public void Inject_CallsProcessExactlyOnce()
    {
        // Arrange
        var injector = new TestInjector(NullLoggerFactory.Instance);
        var services = new ServiceCollection();

        // Act
        injector.Inject(services);

        // Assert
        injector.ProcessCalled.Should().BeTrue();
    }

    /// <summary>
    /// При успешном выполнении <c>Process</c> в лог
    /// записывается информационное сообщение "Dependencies injected.".
    /// </summary>
    [Fact]
    public void Inject_WhenProcessSucceeds_LogsInformation()
    {
        // Arrange
        var logger = new FakeLogger();
        var factory = new FakeLoggerFactory(logger);
        var injector = new TestInjector(factory);
        var services = new ServiceCollection();

        // Act
        injector.Inject(services);

        // Assert
        logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Information)
            .Which.Message.Should().Be(DependencyInjectionLogMessages.DependenciesInjected);
    }

    /// <summary>
    /// При исключении в <c>Process</c> в лог записывается
    /// ошибка с исходным исключением, и исключение пробрасывается наружу.
    /// </summary>
    [Fact]
    public void Inject_WhenProcessThrows_LogsErrorAndRethrows()
    {
        // Arrange
        var logger = new FakeLogger();
        var factory = new FakeLoggerFactory(logger);
        var expectedException = new InvalidOperationException("Test failure");
        var injector = new TestInjector(factory, _ => throw expectedException);
        var services = new ServiceCollection();

        // Act
        var act = () => injector.Inject(services);

        // Assert
        act.Should().ThrowExactly<InvalidOperationException>()
            .Which.Should().BeSameAs(expectedException);
        logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Error)
            .Which.Exception.Should().BeSameAs(expectedException);
    }

    #endregion
}
