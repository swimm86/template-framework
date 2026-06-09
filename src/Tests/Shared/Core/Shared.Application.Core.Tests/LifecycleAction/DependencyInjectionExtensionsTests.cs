// ----------------------------------------------------------------------------------------------
// <copyright file="DependencyInjectionExtensionsTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.LifecycleAction;
using Shared.Application.Core.LifecycleAction.Extensions;
using Shared.Application.Core.LifecycleAction.Interfaces;
using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.Tests.LifecycleAction;

/// <summary>
/// Тесты для <see cref="DependencyInjectionExtensions"/>.
/// Проверяют fluent API, регистрацию <see cref="ILifecycleActionOrchestrator"/>
/// и время жизни scoped.
/// </summary>
public sealed class DependencyInjectionExtensionsTests
{
    /// <summary>
    /// Тестовая сущность, для которой существует handler в тестах.
    /// </summary>
    private sealed class TestEntity
        : IEntity
    {
        private Guid Id { get; } = Guid.NewGuid();

        object IEntity.Id => Id;
    }

    /// <summary>
    /// <see cref="DependencyInjectionExtensions.AddLifecycleActions"/>
    /// возвращает ту же коллекцию (fluent API).
    /// </summary>
    [Fact]
    public void AddLifecycleActions_ReturnsSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddLifecycleActions();

        // Assert
        result.Should().BeSameAs(services);
    }

    /// <summary>
    /// <see cref="DependencyInjectionExtensions.AddLifecycleOrchestrator"/>
    /// возвращает ту же коллекцию (fluent API).
    /// </summary>
    [Fact]
    public void AddLifecycleOrchestrator_ReturnsSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddLifecycleOrchestrator();

        // Assert
        result.Should().BeSameAs(services);
    }

    /// <summary>
    /// <see cref="DependencyInjectionExtensions.AddLifecycleHandlers"/>
    /// возвращает ту же коллекцию (fluent API).
    /// </summary>
    [Fact]
    public void AddLifecycleHandlers_ReturnsSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddLifecycleHandlers();

        // Assert
        result.Should().BeSameAs(services);
    }

    /// <summary>
    /// После <see cref="DependencyInjectionExtensions.AddLifecycleOrchestrator"/>
    /// <see cref="ILifecycleActionOrchestrator"/> доступен для разрешения.
    /// </summary>
    [Fact]
    public void AddLifecycleOrchestrator_RegistersOrchestrator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLifecycleOrchestrator();

        // Act
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<ILifecycleActionOrchestrator>();

        // Assert
        orchestrator.Should().NotBeNull();
        orchestrator.Should().BeAssignableTo<LifecycleActionOrchestrator>();
    }

    /// <summary>
    /// <see cref="ILifecycleActionOrchestrator"/> регистрируется как Scoped —
    /// в рамках одного scope возвращается один и тот же экземпляр,
    /// а между разными scope — разные.
    /// </summary>
    [Fact]
    public void AddLifecycleOrchestrator_RegistersAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLifecycleOrchestrator();
        using var provider = services.BuildServiceProvider();

        // Act
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();
        var fromScope1 = scope1.ServiceProvider.GetRequiredService<ILifecycleActionOrchestrator>();
        var sameScope1 = scope1.ServiceProvider.GetRequiredService<ILifecycleActionOrchestrator>();
        var fromScope2 = scope2.ServiceProvider.GetRequiredService<ILifecycleActionOrchestrator>();

        // Assert
        fromScope1.Should().BeSameAs(sameScope1);
        fromScope2.Should().NotBeSameAs(fromScope1);
    }

    /// <summary>
    /// Ручная регистрация handler-а через <c>AddScoped</c> и последующая
    /// регистрация оркестратора позволяют разрешить handler через
    /// <see cref="ILifecycleActionOrchestrator"/> в тестах.
    /// </summary>
    [Fact]
    public void ManualRegistration_HandlerIsResolvable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ILifecycleActionHandler, ParameterlessTestHandler>();
        services.AddLifecycleOrchestrator();

        // Act
        using var provider = services.BuildServiceProvider();
        var handlers = provider.GetServices<ILifecycleActionHandler>().ToList();

        // Assert
        handlers.Should().ContainSingle().Which.Should().BeOfType<ParameterlessTestHandler>();
    }

    /// <summary>
    /// Handler без параметров в конструкторе, пригодный для DI-регистрации.
    /// </summary>
    [Shared.Application.Core.DependencyInjection.Attributes.ManualConfiguration]
    private sealed class ParameterlessTestHandler
        : ILifecycleActionHandler<TestEntity>
    {
        public LifecyclePhase Phase => LifecyclePhase.BeforeSave;

        public string Key => "test";

        public int Order => 0;

        Type ILifecycleActionHandler.EntityType => typeof(TestEntity);

        public string[] RequiredNavigationProperties => [];

        public Task ExecuteAsync(IEnumerable<IEntity> entities, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task ExecuteAsync(IEnumerable<TestEntity> entities, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    #region Fail-fast валидация дублей на этапе регистрации (ФТ: lifecycle-actions.md)

    /// <summary>
    /// Handler, конфликтующий с <see cref="ParameterlessTestHandler"/> по
    /// <c>(EntityType, Phase, Key)</c>.
    /// </summary>
    [Shared.Application.Core.DependencyInjection.Attributes.ManualConfiguration]
    private sealed class DuplicateOfParameterlessHandler
        : ILifecycleActionHandler<TestEntity>
    {
        public LifecyclePhase Phase => LifecyclePhase.BeforeSave;

        public string Key => "test";

        public int Order => 0;

        Type ILifecycleActionHandler.EntityType => typeof(TestEntity);

        public string[] RequiredNavigationProperties => [];

        public Task ExecuteAsync(IEnumerable<IEntity> entities, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task ExecuteAsync(IEnumerable<TestEntity> entities, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    /// <summary>
    /// ФТ: при старте приложения <see cref="DependencyInjectionExtensions.AddLifecycleActions"/>
    /// обязан бросить <see cref="InvalidOperationException"/>, если среди
    /// зарегистрированных handler-ов есть дубли по
    /// <c>(EntityType, Phase, Key)</c>. Это fail-fast: приложение
    /// не должно подниматься с некорректной конфигурацией.
    /// </summary>
    [Fact]
    public void AddLifecycleActions_DuplicateHandlerKeys_Throws()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ILifecycleActionHandler, ParameterlessTestHandler>();
        services.AddScoped<ILifecycleActionHandler, DuplicateOfParameterlessHandler>();

        // Act
        var act = () => services.AddLifecycleActions();

        // Assert: ошибка на этапе регистрации, до BuildServiceProvider
        var ex = act.Should().Throw<InvalidOperationException>().Which;
        ex.Message.Should().Contain("test");
        ex.Message.Should().Contain("EntityType");
        ex.Message.Should().Contain("Phase");
        ex.Message.Should().Contain("Key");
    }

    /// <summary>
    /// Корректная конфигурация (handler-ы с разными <c>(EntityType, Phase, Key)</c>) —
    /// валидация проходит, регистрация возвращает коллекцию.
    /// </summary>
    [Fact]
    public void AddLifecycleActions_NoDuplicates_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ILifecycleActionHandler, ParameterlessTestHandler>();

        // Act
        var act = () => services.AddLifecycleActions();

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Ограничения валидации: handler с constructor-зависимостями

    /// <summary>
    /// Документирует поведение <see cref="DependencyInjectionExtensions.AddLifecycleActions"/>:
    /// валидация происходит через построение временного <see cref="ServiceProvider"/>
    /// (см. <c>DependencyInjectionExtensions.ValidateHandlerKeys</c>). Если handler
    /// имеет ctor-зависимость от сервиса, зарегистрированного <b>после</b>
    /// <c>AddLifecycleActions</c>, временный ServiceProvider не сможет резолвить
    /// handler — валидация упадёт, даже если в production всё работает.
    /// </summary>
    /// <remarks>
    /// Это known limitation: handler-ы должны иметь DI-friendly конструкторы
    /// (без сервисов, регистрируемых после <c>AddLifecycleActions</c>).
    /// На практике это означает: <c>AddLifecycleActions</c> должен вызываться
    /// последним в <c>ConfigureServices</c>, после всех остальных регистраций.
    /// </remarks>
    [Fact]
    public void AddLifecycleActions_HandlerDependsOnLaterService_StillResolvesViaLaterBuild()
    {
        // Arrange: имитируем production-сценарий, где handler зависит
        // от сервиса, зарегистрированного ПОСЛЕ AddLifecycleActions.
        // Сам AddLifecycleActions не должен ломать production-build.
        var services = new ServiceCollection();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<ILifecycleActionHandler, TenantAwareHandler>();
        services.AddLifecycleActions();
        services.AddScoped<IRequestOptions, RequestOptions>(); // "поздняя" регистрация

        // Act: собираем production-провайдер (он видит ВСЕ регистрации)
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetServices<ILifecycleActionHandler>()
            .OfType<TenantAwareHandler>()
            .Single();

        // Assert: в production-pipeline handler резолвится нормально,
        // потому что видит позднюю регистрацию.
        handler.Should().NotBeNull();
        handler.TenantId.Should().NotBeNull();
    }

    /// <summary>
    /// Контракт handler-а с ctor-зависимостью от <see cref="ITenantContext"/>.
    /// </summary>
    [Shared.Application.Core.DependencyInjection.Attributes.ManualConfiguration]
    private sealed class TenantAwareHandler(ITenantContext tenant)
        : ILifecycleActionHandler<TestEntity>
    {
        private ITenantContext Tenant { get; } = tenant;

        public string? TenantId => Tenant.TenantId;

        public LifecyclePhase Phase => LifecyclePhase.BeforeSave;

        public string Key => "tenant-aware";

        public int Order => 0;

        Type ILifecycleActionHandler.EntityType => typeof(TestEntity);

        public string[] RequiredNavigationProperties => [];

        public Task ExecuteAsync(IEnumerable<IEntity> entities, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task ExecuteAsync(IEnumerable<TestEntity> entities, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    /// <summary>
    /// Тестовый tenant-context для проверки резолва через "позднюю" регистрацию.
    /// </summary>
    private interface ITenantContext
    {
        string? TenantId { get; }
    }

    private sealed class TenantContext
        : ITenantContext
    {
        public string TenantId => "tenant-1";
    }

    private interface IRequestOptions;

    private sealed class RequestOptions
        : IRequestOptions;

    #endregion
}
