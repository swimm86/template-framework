// ----------------------------------------------------------------------------------------------
// <copyright file="LifecycleActionValidatorTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.LifecycleAction;
using Shared.Application.Core.LifecycleAction.Interfaces;
using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.Tests.LifecycleAction;

/// <summary>
/// Тесты для <see cref="LifecycleActionValidator"/>.
/// Покрывают контракт валидации: уникальность составного ключа
/// <c>(EntityType, Phase, Key)</c> и уникальность <c>Order</c> в пределах
/// одного <c>(EntityType, Phase)</c>.
/// </summary>
public sealed class LifecycleActionValidatorTests
{
    /// <summary>
    /// Тестовая сущность для проверки handler-ов BeforeSave.
    /// </summary>
    private sealed class TestEntity
        : IEntity
    {
        private Guid Id { get; } = Guid.NewGuid();

        object IEntity.Id => Id;
    }

    /// <summary>
    /// Вторая тестовая сущность для проверки изоляции handler-ов по типу.
    /// </summary>
    private sealed class OtherEntity : IEntity
    {
        private Guid Id { get; } = Guid.NewGuid();

        object IEntity.Id => Id;
    }

    /// <summary>
    /// Handler, выставляющий <c>(Phase, Key)</c> через параметры конструктора.
    /// Используется для тестов на уникальность <c>(EntityType, Phase, Key)</c>.
    /// </summary>
    [Shared.Application.Core.DependencyInjection.Attributes.ManualConfiguration]
    private sealed class ParameterizedHandler(
        LifecyclePhase phase,
        string key)
        : ILifecycleActionHandler<TestEntity>
    {
        public LifecyclePhase Phase { get; } = phase;

        public string Key { get; } = key;

        public int Order => 0;

        Type ILifecycleActionHandler.EntityType => typeof(TestEntity);

        public string[] RequiredNavigationProperties => [];

        public Task ExecuteAsync(
            IEnumerable<IEntity> entities,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task ExecuteAsync(
            IEnumerable<TestEntity> entities,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    /// <summary>
    /// Handler для <see cref="OtherEntity"/>.
    /// </summary>
    [Shared.Application.Core.DependencyInjection.Attributes.ManualConfiguration]
    private sealed class OtherEntityHandler(
        LifecyclePhase phase,
        string key)
        : ILifecycleActionHandler<OtherEntity>
    {
        public LifecyclePhase Phase { get; } = phase;

        public string Key { get; } = key;

        public int Order => 0;

        Type ILifecycleActionHandler.EntityType => typeof(OtherEntity);

        public string[] RequiredNavigationProperties => [];

        public Task ExecuteAsync(
            IEnumerable<IEntity> entities,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task ExecuteAsync(
            IEnumerable<OtherEntity> entities,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    /// <summary>
    /// Handler, выставляющий <c>(Phase, Key, Order)</c> через параметры
    /// конструктора. Используется для тестов на коллизии <c>Order</c>.
    /// </summary>
    [Shared.Application.Core.DependencyInjection.Attributes.ManualConfiguration]
    private sealed class OrderParameterizedHandler(
        LifecyclePhase phase,
        string key,
        int order)
        : ILifecycleActionHandler<TestEntity>
    {
        public LifecyclePhase Phase { get; } = phase;

        public string Key { get; } = key;

        public int Order { get; } = order;

        Type ILifecycleActionHandler.EntityType => typeof(TestEntity);

        public string[] RequiredNavigationProperties => [];

        public Task ExecuteAsync(
            IEnumerable<IEntity> entities,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task ExecuteAsync(
            IEnumerable<TestEntity> entities,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    #region (EntityType, Phase, Key) uniqueness

    /// <summary>
    /// Пустая коллекция — нет ни одного handler-а, валидация проходит.
    /// </summary>
    [Fact]
    public void Validate_EmptyCollection_DoesNotThrow()
    {
        // Act
        var act = () => LifecycleActionValidator.Validate([]);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Один handler — дублей нет, валидация проходит.
    /// </summary>
    [Fact]
    public void Validate_SingleHandler_DoesNotThrow()
    {
        // Arrange
        var handler = new ParameterizedHandler(LifecyclePhase.BeforeSave, "k");

        // Act
        var act = () => LifecycleActionValidator.Validate([handler]);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Два handler-а с разными <c>Key</c> и разными <c>Order</c> — нет конфликта.
    /// </summary>
    [Fact]
    public void Validate_DifferentKeys_DoesNotThrow()
    {
        // Arrange
        var h1 = new OrderParameterizedHandler(LifecyclePhase.BeforeSave, "k1", 0);
        var h2 = new OrderParameterizedHandler(LifecyclePhase.BeforeSave, "k2", 1);

        // Act
        var act = () => LifecycleActionValidator.Validate([h1, h2]);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Два handler-а с одним <c>(EntityType, Phase, Key)</c> — конфликт,
    /// выбрасывается <see cref="InvalidOperationException"/>.
    /// </summary>
    [Fact]
    public void Validate_DuplicateKey_Throws()
    {
        // Arrange
        var first = new ParameterizedHandler(LifecyclePhase.BeforeSave, "k");
        var duplicate = new ParameterizedHandler(LifecyclePhase.BeforeSave, "k");

        // Act
        var act = () => LifecycleActionValidator.Validate([first, duplicate]);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*EntityType*Phase*Key*k*");
    }

    /// <summary>
    /// Одинаковый <c>Key</c> для разных <c>EntityType</c> — это не конфликт.
    /// </summary>
    [Fact]
    public void Validate_SameKeyDifferentEntityType_Allowed()
    {
        // Arrange
        var onTest = new ParameterizedHandler(LifecyclePhase.BeforeSave, "k");
        var onOther = new OtherEntityHandler(LifecyclePhase.BeforeSave, "k");

        // Act
        var act = () => LifecycleActionValidator.Validate([onTest, onOther]);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Одинаковый <c>Key</c> для разных <c>Phase</c> — это не конфликт.
    /// </summary>
    [Fact]
    public void Validate_SameKeyDifferentPhase_Allowed()
    {
        // Arrange
        var before = new ParameterizedHandler(LifecyclePhase.BeforeSave, "k");
        var after = new ParameterizedHandler(LifecyclePhase.AfterSave, "k");

        // Act
        var act = () => LifecycleActionValidator.Validate([before, after]);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Сообщение об ошибке содержит все обнаруженные конфликты (включая
    /// список конфликтующих handler-ов) — для диагностики при старте.
    /// </summary>
    [Fact]
    public void Validate_MultipleDuplicateKeyGroups_AllReportedInMessage()
    {
        // Arrange: две независимые коллизии по Key, разные Order
        var group1A = new OrderParameterizedHandler(LifecyclePhase.BeforeSave, "k1", 0);
        var group1B = new OrderParameterizedHandler(LifecyclePhase.BeforeSave, "k1", 1);
        var group2A = new OrderParameterizedHandler(LifecyclePhase.BeforeSave, "k2", 2);
        var group2B = new OrderParameterizedHandler(LifecyclePhase.BeforeSave, "k2", 3);

        // Act
        var act = () => LifecycleActionValidator.Validate([group1A, group1B, group2A, group2B]);

        // Assert
        var ex = act.Should().Throw<InvalidOperationException>().Which;
        ex.Message.Should().Contain("k1");
        ex.Message.Should().Contain("k2");
        ex.Message.Should().Contain("Conflicting handlers");
        ex.Message.Should().Contain("Key='k1'");
        ex.Message.Should().Contain("Key='k2'");
    }

    #endregion

    #region (EntityType, Phase, Order) uniqueness

    /// <summary>
    /// Один handler — коллизий <c>Order</c> быть не может.
    /// </summary>
    [Fact]
    public void Validate_SingleHandler_OrderUnique()
    {
        // Arrange
        var handler = new OrderParameterizedHandler(LifecyclePhase.BeforeSave, "k", 0);

        // Act
        var act = () => LifecycleActionValidator.Validate([handler]);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Два handler-а с разными <c>Order</c> в одной фазе для одной сущности —
    /// это не коллизия.
    /// </summary>
    [Fact]
    public void Validate_DifferentOrdersSamePhase_DoesNotThrow()
    {
        // Arrange
        var first = new OrderParameterizedHandler(LifecyclePhase.BeforeSave, "k1", 0);
        var second = new OrderParameterizedHandler(LifecyclePhase.BeforeSave, "k2", 1);

        // Act
        var act = () => LifecycleActionValidator.Validate([first, second]);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Два handler-а с одинаковым <c>Order</c> в одной фазе для одной
    /// сущности — коллизия, <c>DispatchAsync</c> не сможет детерминированно
    /// упорядочить выполнение.
    /// </summary>
    [Fact]
    public void Validate_SameOrderSameEntityTypeAndPhase_Throws()
    {
        // Arrange
        var first = new OrderParameterizedHandler(LifecyclePhase.BeforeSave, "k1", 0);
        var second = new OrderParameterizedHandler(LifecyclePhase.BeforeSave, "k2", 0);

        // Act
        var act = () => LifecycleActionValidator.Validate([first, second]);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*EntityType*Phase*Order*0*");
    }

    /// <summary>
    /// Один и тот же <c>Order</c> для разных <c>Phase</c> — это не
    /// коллизия: фазы диспетчеризуются независимо.
    /// </summary>
    [Fact]
    public void Validate_SameOrderDifferentPhase_Allowed()
    {
        // Arrange
        var before = new OrderParameterizedHandler(LifecyclePhase.BeforeSave, "k1", 0);
        var after = new OrderParameterizedHandler(LifecyclePhase.AfterSave, "k2", 0);

        // Act
        var act = () => LifecycleActionValidator.Validate([before, after]);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Один и тот же <c>Order</c> для разных <c>EntityType</c> — это не
    /// коллизия: handler-ы фильтруются по типу в <c>DispatchAsync</c>.
    /// </summary>
    [Fact]
    public void Validate_SameOrderDifferentEntityType_Allowed()
    {
        // Arrange
        var onTest = new OrderParameterizedHandler(LifecyclePhase.BeforeSave, "k1", 0);
        var onOther = new OrderParameterizedHandlerForOther(LifecyclePhase.BeforeSave, "k2", 0);

        // Act
        var act = () => LifecycleActionValidator.Validate([onTest, onOther]);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Handler для <see cref="OtherEntity"/> с управляемым <c>Order</c>.
    /// </summary>
    [Shared.Application.Core.DependencyInjection.Attributes.ManualConfiguration]
    private sealed class OrderParameterizedHandlerForOther(
        LifecyclePhase phase,
        string key,
        int order)
        : ILifecycleActionHandler<OtherEntity>
    {
        public LifecyclePhase Phase { get; } = phase;

        public string Key { get; } = key;

        public int Order { get; } = order;

        Type ILifecycleActionHandler.EntityType => typeof(OtherEntity);

        public string[] RequiredNavigationProperties => [];

        public Task ExecuteAsync(
            IEnumerable<IEntity> entities,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task ExecuteAsync(
            IEnumerable<OtherEntity> entities,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    /// <summary>
    /// Сообщение об ошибке при коллизии <c>Order</c> содержит все
    /// конфликтующие группы, включая список конфликтующих handler-ов
    /// (с их <c>Key</c> и типом).
    /// </summary>
    [Fact]
    public void Validate_MultipleOrderCollisions_AllReportedInMessage()
    {
        // Arrange: две независимые коллизии по Order
        var group1A = new OrderParameterizedHandler(LifecyclePhase.BeforeSave, "k1", 0);
        var group1B = new OrderParameterizedHandler(LifecyclePhase.BeforeSave, "k2", 0);
        var group2A = new OrderParameterizedHandler(LifecyclePhase.AfterSave, "k3", 5);
        var group2B = new OrderParameterizedHandler(LifecyclePhase.AfterSave, "k4", 5);

        // Act
        var act = () => LifecycleActionValidator.Validate([group1A, group1B, group2A, group2B]);

        // Assert
        var ex = act.Should().Throw<InvalidOperationException>().Which;
        ex.Message.Should().Contain("Order: '0'");
        ex.Message.Should().Contain("Order: '5'");
        ex.Message.Should().Contain("Conflicting handlers");
        ex.Message.Should().Contain("Key='k1'");
        ex.Message.Should().Contain("Key='k2'");
        ex.Message.Should().Contain("Key='k3'");
        ex.Message.Should().Contain("Key='k4'");
    }

    #endregion

    #region Argument validation

    /// <summary>
    /// Передача <c>null</c> вместо коллекции — <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void Validate_NullCollection_ThrowsArgumentNullException()
    {
        // Act
        var act = () => LifecycleActionValidator.Validate(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion
}
